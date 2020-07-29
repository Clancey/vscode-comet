import { MSBuildProject, TargetFramework } from "./omnisharp/protocol";
import * as vscode from 'vscode';
import { BaseEvent, WorkspaceInformationUpdated } from './omnisharp/loggingEvents';
import { EventType } from './omnisharp/EventType';
import { MsBuildProjectAnalyzer } from './msbuild-project-analyzer';
import { DeviceData, XamarinUtil } from "./xamarin-util";

let fs = require('fs');

export class MSBuildProjectInfo implements MSBuildProject
{
	public static async fromProject(project: MSBuildProject): Promise<MSBuildProjectInfo>
	{
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

export class XamarinProjectManager
{
	static SelectedProject: MSBuildProjectInfo;
	static SelectedProjectConfiguration: string;
	static SelectedDevice: DeviceData;
	static Devices: DeviceData[];

	omnisharp: any;

	constructor(context: vscode.ExtensionContext)
	{
		this.omnisharp = vscode.extensions.getExtension("ms-dotnettools.csharp").exports;

		this.omnisharp.eventStream.subscribe(async (e: BaseEvent) => {
			if (e.type === EventType.WorkspaceInformationUpdated) {

				this.StartupProjects = new Array<MSBuildProjectInfo>();

				for (var p of (<WorkspaceInformationUpdated>e).info.MsBuild.Projects)
				{
					if (p.TargetFramework.startsWith("MonoAndroid") || p.TargetFramework.startsWith("Xamarin"))
					{
						this.StartupProjects.push(await MSBuildProjectInfo.fromProject(p));
					}
				}
			}
		});

		context.subscriptions.push(vscode.commands.registerCommand("xamarin.selectProject", this.showProjectPicker, this));
		context.subscriptions.push(vscode.commands.registerCommand("xamarin.selectDevice", this.showDevicePicker, this));
	
		this.projectStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
		this.projectStatusBarItem.tooltip = "Select a Project";
		this.projectStatusBarItem.text = "Select a Project";

		this.deviceStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
		this.deviceStatusBarItem.tooltip = "Select a Device";
		this.deviceStatusBarItem.text = "Select a Device";

		this.updateProjectStatus();
		this.updateDeviceStatus();
	}

	projectStatusBarItem: vscode.StatusBarItem;
	deviceStatusBarItem: vscode.StatusBarItem;

	public StartupProjects = new Array<MSBuildProjectInfo>();

	
	public async showProjectPicker(): Promise<void> {
		var projects = this.StartupProjects
			.map(x => ({
				//description: x.type.toString(),
				label: x.AssemblyName,
				project: x,
			}));
		const p = await vscode.window.showQuickPick(projects, { placeHolder: "Choose a Startup Project" });
		if (p) {
	
			const c = await vscode.window.showQuickPick(p.project.Configurations, { placeHolder: "Build Configuration" });
			if (c) {
				XamarinProjectManager.SelectedProject = p.project;
				XamarinProjectManager.SelectedProjectConfiguration = c;
				XamarinProjectManager.SelectedDevice = undefined;
			}
		}
		this.updateProjectStatus();
		this.updateDeviceStatus();
	}

	public async updateProjectStatus()
	{
		var projectString = XamarinProjectManager.SelectedProject === undefined ? " Select a Project" : ` ${XamarinProjectManager.SelectedProject.Name} | ${XamarinProjectManager.SelectedProjectConfiguration}`;
		this.projectStatusBarItem.text = projectString;
		this.projectStatusBarItem.tooltip = XamarinProjectManager.SelectedProject === undefined ? "Xamarin Startup Project" : XamarinProjectManager.SelectedProject.Path;
		this.projectStatusBarItem.command = "xamarin.selectProject";
		this.projectStatusBarItem.show();
	}


	public async showDevicePicker(): Promise<void> {

		if (XamarinProjectManager.SelectedProject === undefined)
		{
			await vscode.window.showInformationMessage("Select a Startup Project first.");
			return;
		}

		await this.ReloadDevices();

		var devices = XamarinProjectManager.Devices
			.map(x => ({
				//description: x.type.toString(),
				label: x.name,
				device: x,
			}));
		const p = await vscode.window.showQuickPick(devices, { placeHolder: "Choose a Device" });
		if (p) {
			XamarinProjectManager.SelectedDevice = p.device;
		}
		this.updateDeviceStatus();
	}

	public async updateDeviceStatus()
	{
		this.deviceStatusBarItem.text = XamarinProjectManager.SelectedDevice === undefined ? " Select a Device" : ` ${XamarinProjectManager.SelectedDevice.name}`;
		this.deviceStatusBarItem.tooltip = XamarinProjectManager.SelectedProject === undefined ? "Select a Device" : XamarinProjectManager.SelectedDevice.name;
		this.deviceStatusBarItem.command = "xamarin.selectDevice";
		this.deviceStatusBarItem.show();
	}

	public async ReloadDevices()
	{
		var util = new XamarinUtil();
		XamarinProjectManager.Devices = await util.GetDevices();
	}
}