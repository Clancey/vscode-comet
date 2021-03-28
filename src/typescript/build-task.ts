'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
//import {ProjectType } from './msbuild-project-analyzer';
import { MobileProjectManager, ProjectType } from './project-manager';
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

interface MobileBuildTaskDefinition extends vscode.TaskDefinition {
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

export class MobileBuildTaskProvider implements vscode.TaskProvider {
	static MobileBuildScriptType: string = 'comet';
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

		if (!MobileBuildTaskProvider.msBuildPromise) {
			MobileBuildTaskProvider.msBuildPromise = MobileBuildTaskProvider.locateMSBuildImpl();
		}
		return MobileBuildTaskProvider.msBuildPromise;
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
		
		var startupInfo = MobileProjectManager.Shared.StartupInfo;

		if (!startupInfo || !startupInfo.Project || !startupInfo.TargetFramework || !startupInfo.Configuration)
		{
			vscode.window.showErrorMessage("Startup Project not selected!");
			return undefined;
		}

		if (!startupInfo || !startupInfo.Device)
		{
			vscode.window.showErrorMessage("Startup Device not selected!");
			return undefined;
		}

		this.csproj = startupInfo.Project.Path;
		this.configuration = startupInfo.Configuration;
		this.platform = MobileProjectManager.getSelectedProjectPlatform();

		var flags = [];
		var command = "dotnet";
		
		// Use MSBuild for old projects
		if (!MobileProjectManager.getProjectIsCore(startupInfo.TargetFramework))
			command = await MobileBuildTaskProvider.locateMSBuild();

		return [
			this.getTask(command, "Build", flags),
			this.getTask(command, "Run", flags),
		]
	}

	private getTask(command:string ,target: string, flags: string[], definition?: MobileBuildTaskDefinition): vscode.Task{

		var startupInfo = MobileProjectManager.Shared.StartupInfo;

		var configuration = startupInfo.Configuration;
		var csproj = startupInfo.Project.Path;
		var tfm = startupInfo.TargetFramework;
		var platform = MobileProjectManager.getSelectedProjectPlatform();
		var isCore = MobileProjectManager.getProjectIsCore(tfm);
		var projectType = MobileProjectManager.getProjectType(tfm);
		
		var device = startupInfo.Device;

		if (definition === undefined) {
			definition = {
				task: "MSBuild",
				command,
				type: MobileBuildTaskProvider.MobileBuildScriptType,
				csproj,
				configuration,
				projectType,
				platform,
				target,
				flags
			};
		}

		var platformArg = '';
		if (this.platform && !isCore)
			platformArg = `;Platform=${platform}`;

		var msbuildTarget = 'Run';

		if (projectType == ProjectType.iOS || projectType == ProjectType.MacCatalyst)
			msbuildTarget = 'Build';
		
		if (projectType == ProjectType.Android && !isCore)
			msbuildTarget = 'Install;_Run';

		var args = [csproj, `-t:${msbuildTarget}`, `-p:Configuration=${configuration}${platformArg}`];

		// dotnet needs the build verb
		if (isCore) {
			// TODO: THIS IS A HACK THAT CAN BE REMOVED IN .NET 6 P3
			args.unshift("--no-restore");

			args.unshift("build");
			
			if (tfm)
				args.push(`-p:TargetFramework=${tfm}`);
		}

		if (configuration.toLowerCase() === "debug")
		{
			if (projectType == ProjectType.Android)
			{
				var port = startupInfo.DebugPort;

				args.push('-p:AndroidAttachDebugger=true');
				args.push(`-p:AdbTarget=-s%20${device.serial}`);
				args.push(`-p:AndroidSdbTargetPort=${port}`);
				args.push(`-p:AndroidSdbHostPort=${port}`);
			}
		}

		if (projectType == ProjectType.iOS)
		{
			//:v2:udid=6415F4E9-CE0F-455B-ACD0-F81305FB9920
			// --device=:v2:runtime={options.iOSSimulatorDevice},devicetype={options.iOSSimulatorDeviceType}
			//if (device.iosSimulatorDevice)
			//	args.push(`-p:_DeviceName=:v2:udid=${device.iosSimulatorDevice.udid}`);
		}

		args.concat(flags);
		var task = new vscode.Task(definition, definition.target, MobileBuildTaskProvider.MobileBuildScriptType, new vscode.ProcessExecution(command, args),
			"$msCompile");
		return task;
	}
}