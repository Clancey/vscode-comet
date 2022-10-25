import * as vscode from 'vscode';
const path = require('path');

export const omnisharpExtensionId = "ms-dotnettools.csharp";
export const extensionConfigurationKey = "comet-debug";
export const extensionId = "Clancey.comet-debug";
export const outputChanelName = "Comet for .NET Mobile";
export const extensionPath = vscode.extensions.getExtension(extensionId).extensionPath;
export const configureExtensionCommand = "extension.comet.configureExceptions";
export const startSessionCommand = "extension.comet.startSession";
export const analyzerExePath = path.join(extensionPath, 'src', 'DotNetWorkspaceAnalyzer', 'bin', 'Debug', 'net6.0', 'DotNetWorkspaceAnalyzer');
export const mobileDebugPath = path.join(extensionPath, 'src', 'mobile-debug', 'bin', 'Debug', 'net6.0', 'mobile-debug.dll');