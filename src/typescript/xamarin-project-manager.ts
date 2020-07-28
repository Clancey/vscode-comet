import { ObservableValue } from "./ObservableValue";
import { MSBuildProject } from "./omnisharp/protocol";
import * as vscode from 'vscode';

export class XamarinProjectManager
{
	static SelectedProject: MSBuildProject;
	static SelectedProjectConfiguration: string;

	constructor(context: vscode.ExtensionContext)
	{
		context.subscriptions.push(vscode.commands.registerCommand("xamarin.selectProject", this.showProjectPicker, this));
	
		this.projectStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
		this.projectStatusBarItem.tooltip = "Xamarin Startup Project";
		this.projectStatusBarItem.text = "Xamarin";

		this.updateProjectStatus();
	}

	projectStatusBarItem: vscode.StatusBarItem;

	public StartupProjects = new ObservableValue<MSBuildProject[]>();

	
	public async showProjectPicker(): Promise<void> {
		var projects = this.StartupProjects.value
			.map(x => ({
				//description: x.type.toString(),
				label: x.AssemblyName,
				project: x,
			}));
		const p = await vscode.window.showQuickPick(projects, { placeHolder: "Choose a Startup Project" });
		if (p) {
	
			const c = await vscode.window.showQuickPick([ "Debug", "Release" ], { placeHolder: "Build Configuration" });
			if (c) {
				XamarinProjectManager.SelectedProject = p.project;
				XamarinProjectManager.SelectedProjectConfiguration = c;
			}
		}
		this.updateProjectStatus();
	}

	public async updateProjectStatus()
	{
		var projectString = XamarinProjectManager.SelectedProject === undefined ? " Select a Project" : ` ${XamarinProjectManager.SelectedProject.Path} | ${XamarinProjectManager.SelectedProjectConfiguration}`;
        this.projectStatusBarItem.text = projectString;
        this.projectStatusBarItem.command = "xamarin.selectProject";
        this.projectStatusBarItem.show();
	}
}