import * as vscode from 'vscode';
const path = require('path');

export const dotnetVersion = "net7.0";
export const mobileBuildScriptType = "comet";
export const omnisharpExtensionId = "ms-dotnettools.csharp";
export const extensionConfigurationKey = "comet-debug";
export const extensionId = "Clancey.comet-debug";
export const outputChanelName = "Comet for .NET Mobile";
export const extensionPath = vscode.extensions.getExtension(extensionId).extensionPath;
export const configureExtensionCommand = "extension.comet.configureExceptions";
export const startSessionCommand = "extension.comet.startSession";

const isProduction = process.env.NODE_ENV === "production";
const dotnetConfiguration = isProduction ? "Release" : "Debug";
export const analyzerExePath = path.join(extensionPath, 'src', 'DotNetWorkspaceAnalyzer', 'bin', dotnetConfiguration, dotnetVersion, 'DotNetWorkspaceAnalyzer.dll');
export const mobileDebugPath = path.join(extensionPath, 'src', 'mobile-debug', 'bin', dotnetConfiguration, dotnetVersion, 'mobile-debug.dll');