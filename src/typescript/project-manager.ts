import { MSBuildProject, TargetFramework } from "./omnisharp/protocol";
import * as vscode from 'vscode';
import { BaseEvent, WorkspaceInformationUpdated } from './omnisharp/loggingEvents';
import { EventType } from './omnisharp/EventType';
import { MsBuildProjectAnalyzer } from './msbuild-project-analyzer';
import { DeviceData, MobileUtil, SimCtlDevice, AppleDevicesAndSimulators } from "./util";

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
}

export class MSBuildProjectInfo implements MSBuildProject {
	public static async fromProject(project: MSBuildProject): Promise<MSBuildProjectInfo> {
		var r = new MSBuildProjectInfo();

		r.ProjectGuid = project.ProjectGuid;
		r.Path = project.Path;
		r.AssemblyName = project.AssemblyName;
		r.TargetPath = project.TargetPath;
		r.TargetFramework = project.TargetFramework;
		r.SourceFiles = project.SourceFiles;
		r.TargetFrameworks = project.TargetFrameworks;
		r.OutputPath = project.OutputPath;
		r.IsExe = project.IsExe;
		r.IsUnityProject = project.IsUnityProject;

		var projXml = fs.readFileSync(r.Path);
		var msbpa = new MsBuildProjectAnalyzer(projXml);
		await msbpa.analyze();

		r.Configurations = msbpa.getConfigurationNames();
		r.Platforms = msbpa.getPlatformNames();
		r.Name = msbpa.getProjectName();
		return r;
	}

	Name: string;
	ProjectGuid: string;
	Path: string;
	AssemblyName: string;
	TargetPath: string;
	TargetFramework: string;
	SourceFiles: string[];
	TargetFrameworks: TargetFramework[];
	OutputPath: string;
	IsExe: boolean;
	IsUnityProject: boolean;

	Configurations: string[];
	Platforms: string[];
}

export class MobileSessionStartupInfo {
	constructor() {
		this.Project = undefined;
		this.Configuration = undefined;
		this.TargetFramework = undefined;
		this.DebugPort = 55555;
		this.Device = undefined;
	}

	Project: MSBuildProjectInfo;
	Configuration: string;
	TargetFramework: string;
	Device: DeviceData;
	DebugPort: number = 55555;
}

export class MobileProjectManager {
	static selectProjectCommandId: string = "dotnetmobile.selectProject";
	static selectDeviceCommandId: string = "dotnetmobile.selectDevice";

	static Shared: MobileProjectManager;

	StartupInfo: MobileSessionStartupInfo;

	omnisharp: any;
	context: vscode.ExtensionContext;

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

		this.omnisharp.eventStream.subscribe(async (e: BaseEvent) => {

			// var evtTp = EventType[e.type];

			// if (evtTp)
			// 	console.log("OMNIEVENT: " + evtTp);


			if (e.type === EventType.WorkspaceInformationUpdated) {

				this.StartupProjects = new Array<MSBuildProjectInfo>();

				for (var p of (<WorkspaceInformationUpdated>e).info.MsBuild.Projects) {

					if (MobileProjectManager.getIsSupportedProject(p)) {
						this.StartupProjects.push(await MSBuildProjectInfo.fromProject(p));
					}
				}

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

	public StartupProjects = new Array<MSBuildProjectInfo>();

	fixTfm(targetFramework: string) : string {

		// /^net[0-9]{2}(\-[a-z0-9\.]+)?$/gis
		var r = /^net[0-9]{2}(\-[a-z0-9\.]+)?$/gis.test(targetFramework);
		if (r)
			return 'net' + targetFramework[3] + '.' + targetFramework[4] + targetFramework.substr(5);
		return targetFramework;
	}

	private startupProjectCommand()
	{
		this.selectStartupProject(true);
	}

	public async selectStartupProject(interactive: boolean = false): Promise<any> {

		var availableProjects = this.StartupProjects;

		if (!availableProjects || availableProjects.length <= 0)
			return;

		var selectedProject = undefined;

		// Try and auto select some defaults
		if (availableProjects.length == 1)
			selectedProject = availableProjects[0];
		
		// If there are multiple projects and we allow interactive, show a picker
		if (!selectedProject && availableProjects.length > 0 && interactive)
		{
			var projects = availableProjects
			.map(x => ({
				label: x.AssemblyName,
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

		var selectedTargetFramework = undefined;

		if (selectedProject.TargetFrameworks && selectedProject.TargetFrameworks.length > 0)
		{
			// If just 1, use it without asking
			if (selectedProject.TargetFrameworks.length == 1)
			{
				selectedTargetFramework = this.fixTfm(selectedProject.TargetFrameworks[0].ShortName);
			}
			else
			{
				// If more than 1 and we are interactive, prompt the user to pick
				if (interactive)
				{
					var tfms = selectedProject.TargetFrameworks
						// Only return supported tfms
						.filter(x => MobileProjectManager.getIsSupportedTargetFramework(x.ShortName))
						.map(x => this.fixTfm(x.ShortName));

					var t = await vscode.window.showQuickPick(tfms, { placeHolder: "Startup Project's Target Framework" });

					if (t)
						selectedTargetFramework = t;
				}
				else {
					// Pick the first one if not interactive
					selectedTargetFramework = this.fixTfm(selectedProject.TargetFrameworks[0].ShortName);
				}
			}
		}
		else if (selectedProject.TargetFramework)
		{
			selectedTargetFramework = this.fixTfm(selectedProject.TargetFramework);
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
			MobileProjectManager.Shared.StartupInfo.Device = undefined;
		}

		if (selectedTargetFramework && MobileProjectManager.getProjectType(selectedTargetFramework) == ProjectType.MacCatalyst)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platform = 'maccatalyst';
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

			var projectString = selProj === undefined ? "Startup Project" : `${selProj.Name ?? selProj.AssemblyName} | ${selTfm} | ${selConfig}`;

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
				title: 'Loading Android Devices'
			}, async (progress) => {
				
				progress.report({  increment: 0 });
				androidDevices = await util.GetAndroidDevices();
				progress.report({ increment: 100 });
			});

			var androidPickerDevices = androidDevices
				.map(x => ({
					//description: x.type.toString(),
					label: x.name,
					device: x,
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
		else if (projectType === ProjectType.MacCatalyst)
		{
			var deviceData = new DeviceData();
			deviceData.name = "Local Machine";
			deviceData.platform = 'maccatalyst';
			deviceData.serial = "local";

			selectedDevice = deviceData;
		}
		else if (projectType === ProjectType.iOS) {
			
			var iosDevices : AppleDevicesAndSimulators;

			await vscode.window.withProgress({
				location: vscode.ProgressLocation.Notification,
				cancellable: false,
				title: 'Loading iOS Devices'
			}, async (progress) => {
				
				progress.report({  increment: 0 });
				iosDevices = await util.GetiOSDevices();
				progress.report({ increment: 100 });
			});

			var iosPickerDevices = iosDevices.devices
				.map(x => ({
					//description: x.type.toString(),
					label: x.name,
					device: x,
					devices: null as SimCtlDevice[]
				}))
				.concat(iosDevices.simulators
					.map(y => ({
						label: y.name,
						device: null,
						devices: y.devices
					})));

			const p = await vscode.window.showQuickPick(iosPickerDevices, { placeHolder: "Select a Device" });
			if (p) {
				if (p.device)
					selectedDevice = p.device;
				else {
					var devicePickerItems = p.devices
						.map(z => ({
							label: z.runtime.name,
							device: z
						}));

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
						var deviceData = new DeviceData();
						deviceData.name = d.device.name + ' | ' + d.device.runtime.name;
						deviceData.iosSimulatorDevice = d.device;
						deviceData.isEmulator = true;
						deviceData.isRunning = false;
						deviceData.platform = 'ios';
						deviceData.serial = d.device.udid;
						deviceData.version = d.device.runtime.version;

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

	public static getIsSupportedTargetFramework(targetFramework: string) : boolean
	{
		var projType = this.getProjectType(targetFramework);

		return projType == ProjectType.Android || projType == ProjectType.iOS || projType == ProjectType.MacCatalyst;
	}

	public static getIsSupportedProject(project: MSBuildProject): boolean
	{
		if (project.TargetFrameworks && project.TargetFrameworks.length > 0) {

			for (var tf of project.TargetFrameworks)
			{
				if (this.getIsSupportedTargetFramework(tf.ShortName))
					return true;
			}
		} else {
			if (this.getIsSupportedTargetFramework(project.TargetFramework))
				return true;
		}

		return false;
	}

	public static getProjectType(targetFramework: string): ProjectType
	{
		if (!targetFramework)
			targetFramework = MobileProjectManager.Shared?.StartupInfo?.TargetFramework;

		if (!targetFramework)
			return ProjectType.Mono;
		
		var tfm = targetFramework.toLowerCase().replace(".", "");

		if (tfm.indexOf('monoandroid') >= 0 || tfm.indexOf('-android') >= 0)
			return ProjectType.Android;
		else if (tfm.indexOf('xamarinios') >= 0 || tfm.indexOf('-ios') >= 0)
			return ProjectType.iOS;
		else if (tfm.indexOf('xamarinmac') >= 0 || tfm.indexOf('-macos') >= 0)
			return ProjectType.Mac;
		else if (tfm.indexOf('xamarinmaccatalyst') >= 0 || tfm.indexOf('-maccatalyst') >= 0)
			return ProjectType.MacCatalyst;
	}

	public static getProjectIsCore(targetFramework: string): boolean
	{
		var tfm = targetFramework.toLowerCase();

		return tfm.startsWith('net') && this.getIsSupportedTargetFramework(tfm);
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
					if (selectedDevice.iosSimulatorDevice)
						return 'iPhoneSimulator';
				}

				return 'iPhone';
			}
		}
		
		return null;
	}
}