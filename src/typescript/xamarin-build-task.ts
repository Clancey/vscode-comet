'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
//import {ProjectType } from './msbuild-project-analyzer';
import { XamarinProjectManager, ProjectType } from './xamarin-project-manager';
import * as child from 'child_process';

interface ExecResult {
    stdout: string,
    stderr: string
}

function execFileAsync(file: string, args?: string[]): Thenable<ExecResult> {

    return new Promise((resolve, reject) => {
        child.execFile(file, args, (error, stdout, stderr) => {
            if (error) {
                reject(error);
            }
            resolve({ stdout, stderr });
        });
    });
}

function existsAsync(path: string | Buffer): Promise<boolean> {

    return new Promise((resolve) => fs.exists(path, resolve));
}

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
	
	// We use a CustomExecution task when state needs to be shared across runs of the task or when 
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
		// All supported tasks are provided by getTasks. If that changes, the easiest
		// thing to do might be to move msbuild location into util, and launch util directly.
		return undefined;
	}

	static msBuildPromise:Promise<string>;

	private static locateMSBuild(): Promise<string> {

		if (!XamarinBuildTaskProvider.msBuildPromise) {
			XamarinBuildTaskProvider.msBuildPromise = XamarinBuildTaskProvider.locateMSBuildImpl();
		}
		return XamarinBuildTaskProvider.msBuildPromise;
	}

	private static async locateMSBuildImpl(): Promise<string> {

		if (process.platform !== "win32") {
			return "msbuild";
		}
	
		// msbuild isn't normally in the path on windows. Use vswhere to locate it.
		// https://github.com/microsoft/vswhere
		const progFiles = process.env["ProgramFiles(x86)"];
		const vswhere = path.join(progFiles, "Microsoft Visual Studio", "Installer", "vswhere.exe");
		if (!await existsAsync(vswhere)) {
			return "msbuild";
		}
	
		var findMSBuild = ["-latest", "-requires", "Microsoft.Component.MSBuild", "-find", "MSBuild\\**\\Bin\\MSBuild.exe"];
		var { stdout } = await execFileAsync(vswhere, findMSBuild);
		stdout = stdout.trim();
		if (stdout.length > 0) {
			return stdout;
		}
	
		findMSBuild.push("-preRelease");
		var { stdout } = await execFileAsync(vswhere, findMSBuild);
		stdout = stdout.trim();
		if (stdout.length > 0) {
			return stdout;
		} else {
			return "msbuild";
		}
	}

	private async getTasks(): Promise<vscode.Task[]> {
		
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
		var command = await XamarinBuildTaskProvider.locateMSBuild();

		return [ this.getTask(command,target,flags) ]
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

		var args = [csproj, `/t:${target}`, `/p:Configuration=${configuration}${platformArg}`];
		args.concat(flags);
		var task = new vscode.Task(definition, definition.target, 'xamarin', new vscode.ProcessExecution(command, args));
		return task;
	}
}