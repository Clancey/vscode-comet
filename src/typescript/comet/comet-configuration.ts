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
    private currentProjectDisplay:string;
    private currentConfigurations:string[];
    private currentPlatforms:string[];
    private currentConfig:string;
    private currentProjectType:ProjectType = ProjectType.Unknown;
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
        if(this.currentProjectDisplay == null){
            this.statusBarItem.hide();
            return;
        }

        this.statusBarItem.text = (this.hasComet == true ? "☄️"  : "") + ` ${this.currentProjectDisplay} | ${this.currentConfig}`;
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
            this.currentProjectType = analyzer.getProjectType();
            //TODO: do a better check based on current os
            if(this.currentProjectType == ProjectType.Unknown)
            { 
                this.currentProjectDisplay = null;
                vscode.window.showInformationMessage("Unsupported Project");
                return;
            }
            this.currentProjectDisplay = analyzer.getProjectName();
            this.currentConfigurations = analyzer.getConfigurationNames();
            this.currentConfig = this.currentConfigurations.find(x=> x.includes("Debug"));
            
            if(this.currentConfig == null) 
                this.currentConfig = this.currentConfigurations[0];

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
            this.currentProjectDisplay = null;
        }

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
    var hasCommet =  nugets.find(e=> e.includes("Comet.Reload")) != null;
    var references = analyzer.getReferences();
    config.hasComet = hasCommet;

    return config;
}