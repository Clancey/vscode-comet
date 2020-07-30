'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
//import {ProjectType } from './msbuild-project-analyzer';
import { XamarinProjectManager, ProjectType } from './xamarin-project-manager';

interface XamarinBuildTaskDefinition extends vscode.TaskDefinition {
	/**
	 * Additional build flags
	 */

	task: string;

	command:string;

	csproj:string;

	configuration:string;

	projectType:ProjectType;

	target:string;

	platform:string;

	flags?: string[];
}

export class XamarinBuildTaskProvider implements vscode.TaskProvider {
	static XamarinBuildScriptType: string = 'xamarin';
	private csproj:string;
	private configuration:string;
	private platform:string;
	
	// We use a CustomExecution task when state needs to be shared accross runs of the task or when 
	// the task requires use of some VS Code API to run.
	// If you don't need to share state between runs and if you don't need to execute VS Code API in your task, 
	// then a simple ShellExecution or ProcessExecution should be enough.
	// Since our build has this shared state, the CustomExecution is used below.
	private sharedState: string | undefined;

	constructor(private workspaceRoot: string){
		console.log(workspaceRoot);
	}

	public async provideTasks(): Promise<vscode.Task[]> {
		return this.getTasks();
	}

	public resolveTask(_task: vscode.Task): vscode.Task | undefined {
		return this.getTask(XamarinBuildTaskProvider.msBuildCommand,"Build",[]);
	}

	static msBuildCommand:string = "msbuild";

	private getTasks(): vscode.Task[] {
		
		if (!XamarinProjectManager.SelectedProject)
		{
			vscode.window.showInformationMessage("Startup Project not selected!");
			return undefined;
		}

		this.csproj = XamarinProjectManager.SelectedProject.Path;
		this.configuration = XamarinProjectManager.SelectedProjectConfiguration;
		this.platform = XamarinProjectManager.getSelectedProjectPlatform();

		var flags = [];
		var target = "Build";

		return [ this.getTask(XamarinBuildTaskProvider.msBuildCommand,target,flags) ]
	}

	private getTask(command:string ,target: string, flags: string[], definition?: XamarinBuildTaskDefinition): vscode.Task{
		var configuration = XamarinProjectManager.SelectedProjectConfiguration;
		var csproj = XamarinProjectManager.SelectedProject.Path;
		var platform = XamarinProjectManager.getSelectedProjectPlatform();
		var projectType = XamarinProjectManager.getProjectType(XamarinProjectManager.SelectedTargetFramework);
		if (definition === undefined) {
			definition = {
				task: "MSBuild",
				command,
				type: XamarinBuildTaskProvider.XamarinBuildScriptType,
				csproj,
				configuration,
				projectType,
				platform,
				target,
				flags
			};
		}

		var platformArg = '';
		if (this.platform)
			platformArg = `;Platform=${platform}`;

		var fullCommand = `${command} ${csproj} /t:${target} /p:Configuration=${configuration}${platformArg} ${flags.join(' ')}`;
		var task = new vscode.Task(definition, definition.target, 'xamarin', new vscode.ShellExecution(fullCommand));
		return task;
	}
}