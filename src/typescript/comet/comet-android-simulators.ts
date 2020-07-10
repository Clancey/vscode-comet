import * as _ from "lodash";
import * as cp from 'child_process';
import * as vscode from 'vscode';
import * as util from "./services/utils";
let fs = require("fs");
let path = require('path')
import {ProjectType } from './msbuild-project-analyzer';
import { ISimulatorVersion, SimVersion } from "./comet-ios-simulator-analyzer";


interface AndroidDevice
{
    Name : string;
    Serial : string;
    IsEmulator : boolean;
    IsRunning : boolean;
}

export class CometAndroidSimulatorAnalyzer {

    private Simulators: ISimulatorVersion[];
    public async GetSimulators(): Promise<ISimulatorVersion[]>
    {
        if(this.Simulators === undefined)
            await this.RefreshSimulators();
        return this.Simulators;
    }
    private jsonFile: string;
    private parsedXml: AndroidDevice[];
    constructor(storagePath: string, extensionPath: string) {
        if (!fs.existsSync(storagePath)) {
            fs.mkdirSync(storagePath);
        }
        this.jsonFile = path.join(storagePath, 'androidDevices.json');
        this.executablePath = path.join(extensionPath,CometAndroidSimulatorAnalyzer.mLaunchPath);
    }
    static mLaunchPath: string = "./bin/Debug/AndroidDevices.exe";
    executablePath: string;
    public async RefreshSimulators(): Promise<void> {

        var stdOut:string;
        try {
            var result = await exec(`${this.executablePath} "${this.jsonFile}"`, {});
            stdOut=result.stdout;

            if(!fs.existsSync(this.jsonFile)){
                //tODO: lets get a better error message, maybe see if the Android SDK is installed?
                vscode.window.showErrorMessage("Unknown Error getting Android Devices.");
                return;
            }
            var projXml = fs.readFileSync(this.jsonFile);
            if(projXml == null || projXml==''){
                //tODO: lets get a better error message, maybe see if the Android SDK is installed?
                vscode.window.showErrorMessage("Unknown Error getting Android Devices");
                return;
            }
            this.parsedXml = <AndroidDevice[]>JSON.parse(projXml);

            this.Simulators = this.getAvailableDevices();
        }
        catch (ex) {
            this.Simulators = undefined;
            //console.error(ex);
            vscode.window.showErrorMessage(`Listing Android devices didnt work :( ${ex.stderr}: `);
        }
    }

    
    private getAvailableDevices(): ISimulatorVersion[] {

        return this.parsedXml.map(x=> ({
            id : x.Serial,
            name : x.Name,
            projectType : ProjectType.Android,
        }));
    }

}

function exec(command: string, options: cp.ExecOptions): Promise<{ stdout: string; stderr: string }> {
    return new Promise<{ stdout: string; stderr: string }>((resolve, reject) => {
        if(!util.isWin)
            command = command = `mono ${command}`
        cp.exec(command, options, (error, stdout, stderr) => {
            if (error) {
                reject({ error, stdout, stderr });
            }
            resolve({ stdout, stderr });
        });
    });
}
