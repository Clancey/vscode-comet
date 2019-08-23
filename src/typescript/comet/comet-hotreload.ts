import { DiagnosticCollection, DiagnosticSeverity, ExtensionContext, commands, workspace } from "vscode";

import {CometDebugger} from "./comet-debuger";

export function setUpHotReload(context: ExtensionContext)
{
    let hotReloadDelayTimer: NodeJS.Timer;

    context.subscriptions.push(workspace.onDidSaveTextDocument((td) => {
		// Debounce to avoid reloading multiple times during multi-file-save (Save All).
		// Hopefully we can improve in future: https://github.com/Microsoft/vscode/issues/42913
		if (hotReloadDelayTimer) {
			clearTimeout(hotReloadDelayTimer);
		}

		hotReloadDelayTimer = setTimeout(() => {
            hotReloadDelayTimer = null;
            CometDebugger.Shared.SendDocumentChanged(td.fileName, td.getText());
		}, 200);
    }));
    
    context.subscriptions.push(workspace.onDidChangeTextDocument((td) => {


		// Debounce to avoid reloading multiple times during multi-file-save (Save All).
		// Hopefully we can improve in future: https://github.com/Microsoft/vscode/issues/42913
		if (hotReloadDelayTimer) {
			clearTimeout(hotReloadDelayTimer);
		}

		hotReloadDelayTimer = setTimeout(() => {
            hotReloadDelayTimer = null;
            CometDebugger.Shared.SendDocumentChanged(td.document.fileName, td.document.getText());
		}, 800);
	}));
}

