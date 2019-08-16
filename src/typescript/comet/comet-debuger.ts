/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import { DebugProtocol } from 'vscode-debugprotocol';
import * as nls from 'vscode-nls';

import { CometProjectManager, getConfiguration } from './comet-configuration';

const localize = nls.config(process.env.VSCODE_NLS_CONFIG)();

const configuration = vscode.workspace.getConfiguration('comet-debug');

export class CometDebugger implements vscode.Disposable {


    private currentDebugSession: vscode.DebugSession;


    constructor(context: vscode.ExtensionContext, cometManger: CometProjectManager) {
        context.subscriptions.push(vscode.commands.registerCommand('extension.comet-debug.configureExceptions', () => configureExceptions()));
        context.subscriptions.push(vscode.commands.registerCommand('extension.comet-debug.startSession', config => startSession(config)));
        // register a configuration provider for 'mock' debug type
        const provider = new CometConfigurationProvider();
        context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider('comet', provider));

        context.subscriptions.push(vscode.debug.onDidStartDebugSession(async (s) => {
            let type = s.type;

            if (type === "vslsShare") {
                const debugSessionInfo = await s.customRequest("debugSessionInfo");
                type = debugSessionInfo.configurationProperties.type;
            }

            if (type === "dart") {
                this.currentDebugSession = s;
                //this.resetFlutterSettings();
            }
        }));
        context.subscriptions.push(vscode.debug.onDidTerminateDebugSession((s) => {
            if (s === this.currentDebugSession) {
                this.currentDebugSession = null;
                // this.reloadStatus.hide();
                // this.debugMetrics.hide();
                const debugSessionEnd = new Date();
                // this.disableAllServiceExtensions();
            }
        }));
    }
    dispose() {

    }

}


//----- configureExceptions ---------------------------------------------------------------------------------------------------

// we store the exception configuration in the workspace or user settings as
type ExceptionConfigurations = { [exception: string]: DebugProtocol.ExceptionBreakMode; };

// if the user has not configured anything, we populate the exception configurationwith these defaults
const DEFAULT_EXCEPTIONS: ExceptionConfigurations = {
    "System.Exception": "never",
    "System.SystemException": "never",
    "System.ArithmeticException": "never",
    "System.ArrayTypeMismatchException": "never",
    "System.DivideByZeroException": "never",
    "System.IndexOutOfRangeException": "never",
    "System.InvalidCastException": "never",
    "System.NullReferenceException": "never",
    "System.OutOfMemoryException": "never",
    "System.OverflowException": "never",
    "System.StackOverflowException": "never",
    "System.TypeInitializationException": "never"
};

class BreakOptionItem implements vscode.QuickPickItem {
    label: string;
    description: string;
    breakMode: DebugProtocol.ExceptionBreakMode
}

// the possible exception options converted into QuickPickItem
const OPTIONS = ['never', 'always', 'unhandled'].map<BreakOptionItem>((bm: DebugProtocol.ExceptionBreakMode) => {
    return {
        label: translate(bm),
        description: '',
        breakMode: bm
    }
});

class ExceptionItem implements vscode.QuickPickItem {
    label: string;
    description: string;
    model: DebugProtocol.ExceptionOptions
}

function translate(mode: DebugProtocol.ExceptionBreakMode): string {
    switch (mode) {
        case 'never':
            return localize('breakmode.never', "Never break");
        case 'always':
            return localize('breakmode.always', "Always break");
        case 'unhandled':
            return localize('breakmode.unhandled', "Break when unhandled");
        default:
            return mode;
    }
}

function getModel(): ExceptionConfigurations {
    let model = DEFAULT_EXCEPTIONS;
    if (configuration) {
        const exceptionOptions = configuration.get('exceptionOptions');
        if (exceptionOptions) {
            model = <ExceptionConfigurations>exceptionOptions;
        }
    }
    return model;
}

function configureExceptions(): void {

    const options: vscode.QuickPickOptions = {
        placeHolder: localize('select.exception', "First Select Exception"),
        matchOnDescription: true,
        matchOnDetail: true
    };

    const exceptionItems: vscode.QuickPickItem[] = [];
    const model = getModel();
    for (var exception in model) {
        exceptionItems.push({
            label: exception,
            description: model[exception] !== 'never' ? `⚡ ${translate(model[exception])}` : ''
        });
    }

    vscode.window.showQuickPick(exceptionItems, options).then(exceptionItem => {

        if (exceptionItem) {

            const options: vscode.QuickPickOptions = {
                placeHolder: localize('select.break.option', "Then Select Break Option"),
                matchOnDescription: true,
                matchOnDetail: true
            };

            vscode.window.showQuickPick(OPTIONS, options).then(item => {
                if (item) {
                    model[exceptionItem.label] = item.breakMode;
                    if (configuration) {
                        configuration.update('exceptionOptions', model);
                    }
                    setExceptionBreakpoints(model);
                }
            });
        }
    });
}

function setExceptionBreakpoints(model: ExceptionConfigurations): Thenable<DebugProtocol.SetExceptionBreakpointsResponse> {

    const args: DebugProtocol.SetExceptionBreakpointsArguments = {
        filters: [],
        exceptionOptions: convertToExceptionOptions(model)
    }

    return vscode.commands.executeCommand<DebugProtocol.SetExceptionBreakpointsResponse>('workbench.customDebugRequest', 'setExceptionBreakpoints', args);
}


function convertToExceptionOptions(model: ExceptionConfigurations): DebugProtocol.ExceptionOptions[] {

    const exceptionItems: DebugProtocol.ExceptionOptions[] = [];
    for (var exception in model) {
        exceptionItems.push({
            path: [{ names: [exception] }],
            breakMode: model[exception]
        });
    }
    return exceptionItems;
}

//----- configureExceptions ---------------------------------------------------------------------------------------------------

/**
 * The result type of the startSession command.
 */
class StartSessionResult {
    status: 'ok' | 'initialConfiguration' | 'saveConfiguration';
    content?: string;	// launch.json content for 'save'
};

function startSession(config: any): StartSessionResult {

    if (config && !config.__exceptionOptions) {
        config.__exceptionOptions = convertToExceptionOptions(getModel());
    }

    vscode.commands.executeCommand('vscode.startDebug', config);

    return {
        status: 'ok'
    };
}

class CometConfigurationProvider implements vscode.DebugConfigurationProvider {


	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */

    // 	if (!folder || !folder.uri) {
    //         vscode.window.showErrorMessage("Cannot create .NET debug configurations. No workspace folder was selected.");
    //         return [];
    //     }

    //     try
    //     {
    //         let hasWorkspaceMatches : boolean = await this.checkWorkspaceInformationMatchesWorkspaceFolder(folder); 
    //         if (!hasWorkspaceMatches) {
    //             vscode.window.showErrorMessage(`Cannot create .NET debug configurations. The active C# project is not within folder '${folder.uri.fsPath}'.`);
    //             return [];
    //         }

    //         let info: WorkspaceInformationResponse = await serverUtils.requestWorkspaceInformation(this.server);           

    //         const generator = new AssetGenerator(info, folder);
    //         if (generator.hasExecutableProjects()) {

    //             if (!await generator.selectStartupProject())
    //             {
    //                 return [];
    //             }

    //             // Make sure .vscode folder exists, addTasksJsonIfNecessary will fail to create tasks.json if the folder does not exist. 
    //             await fs.ensureDir(generator.vscodeFolder);

    //             // Add a tasks.json
    //             const buildOperations : AssetOperations = await getBuildOperations(generator);
    //             await addTasksJsonIfNecessary(generator, buildOperations);

    //             const isWebProject = generator.hasWebServerDependency();
    //             const launchJson: string = generator.createLaunchJson(isWebProject);

    //             // jsonc-parser's parse function parses a JSON string with comments into a JSON object. However, this removes the comments. 
    //             return parse(launchJson);

    //         } else {               
    //             // Error to be caught in the .catch() below to write default C# configurations
    //             throw new Error("Does not contain .NET Core projects.");
    //         }
    //     }
    //     catch
    //     {
    //         // Provider will always create an launch.json file. Providing default C# configurations.
    //         // jsonc-parser's parse to convert to JSON object without comments. 
    //         return [
    //             createFallbackLaunchConfiguration(),                  
    //             parse(createAttachConfiguration())
    //         ];
    //     }

    // }
    //resolveDebugConfiguration?(folder: WorkspaceFolder | undefined, debugConfiguration: DebugConfiguration, token?: CancellationToken): ProviderResult<DebugConfiguration>;
    async resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: vscode.CancellationToken): Promise<DebugConfiguration> {

        // if launch.json is missing or empty
        if (!config.type && !config.request && !config.name) {
            const editor = vscode.window.activeTextEditor;
            if (editor && editor.document.languageId === 'markdown') {
                config.type = 'comet';
                // config.name = 'Launch';
                // config.request = 'launch';
                // config.program = '${file}';
                // config.stopOnEntry = true;
            }
        }

        if (!config.csproj) {
            return vscode.window.showInformationMessage("csproj is not set").then(_ => {
                return undefined;	// abort launch
            });
        }
        console.log("Parsing CSProj");
        var cometConfig = await getConfiguration(config);//.then((cometConfig) =>{
        cometConfig.Hi = "From VSCode";
        return cometConfig;
    }
}