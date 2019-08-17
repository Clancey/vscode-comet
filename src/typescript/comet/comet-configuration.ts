/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
import {ProjectType, MsBuildProjectAnalyzer } from './msbuild-project-analyzer';


export class CometProjectManager implements vscode.Disposable {
    
    private currentProjectPath:string;
    private static currentProjectDisplay:string;
    public static CurrentProjectDisplay():string {return CometProjectManager.currentProjectDisplay;}
    private static currentConfigurations:string[];
    private currentPlatforms:string[];
    private static currentConfig:string;
    public static CurrentConfig():string {return CometProjectManager.currentConfig;}
    private static currentPlatform:string = "iPhoneSimulator";
    public static CurrentPlatform():string {return CometProjectManager.currentPlatform;}
    private static currentProjectType:ProjectType = ProjectType.Unknown;
    public static CurrentProjectType():ProjectType {return CometProjectManager.currentProjectType;}
    private hasComet:boolean;
    private statusBarItem: vscode.StatusBarItem;
    constructor(){
        this.statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
        this.statusBarItem.tooltip = "Comet";
        this.statusBarItem.text = "☄️";
    }

    public async SetCurentProject(project:string)
    {
        this.currentProjectPath = project;
        await this.parseProject(project);
        this.updateStatus();
    }
    updateStatus()
    {
        if(CometProjectManager.currentProjectDisplay == null){
            this.statusBarItem.hide();
            return;
        }

        this.statusBarItem.text = (this.hasComet == true ? "☄️"  : "") + ` ${CometProjectManager.currentProjectDisplay} | ${CometProjectManager.currentConfig}`;
        this.statusBarItem.show(); 
    }

    async parseProject(project:string) {
        try{
            var proj = project;

            if (vscode.workspace.workspaceFolders !== undefined) { // is undefined if no folder is opened
                let workspaceRoot = vscode.workspace.workspaceFolders[0];
                proj = proj.replace('${workspaceRoot}', workspaceRoot.uri.path);
            }
        
            var projXml = fs.readFileSync(proj);
            
            var analyzer = new MsBuildProjectAnalyzer(projXml);
            await analyzer.analyze();
            CometProjectManager.currentProjectType = analyzer.getProjectType();
            //TODO: do a better check based on current os
            if(CometProjectManager.currentProjectType == ProjectType.Unknown)
            { 
                CometProjectManager.currentProjectDisplay = null;
                vscode.window.showInformationMessage("Unsupported Project");
                return;
            }
            CometProjectManager.currentProjectDisplay = analyzer.getProjectName();
            CometProjectManager.currentConfigurations = analyzer.getConfigurationNames();
            CometProjectManager.currentConfig = CometProjectManager.currentConfigurations.find(x=> x.includes("Debug"));
            
            if(CometProjectManager.currentConfig === null) 
            CometProjectManager.currentConfig = CometProjectManager.currentConfigurations[0];

            this.currentPlatforms = analyzer.getPlatformNames();
            var nugets = analyzer.getPackageReferences();
            this.hasComet = nugets.find(e=> e.includes("Comet")) != null;
            //If no nuget check the project references
            if(!this.hasComet)
            {
                this.hasComet = analyzer.getProjectReferences().find(x=> x.includes("Comet")) != null;
            }
            var references = analyzer.getReferences();
        }
        catch(ex)
        {
            console.log(ex);
            CometProjectManager.currentProjectDisplay = null;
        }

    }
    public async ApplyConfiguration(config: any) : Promise<any>{

        if(this.currentProjectPath == null)
        {
            if(config.csproj != null)
                return config;

            return vscode.window.showInformationMessage("csproj is not set").then(_ => {
                return undefined;	// abort launch
            });
        }

        config.hasComet = this.hasComet;

        if(!config.configuration)
        {
            config.configuration = CometProjectManager.currentConfig;
        }
        if(!config.platform)
        {
            config.platform = CometProjectManager.currentPlatform;
        }

        if(CometProjectManager.currentProjectType === ProjectType.iOS)
        {
            if(!config.csproj)
            {
                config.csproj = this.currentProjectPath
            }
        }
        return config;
    }
    
    dispose() {
        
    }

}

export async function getConfiguration(config: any) : Promise<any>{

    var proj = config.csproj;

    if (vscode.workspace.workspaceFolders !== undefined) { // is undefined if no folder is opened
        let workspaceRoot = vscode.workspace.workspaceFolders[0];
        proj = proj.replace('${workspaceRoot}', workspaceRoot.uri.path);
    }

    var projXml = fs.readFileSync(proj);
     
    var analyzer = new MsBuildProjectAnalyzer(projXml);
    await analyzer.analyze();
    var projectName = analyzer.getProjectName();
    var configurations = analyzer.getConfigurationNames();
    var platforms = analyzer.getPlatformNames();
    var nugets = analyzer.getPackageReferences();
    var hasComet =  nugets.find(e=> e.includes("Comet.Reload")) != null;
    var references = analyzer.getReferences();
    config.hasComet = hasComet;

    return config;
}