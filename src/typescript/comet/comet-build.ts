'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
import { CometProjectManager} from './comet-configuration';
import {ProjectType } from './msbuild-project-analyzer';

interface CometBuildTaskDefinition extends vscode.TaskDefinition {
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

export class CometBuildTaskProvider implements vscode.TaskProvider {
	static CometBuildScriptType: string = 'comet';
	private csproj:string;
	private configuration:string;
	private platform:string;

	private tasks: vscode.Task[] | undefined;
	
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
		
			return this.getTask(CometBuildTaskProvider.msBuildCommand,"Build",[]);
	}
	static msBuildCommand:string = "msbuild";

	private getTasks(): vscode.Task[] {
		
		if(CometProjectManager.Shared().CurrentCSProj() === this.csproj
		 && CometProjectManager.CurrentConfig() === this.configuration
		  && CometProjectManager.CurrentPlatform()=== this.platform  
		  && this.tasks !== undefined && this.tasks.length > 0)
		 	return this.tasks;

		
		this.csproj = CometProjectManager.Shared().CurrentCSProj();
		if(this.csproj === undefined)
		{

			vscode.window.showInformationMessage("csproj is not set");
			return undefined;
		}
		this.configuration = CometProjectManager.CurrentConfig();
		this.platform = CometProjectManager.CurrentPlatform();

		this.tasks = [];
		this.tasks.push(this.getTask(CometBuildTaskProvider.msBuildCommand,"Build",[]));
		if(CometProjectManager.CurrentProjectType() === ProjectType.Android)
		{
			//TODO: target install
			//TODO: Target: _run
		}
	
		return this.tasks;
	}

	private getTask(command:string ,target: string, flags: string[], definition?: CometBuildTaskDefinition): vscode.Task{
		var configuration = CometProjectManager.CurrentConfig();
		var csproj = CometProjectManager.Shared().CurrentCSProj();
		var platform = CometProjectManager.CurrentPlatform();
		if (definition === undefined) {
			definition = {
				task: "MSBuild",
				command,
				type: CometBuildTaskProvider.CometBuildScriptType,
				csproj,
				configuration,
				projectType: CometProjectManager.CurrentProjectType(),
				platform,
				target,
				flags
			};
		}


		var task = new vscode.Task(definition, definition.target, 'comet', new vscode.ShellExecution(`${command} ${csproj} /t:${target} /p:Configuration=${configuration};Platform=${platform} ${flags.join(' ')}`));
		return task;
	}
}