import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import { XamarinProjectManager } from './xamarin-project-manager';

export class XamarinConfigurationProvider implements vscode.DebugConfigurationProvider {

	constructor() {
	}

	async resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: vscode.CancellationToken): Promise<DebugConfiguration> {

		// if launch.json is missing or empty
		if (config.type == 'xamarin') {
			// config.request = 'attach';

			if (XamarinProjectManager.SelectedProject !== undefined) {
			
				var project = XamarinProjectManager.SelectedProject;

				var device = XamarinProjectManager.SelectedDevice;

				if (!config['startupProject'])
					config['startupProject'] = project.Path;

				if (!config['device'])
				{
					if (device.serial)
						config['device'] = device.serial;
					else if (device.name)
						config['device'] = device.name;

					// config['ios-devicetypeidentifier'] = device.iosSimulatorDevice.deviceTypeIdentifier;
					// config['ios-runtimeidentifier'] = device.iosSimulatorDevice.runtime.identifier;

					config['platform'] = device.platform;
				}
			}
		}

		return config;
	}
}