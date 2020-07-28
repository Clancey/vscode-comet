import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';

export class XamarinConfigurationProvider implements vscode.DebugConfigurationProvider {

	constructor() {
	}

	async resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: vscode.CancellationToken): Promise<DebugConfiguration> {

		// if launch.json is missing or empty
		if (!config.type && !config.request && !config.name) {
			const editor = vscode.window.activeTextEditor;
			if (editor && editor.document.languageId === 'csharp') {
				config.type = 'xamarin';
				config.name = 'Debug';
				config.request = 'attach';
				// config.program = '${file}';
				// config.stopOnEntry = true;
			}
		}

		return config;
	}
}