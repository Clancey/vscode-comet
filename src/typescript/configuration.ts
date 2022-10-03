import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import { MobileBuildTaskProvider } from './build-task';
import { MobileProjectManager, ProjectType } from './project-manager';

export class MobileConfiguration implements DebugConfiguration {
	[key: string]: any;
	type: string;
	name: string;
	request: string;

}

export class MobileConfigurationProvider implements vscode.DebugConfigurationProvider {

	constructor() {
	}

	async resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: vscode.CancellationToken): Promise<DebugConfiguration> {

		// No launch.json exists, let's help fill out a nice default
		if (!config.type && !config.request && !config.name)
		{
			config.type = MobileBuildTaskProvider.MobileBuildScriptType;
			config.request = 'launch';
			return config;
		}

		// if launch.json is missing or empty
		if (config.type == MobileBuildTaskProvider.MobileBuildScriptType) {
			
			if (!config.request)
				config.request = 'launch';

			var startupInfo = MobileProjectManager.Shared.StartupInfo;
			var project = startupInfo.Project;

			if (!project)
			{
				await MobileProjectManager.Shared.selectStartupProject(true);
				project = MobileProjectManager.Shared.StartupInfo.Project;
			}

			if (!project) {
				vscode.window.showErrorMessage("Startup Project not selected!");
				return undefined;
			}

			config['workspaceUri'] = folder.uri.fsPath;
			startupInfo = MobileProjectManager.Shared.StartupInfo;

			if (project) {

				if (!config['projectPath'])
					config['projectPath'] = project.Path;

				if (!config['projectConfiguration'])
					config['projectConfiguration'] = startupInfo.Configuration;

				var projectType = MobileProjectManager.getProjectType(startupInfo.TargetFramework);
				var projectIsCore = MobileProjectManager.getProjectIsCore(startupInfo.TargetFramework);
				var projectTfm = startupInfo.TargetFramework;

				config['projectType'] = projectType;
				config['projectIsCore'] = projectIsCore;
				config['projectTargetFramework'] = startupInfo.TargetFramework.FullName;
				config['projectPlatform'] = MobileProjectManager.getSelectedProjectPlatform();

				config['debugPort'] = startupInfo.DebugPort;

				var device = startupInfo.Device;

				if (!device)
				{ 
					await MobileProjectManager.Shared.showDevicePicker();
					device = MobileProjectManager.Shared?.StartupInfo?.Device;
				}

				if (!device) {
					vscode.window.showErrorMessage("Device not selected!");
					return undefined;
				}

				if (device) {
					if (projectType === ProjectType.Android) {
						if (device.isEmulator)
							config['adbEmulatorName'] = device.name;

					} else if (projectType === ProjectType.iOS) {
						
						if (device.isEmulator)
						{
							config['projectPlatform'] = 'iPhoneSimulator';
						}
						else
						{
							config['projectPlatform'] = 'iPhone';
						}
					}

					if (!config['device']) {
						if (device.serial)
							config['device'] = device.serial;
						else if (device.name)
							config['device'] = device.name;
					}

					config['devicePlatform'] = projectTfm.Platform;
				}
			}
		}

		return config;
	}
}
