/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

'use strict';
let fs = require("fs");
let path = require('path')
import * as vscode from 'vscode';
import { ProjectType, MsBuildProjectAnalyzer } from './msbuild-project-analyzer';
import {CometiOSSimulatorAnalyzer} from './comet-ios-simulator-analyzer';


export interface IProjects {
    name: string;
    path: string;
    type: ProjectType;
    configurations:string[];
    platforms:string[];
}

export class CometProjectManager implements vscode.Disposable {

    static mLaunchPath: string = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
    public static IsXamarinIosInstalled(): boolean {
        return fs.existsSync(this.mLaunchPath);
    }
    private subscriptions: vscode.Disposable[] = [];
    public static Shared(): CometProjectManager { return CometProjectManager.shared };
    private static shared: CometProjectManager;
    private currentProjectPath: string;
    public CurrentCSProj(): string { return this.currentProjectPath; }
    private static currentProjectDisplay: string;
    public static CurrentProjectDisplay(): string { return CometProjectManager.currentProjectDisplay; }
    private static currentConfigurations: string[];
    private currentPlatforms: string[];
    private static currentConfig: string;
    public static CurrentConfig(): string { return CometProjectManager.currentConfig; }
    private static currentPlatform: string = "iPhoneSimulator";
    public static CurrentPlatform(): string { return CometProjectManager.currentPlatform; }
    private static currentProjectType: ProjectType = ProjectType.Unknown;
    public static CurrentProjectType(): ProjectType { return CometProjectManager.currentProjectType; }
    
    private simulatorAnalyzer:CometiOSSimulatorAnalyzer;
    
    private hasComet: boolean;
    private statusBarItem: vscode.StatusBarItem;

    public async GetProjects(): Promise<IProjects[]>{
        if(this.foundProjects.length > 0)
            return this.foundProjects;
        await this.FindProjectsInWorkspace();
        return this.foundProjects;
    }
    private foundProjects: IProjects[] = [];

    constructor(context: vscode.ExtensionContext) {
        CometProjectManager.shared = this;
        this.statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
        this.statusBarItem.tooltip = "Comet";
        this.statusBarItem.text = "☄️";
        
        this.subscriptions.push(vscode.commands.registerCommand("comet.selectProject", this.showProjectPicker, this));
        this.subscriptions.push(vscode.commands.registerCommand("comet.selectDevice", this.showProjectPicker, this));
        this.subscriptions.push(vscode.commands.registerCommand("comet.refreshiOSSimulators", this.showProjectPicker, this));
        let pattern = path.join(vscode.workspace.rootPath, '.csproj');
        let fileWatcher = vscode.workspace.createFileSystemWatcher(pattern);
        fileWatcher.onDidChange((e: vscode.Uri) => {
            console.log(e);
            this.foundProjects = [];
        });
        fileWatcher.onDidDelete((e: vscode.Uri) => {
            console.log(e);
            this.foundProjects = [];
        });
        fileWatcher.onDidCreate((e: vscode.Uri) => {
            console.log(e);
            this.foundProjects = [];
        });
        this.simulatorAnalyzer = new CometiOSSimulatorAnalyzer(context.storagePath);
        this.simulatorAnalyzer.RefreshSimulators();
        
        this.subscriptions.push(fileWatcher);
        this.FindProjectsInWorkspace();
        this.updateStatus();
    }

    public async SetCurentProject(project: string) {
        this.currentProjectPath = project;
        await this.parseProject(project);
        this.updateStatus();
    }

    updateStatus() {
        // if (CometProjectManager.currentProjectDisplay == null) {
        //     this.statusBarItem.hide();
        //     return;
        // }
        
        var projectString = this.currentProjectPath === undefined ? " Select a CSProj" : ` ${CometProjectManager.currentProjectDisplay} | ${CometProjectManager.currentConfig}`;
        
        //this.statusBarItem.text = (this.hasComet == true ? "☄️" : "") + projectString;
        
        this.statusBarItem.text =  "☄️" + projectString;
        this.statusBarItem.command = "comet.selectProject";
        this.statusBarItem.show();
    }

    public async showProjectPicker(): Promise<void> {
        var projects = (await this.GetProjects())
                .map(x=> ({
                    //description: x.type.toString(),
                    label: x.name,
                    project: x,
                }));
		 const p = await vscode.window.showQuickPick(projects, { placeHolder: "Select a device to use" });
		if (p) {
			this.SetCurentProject(p.project.path);
        }
        const c = await vscode.window.showQuickPick(p.project.configurations, { placeHolder: "Select a device to use" });
        if(c)
            CometProjectManager.currentConfig = c;
            this.updateStatus();
    }
    
    private isProjectSupported(type:ProjectType) : boolean
    {
        return type === ProjectType.iOS;
    }


    async parseProject(project: string) {
        try {
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
            if (CometProjectManager.currentProjectType == ProjectType.Unknown) {
                CometProjectManager.currentProjectDisplay = null;
                vscode.window.showInformationMessage("Unsupported Project");
                return;
            }
            CometProjectManager.currentProjectDisplay = analyzer.getProjectName();
            CometProjectManager.currentConfigurations = analyzer.getConfigurationNames();
            CometProjectManager.currentConfig = CometProjectManager.currentConfigurations.find(x => x.includes("Debug"));

            if (CometProjectManager.currentConfig === null)
                CometProjectManager.currentConfig = CometProjectManager.currentConfigurations[0];

            this.currentPlatforms = analyzer.getPlatformNames();
            var nugets = analyzer.getPackageReferences();
            this.hasComet = nugets.find(e => e.includes("Comet")) != null;
            //If no nuget check the project references
            if (!this.hasComet) {
                this.hasComet = analyzer.getProjectReferences().find(x => x.includes("Comet")) != null;
            }
            var references = analyzer.getReferences();
        }
        catch (ex) {
            console.log(ex);
            CometProjectManager.currentProjectDisplay = null;
        }

    }

    async FindProjectsInWorkspace() {
        var projects = [];
        async function fromDir(startPath, filter ) : Promise<void>{
            if (!fs.existsSync(startPath)) {
                console.log("no dir ", startPath);
                return;
            }

            var files = fs.readdirSync(startPath);
            for (var i = 0; i < files.length; i++) {
                var filename = path.join(startPath, files[i]);
                var stat = fs.lstatSync(filename);
                if (stat.isDirectory()) {
                    await fromDir(filename, filter); //recurse
                }
                else if (filename.endsWith(filter)) {
                    projects.push(await parseProject(filename));
                };
            };
        };
        await fromDir(vscode.workspace.rootPath, ".csproj");
        this.foundProjects = projects.filter((x) => this.isProjectSupported(x.type));
        if(!this.currentProjectPath && this.foundProjects.length > 0)
            this.SetCurentProject(this.foundProjects[0].path);

    }

    public async ApplyConfiguration(config: any): Promise<any> {

        if (this.currentProjectPath == null) {
            if (config.csproj != null)
                return config;

            return vscode.window.showInformationMessage("csproj is not set").then(_ => {
                return undefined;	// abort launch
            });
        }

        config.hasComet = this.hasComet;

        if (!config.configuration) {
            config.configuration = CometProjectManager.currentConfig;
        }
        if (!config.platform) {
            config.platform = CometProjectManager.currentPlatform;
        }

        if (CometProjectManager.currentProjectType === ProjectType.iOS) {
            if (!config.csproj) {
                config.csproj = this.currentProjectPath
            }
        }
        return config;
    }

    dispose() {
        this.subscriptions.forEach((s) => s.dispose());
    }

}
export async function parseProject(csproj:string) : Promise<IProjects>{
    var proj = csproj;
    if (vscode.workspace.workspaceFolders !== undefined) { // is undefined if no folder is opened
        let workspaceRoot = vscode.workspace.workspaceFolders[0];
        proj = proj.replace('${workspaceRoot}', workspaceRoot.uri.path);
    }

    var projXml = fs.readFileSync(proj);

    var analyzer = new MsBuildProjectAnalyzer(projXml);
    await analyzer.analyze();
    var name = analyzer.getProjectName();
    return {
        name,
        path: proj,
        type: analyzer.getProjectType(),
        configurations: analyzer.getConfigurationNames(),
        platforms: analyzer.getPlatformNames()
    };

}
export async function getConfiguration(config: any): Promise<any> {

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
    var hasComet = nugets.find(e => e.includes("Comet.Reload")) != null;
    var references = analyzer.getReferences();
    config.hasComet = hasComet;

    return config;
}