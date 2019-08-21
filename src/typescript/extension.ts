/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';

import * as vscode from 'vscode';
import { CometProjectManager, getConfiguration } from './comet/comet-configuration';
import {CometDebugger} from './comet/comet-debuger';
import {CometBuildTaskProvider} from './comet/comet-build';
import {setUpHotReload} from "./comet/comet-hotreload";



let cometBuildTaskProvider: vscode.Disposable | undefined;
let cometManager:CometProjectManager;
let cometDebugger:CometDebugger;


export function activate(context: vscode.ExtensionContext) {
	console.log("Activated comet!!!");
	cometManager= new CometProjectManager(context);
	cometDebugger = new CometDebugger(context,cometManager);
	context.subscriptions.push(vscode.commands.registerCommand('extension.comet.setAsStartup',(e: vscode.Uri) =>{
		cometManager.SetCurentProject(e.path);

	}));
	cometBuildTaskProvider = vscode.tasks.registerTaskProvider(CometBuildTaskProvider.CometBuildScriptType, new CometBuildTaskProvider(vscode.workspace.rootPath));
	setUpHotReload(context);
}

export function deactivate() {
	cometDebugger.dispose();
	cometDebugger = null;
	cometBuildTaskProvider.dispose();
}
