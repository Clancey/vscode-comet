/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';

import * as vscode from 'vscode';
import { CometProjectManager, getConfiguration } from './comet/comet-configuration';
import {CometDebugger} from './comet/comet-debuger';



const cometManager = new CometProjectManager();
let cometDebugger:CometDebugger;


export function activate(context: vscode.ExtensionContext) {
	console.log("Activated comet!!!");
	cometDebugger = new CometDebugger(context,cometManager);
	context.subscriptions.push(vscode.commands.registerCommand('extension.comet.setAsStartup',(e: vscode.Uri) =>{
		cometManager.SetCurentProject(e.path);

	}));
}

export function deactivate() {
	cometDebugger.dispose();
	cometDebugger = null;
}
