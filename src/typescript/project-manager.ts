import { MSBuildProject } from "./omnisharp/protocol";
import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc/node';

import { BaseEvent, WorkspaceInformationUpdated } from './omnisharp/loggingEvents';
import { EventType } from './omnisharp/EventType';
import { DeviceData, MobileUtil } from "./util";
import { analyzerExePath, extensionId } from "./extensionInfo";
import { ProjectInfo } from "./ProjectInfo";
import { WorkspaceInfo } from "./WorkspaceInfo";
import { TargetFrameworkInfo } from "./TargetFrameworkInfo";
import { MobileSessionStartupInfo } from "./MobileSessionStartupInfo";

const path = require('path');
let fs = require('fs');

export enum ProjectType
{
	Mono,
	Android,
	iOS,
	MacCatalyst,
	Mac,
	UWP,
	Unknown,
	WPF,
	Blazor,
	Windows
}

export class MobileProjectManager {
	static selectProjectCommandId: string = "dotnetmobile.selectProject";
	static selectDeviceCommandId: string = "dotnetmobile.selectDevice";

	static Shared: MobileProjectManager;

	StartupInfo: MobileSessionStartupInfo;

	omnisharp: any;
	context: vscode.ExtensionContext;
	dotnetProjectAnalyzerRpc: rpc.MessageConnection;
	rpcEvaluateProjectRequest : rpc.RequestType2<string, any, ProjectInfo, void>;

	constructor(context: vscode.ExtensionContext) {
		MobileProjectManager.Shared = this;

		this.StartupInfo = new MobileSessionStartupInfo();

		this.context = context;
		this.omnisharp = vscode.extensions.getExtension("ms-dotnettools.csharp").exports;

		this.loadingStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
		this.loadingStatusBarItem.text = "$(sync~spin) Mobile .NET Projects...";
		this.loadingStatusBarItem.tooltip = "Looking for Mobile .NET Projects loaded via OmniSharp...";
		this.loadingStatusBarItem.show();

		var loadingStatusBarItemHidden = false;

		let childProcess = cp.spawn('dotnet', [ analyzerExePath ]);

		// Use stdin and stdout for communication:
		this.dotnetProjectAnalyzerRpc = rpc.createMessageConnection(
			new rpc.StreamMessageReader(childProcess.stdout),
			new rpc.StreamMessageWriter(childProcess.stdin));
		
		let rpcWorkspaceChangedNotification = new rpc.NotificationType<WorkspaceInfo>('WorkspaceChanged');
		let rpcOpenWorkspaceRequest = new rpc.RequestType3<string, string, string, void, void>('OpenWorkspace');
		this.rpcEvaluateProjectRequest = new rpc.RequestType2<string, any, ProjectInfo, void>('EvaluateProject');
		this.updateMenus();

		this.dotnetProjectAnalyzerRpc.onNotification(rpcWorkspaceChangedNotification, (workspace: WorkspaceInfo) => {

			this.StartupProjects = workspace.Solution.Projects.filter(p => p.IsExe || !p.IsExe);

			// After projects are reloaded we need to see if any of our existing startup settings
			// are now invalid
			if (!this.StartupInfo)
				this.StartupInfo = new MobileSessionStartupInfo();

			if (!this.StartupInfo.Project || !this.StartupProjects.find(p => p.Path === this.StartupInfo.Project.Path))
			{
				this.StartupInfo.Project = undefined;
				this.StartupInfo.Configuration = undefined;
				this.StartupInfo.TargetFramework = undefined;
				this.StartupInfo.Device = undefined;

				this.selectStartupProject(false);
			}

			if (!loadingStatusBarItemHidden && this.loadingStatusBarItem)
			{
				this.loadingStatusBarItem.hide();
				this.loadingStatusBarItem.dispose();
				this.loadingStatusBarItem = null;
			}

			this.updateMenus();

			this.updateProjectStatus();
			this.updateDeviceStatus();
		});


		this.dotnetProjectAnalyzerRpc.listen();

		this.omnisharp.eventStream.subscribe(async (e: BaseEvent) => {

			if (e.type === EventType.WorkspaceInformationUpdated) {

				this.StartupProjects = new Array<ProjectInfo>();
				let msBuild = (<WorkspaceInformationUpdated>e).info.MsBuild;
				var slnOrProjPath = msBuild.SolutionPath;

				if (!slnOrProjPath.endsWith(".sln")) {
					// If there's no sln file, the SolutionPath point to the folder.
					let project = msBuild.Projects.find(p => p.Path.includes(slnOrProjPath));
					// Use the project file in this case and DotNetWorkspaceAnalyzer can handle it.
					slnOrProjPath = project.Path;
				}

				this.dotnetProjectAnalyzerRpc.sendRequest(
					rpcOpenWorkspaceRequest,
					slnOrProjPath,
					this.StartupInfo?.Configuration,
					this.StartupInfo?.Platform);
			}
		});
	}

	isMenuSetup: boolean = false;

	
	updateMenus()
	{
		if (!this.isMenuSetup)
		{
			this.context.subscriptions.push(vscode.commands.registerCommand(MobileProjectManager.selectProjectCommandId, this.startupProjectCommand, this));
			this.context.subscriptions.push(vscode.commands.registerCommand(MobileProjectManager.selectDeviceCommandId, this.showDevicePicker, this));

			this.projectStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
			this.projectStatusBarItem.command = MobileProjectManager.selectProjectCommandId;
			this.deviceStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
			this.deviceStatusBarItem.command = MobileProjectManager.selectDeviceCommandId;

			this.isMenuSetup = true;
		}

		this.updateProjectStatus();
		this.updateDeviceStatus();
	}

	loadingStatusBarItem: vscode.StatusBarItem;
	projectStatusBarItem: vscode.StatusBarItem;
	deviceStatusBarItem: vscode.StatusBarItem;

	public StartupProjects = new Array<ProjectInfo>();

	private startupProjectCommand()
	{
		this.selectStartupProject(true);
	}

	public async selectStartupProject(interactive: boolean = false): Promise<any> {

		var availableProjects = this.StartupProjects;

		if (!availableProjects || availableProjects.length <= 0)
			return;

		var selectedProject: ProjectInfo;

		// Try and auto select some defaults
		if (availableProjects.length == 1)
			selectedProject = availableProjects[0];
		
		// If there are multiple projects and we allow interactive, show a picker
		if (!selectedProject && availableProjects.length > 0 && interactive)
		{
			var projects = availableProjects
			.map(x => ({
				label: x.AssemblyName ?? x.Name,
				project: x,
			}));

			if (projects && projects.length > 0)
				selectedProject = (await vscode.window.showQuickPick(projects, { placeHolder: "Startup Project" })).project;
		}

		// If we were interactive and didn't select a project, don't assume the first
		// if non interactive, it's ok to assume the first project
		if (!selectedProject && !interactive)
			selectedProject = availableProjects[0];
		
		if (!selectedProject)
			return;

		var selectedTargetFramework: TargetFrameworkInfo;

		if (selectedProject.TargetFrameworks && selectedProject.TargetFrameworks.length > 0)
		{
			// If just 1, use it without asking
			if (selectedProject.TargetFrameworks.length == 1)
			{
				selectedTargetFramework = selectedProject.TargetFrameworks[0];
			}
			else
			{
				// If more than 1 and we are interactive, prompt the user to pick
				if (interactive)
				{
					var tfms = selectedProject.TargetFrameworks
						// Only return supported tfms
						.filter(x => MobileProjectManager.getIsSupportedTargetFramework(x))
						.map(x => x.FullName);

					var t = await vscode.window.showQuickPick(tfms, { placeHolder: "Startup Project's Target Framework" });

					if (t)
						selectedTargetFramework = selectedProject.TargetFrameworks.filter(st => st.FullName == t)[0];
				}
				else {
					// Pick the first one if not interactive
					selectedTargetFramework = selectedProject.TargetFrameworks[0];
				}
			}
		}

		if (!selectedTargetFramework)
			return;

		var projectSelectionWasChanged = false;

		if (MobileProjectManager.Shared.StartupInfo.Project !== selectedProject)
		{
			MobileProjectManager.Shared.StartupInfo.Project = selectedProject;
			projectSelectionWasChanged = true;
		}

		if (MobileProjectManager.Shared.StartupInfo.TargetFramework !== selectedTargetFramework)
		{
			MobileProjectManager.Shared.StartupInfo.TargetFramework = selectedTargetFramework;
			projectSelectionWasChanged = true;
		}
		
		var defaultConfig = "Debug";

		var selectedConfiguration = undefined;

		if (selectedProject && selectedProject.Configurations)
		{
			if (selectedProject.Configurations.length > 0)
			{
				if (selectedProject.Configurations.length == 1)
				{
					selectedConfiguration = selectedProject.Configurations[0];
				}
				else
				{
					if (interactive)
					{
						var c = await vscode.window.showQuickPick(selectedProject.Configurations, { placeHolder: "Startup Project's Configuration" });

						if (c)
							selectedConfiguration = c;
					}
					else
					{
						if (selectedProject.Configurations.includes(defaultConfig))
							selectedConfiguration = defaultConfig;
						else
							selectedConfiguration = selectedProject.Configurations[0];
					}

				}
			}
			else
			{
				selectedConfiguration = defaultConfig;
			}
		}

		if (selectedConfiguration)
		{
			if (MobileProjectManager.Shared.StartupInfo.Configuration !== selectedConfiguration)
			{
				MobileProjectManager.Shared.StartupInfo.Configuration = selectedConfiguration;
				projectSelectionWasChanged = true;
			}
		}

		if (projectSelectionWasChanged)
		{
			// Get evaluated project
			var evaluatedProject = await this.dotnetProjectAnalyzerRpc.sendRequest(this.rpcEvaluateProjectRequest,
				selectedProject.Path,
				{
					TargetFramework: selectedTargetFramework.FullName,
					Configuration: selectedConfiguration
				});

			MobileProjectManager.Shared.StartupInfo.Project = evaluatedProject;
			MobileProjectManager.Shared.StartupInfo.Device = undefined;
		}

		if (selectedTargetFramework && MobileProjectManager.getProjectType(selectedTargetFramework) == ProjectType.MacCatalyst)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platforms = [ 'maccatalyst' ];
			deviceData.serial = "local";

			MobileProjectManager.Shared.StartupInfo.Device = deviceData;
		}

		if (selectedTargetFramework && MobileProjectManager.getProjectType(selectedTargetFramework) == ProjectType.Windows)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platforms = [ 'windows' ];
			deviceData.serial = "local";

			MobileProjectManager.Shared.StartupInfo.Device = deviceData;
		}

		this.updateProjectStatus();
		this.updateDeviceStatus();
	}

	public async updateProjectStatus() {

		if (this.StartupProjects && this.StartupProjects.length > 0)
		{
			var startupInfo = MobileProjectManager.Shared.StartupInfo;

			var selProj = startupInfo.Project;
			var selTfm = startupInfo.TargetFramework;
			var selConfig = startupInfo.Configuration;

			var projectString = selProj === undefined ? "Startup Project" : `${selProj.Name ?? selProj.AssemblyName} | ${selTfm.FullName} | ${selConfig}`;

			this.projectStatusBarItem.text = "$(project) " + projectString;
			this.projectStatusBarItem.tooltip = selProj === undefined ? "Select a Startup Project" : selProj.Path;
			this.projectStatusBarItem.show();
		}
		else
		{
			this.projectStatusBarItem.hide();
		}
	}


	public async showDevicePicker(): Promise<void> {

		if (MobileProjectManager.Shared.StartupInfo?.Project === undefined || MobileProjectManager.Shared.StartupInfo?.TargetFramework === undefined) {
			await vscode.window.showInformationMessage("Select a Startup Project first.");
			return;
		}

		var selectedDevice : DeviceData = undefined;

		var util = new MobileUtil();

		var projectType = MobileProjectManager.getProjectType(MobileProjectManager.Shared.StartupInfo.TargetFramework);

		if (projectType === ProjectType.Android) {

			var androidDevices : DeviceData[] = [];

			await vscode.window.withProgress({
				location: vscode.ProgressLocation.Notification,
				cancellable: false,
				title: 'Loading Devices'
			}, async (progress) => {
				
				progress.report({  increment: 0 });
				androidDevices = await util.GetDevices("android");
				progress.report({ increment: 100 });
			});

			var androidPickerDevices = androidDevices
				.map(x => ({
					description:  x.isEmulator ? "Emulator" : "Device",
					label: x.name,
					device: x,
					detail: x.details + " - " + x.version
				}));

			if (androidPickerDevices && androidPickerDevices.length > 0)
			{
				// If only one, don't prompt to pick
				if (androidPickerDevices.length == 1)
				{
					selectedDevice = androidPickerDevices[0].device;
				}
				else
				{
					const p = await vscode.window.showQuickPick(androidPickerDevices, { placeHolder: "Select a Device" });
					if (p) {
						selectedDevice = p.device;
					}
				}
			}
		}
		else if (projectType === ProjectType.Windows)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platforms = [ 'windows' ];
			deviceData.serial = "local";

			selectedDevice = deviceData;
		}
		else if (projectType === ProjectType.MacCatalyst)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platforms = [ 'maccatalyst' ];
			deviceData.serial = "local";

			selectedDevice = deviceData;
		}
		else if (projectType === ProjectType.iOS) {
			
			var iosDevices : DeviceData[];

			await vscode.window.withProgress({
				location: vscode.ProgressLocation.Notification,
				cancellable: false,
				title: 'Loading Devices'
			}, async (progress) => {
				
				progress.report({  increment: 0 });
				iosDevices = await util.GetDevices('ios');
				progress.report({ increment: 100 });
			});

			var iosPickerDevices = iosDevices
				.map(x => ({
					description: x.isEmulator ? "Emulator" : "Device",
					detail: x.version + " - " + x.details,
					label: x.name,
					device: x
				}));

			const p = await vscode.window.showQuickPick(iosPickerDevices, { placeHolder: "Select a Device" });
			if (p) {
				if (p.device)
					selectedDevice = p.device;
				else {
					var devicePickerItems = iosPickerDevices;

					var d;

					if (devicePickerItems && devicePickerItems.length > 0)
					{
						if (devicePickerItems.length == 1)
						{
							d = devicePickerItems[0];
						}
						else
						{
							d = await vscode.window.showQuickPick(devicePickerItems, { placeHolder: "Select a Runtime Version" });
						}
					}
					
					if (d) {
						var deviceData = d.device as DeviceData;

						selectedDevice = deviceData;
					}
				}
			}
		}

		if (selectedDevice) {
			MobileProjectManager.Shared.StartupInfo.Device = selectedDevice;
		}

		this.updateDeviceStatus();
	}

	public async updateDeviceStatus() {

		if (this.StartupProjects && this.StartupProjects.length > 0)
		{
			var startupInfo = MobileProjectManager.Shared.StartupInfo;

			var selectedDevice : DeviceData = undefined;

			if (startupInfo && startupInfo.Project && startupInfo.TargetFramework)
				selectedDevice = startupInfo.Device;

			var deviceStr = selectedDevice === undefined ? "Select a Device" : `${selectedDevice.name}`;
			this.deviceStatusBarItem.text = "$(device-mobile) " + deviceStr;
			this.deviceStatusBarItem.tooltip = deviceStr;
			this.deviceStatusBarItem.show();
		}
		else
		{
			this.deviceStatusBarItem.hide();
		}
	}

	public static getIsSupportedTargetFramework(targetFramework: TargetFrameworkInfo) : boolean
	{
		var projType = this.getProjectType(targetFramework);

		return projType == ProjectType.Android || projType == ProjectType.iOS || projType == ProjectType.MacCatalyst || projType == ProjectType.Windows;
	}

	public static getIsSupportedProject(project: ProjectInfo): boolean
	{
		if (!project.IsExe)
			return false;

		if (project.TargetFrameworks && project.TargetFrameworks.length > 0) {

			for (var tf of project.TargetFrameworks)
			{
				if (this.getIsSupportedTargetFramework(tf))
					return true;
			}
		}

		return false;
	}

	public static getProjectType(targetFramework: TargetFrameworkInfo): ProjectType
	{
		if (!targetFramework)
			targetFramework = MobileProjectManager.Shared?.StartupInfo?.TargetFramework;

		if (!targetFramework)
			return ProjectType.Mono;
		
		var tfm = targetFramework.FullName.toLowerCase().replace(".", "");

		if (tfm.indexOf('monoandroid') >= 0 || tfm.indexOf('-android') >= 0)
			return ProjectType.Android;
		else if (tfm.indexOf('xamarinios') >= 0 || tfm.indexOf('-ios') >= 0)
			return ProjectType.iOS;
		else if (tfm.indexOf('xamarinmac') >= 0 || tfm.indexOf('-macos') >= 0)
			return ProjectType.Mac;
		else if (tfm.indexOf('xamarinmaccatalyst') >= 0 || tfm.indexOf('-maccatalyst') >= 0)
			return ProjectType.MacCatalyst;
		else if (tfm.indexOf('-windows') >= 0)
			return ProjectType.Windows;
	}

	public static getProjectIsCore(targetFramework: TargetFrameworkInfo): boolean
	{
		var tfm = targetFramework.FullName.toLowerCase();

		return tfm.startsWith('net') && this.getIsSupportedTargetFramework(targetFramework);
	}

	public static getSelectedProjectPlatform():string
	{
		var projectType = this.getProjectType(MobileProjectManager.Shared?.StartupInfo?.TargetFramework);

		if (projectType)
		{
			if (projectType === ProjectType.iOS)
			{
				var selectedDevice = MobileProjectManager.Shared?.StartupInfo?.Device;
				if (selectedDevice)
				{
					if (selectedDevice.isEmulator)
						return 'iPhoneSimulator';
				}

				return 'iPhone';
			}
		}
		
		return null;
	}
}
