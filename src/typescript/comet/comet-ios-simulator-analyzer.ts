import * as _ from "lodash";
import * as cp from 'child_process';
import * as vscode from 'vscode';
let fs = require("fs");
let path = require('path')

import { parseString, selectPropertyPathItems } from "./xml-parsing";

export interface ISimulatorVersion {
    name: string;
    id: string;
    versions: { name: string, id: string }[];
}

interface SimVersion {
    name: string;
    id: string;
    identifier: string;
}
export class CometiOSSimulatorAnalyzer {

    private Simulators: ISimulatorVersion[];
    public async GetSimulators(): Promise<ISimulatorVersion[]>
    {
        if(this.Simulators === undefined)
            await this.RefreshSimulators();
        return this.Simulators;
    }
    private xml: string;
    private parsedXml: any;
    constructor(storagePath: string) {
        if (!fs.existsSync(storagePath)) {
            fs.mkdirSync(storagePath);
        }
        this.xml = path.join(storagePath, 'iosSimulators.xml');
    }
    static mLaunchPath: string = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
    public async RefreshSimulators(): Promise<void> {

        var stdOut:string;
        try {
            var result = await exec(`${CometiOSSimulatorAnalyzer.mLaunchPath} --listsim="${this.xml}"`, {});
            stdOut=result.stdout;

            if(!fs.existsSync(this.xml)){
                vscode.window.showErrorMessage("Please verify your Xamarin.iOS installation is correct.");
                return;
            }
            var projXml = fs.readFileSync(this.xml);
            if(projXml == null || projXml==''){
                vscode.window.showErrorMessage("Please verify your Xamarin.iOS installation is correct.");
                return;
            }
            this.parsedXml = await parseString(projXml);

            this.Simulators = this.getAvailableDevices();
        }
        catch (ex) {
            this.Simulators = undefined;
            //console.error(ex);
            vscode.window.showErrorMessage(ex.stderr);
        }
    }

    private getVersions(): SimVersion[] {
        var runtimes = selectPropertyPathItems<any>(this.parsedXml, ["MTouch", "Simulator", "SupportedRuntimes", "SimRuntime"])
            .filter(x => x.Name[0].indexOf("iOS") > -1)
            .map(c => ({
                name: c.Name[0],
                id: c.Name[0].replace("iOS ", ""),
                identifier: c.Identifier[0],

            }));
        return runtimes;
    }
    private getAvailableDevices(): ISimulatorVersion[] {
        var versions = this.getVersions();
        var versionsMap: { [identifier: string]: { name: string, id: string }; };
        versionsMap = _.keyBy(versions, 'identifier');// versions.map(x=> [x.identifier] = {name:x.name,id:x.version})

        var devices = selectPropertyPathItems<any>(this.parsedXml, ["MTouch", "Simulator", "AvailableDevices", "SimDevice"])
            .filter(x => x.SimRuntime[0].indexOf("iOS") > -1)
            .map(c => ({
                name: c.$.Name,
                runtime: c.SimRuntime[0],
                type: c.SimDeviceType[0].replace("com.apple.CoreSimulator.SimDeviceType.", ""),
            }));

        var groupedDevices = _(devices).groupBy(c => c.type).map((runtimes, type) => ({ type, runtimes })).value();
        return groupedDevices.map(c => ({
            id: c.type,
            name: c.runtimes[0].name,
            versions: c.runtimes.map(x => versionsMap[x.runtime]),
        }));
    }

}

function exec(command: string, options: cp.ExecOptions): Promise<{ stdout: string; stderr: string }> {
    return new Promise<{ stdout: string; stderr: string }>((resolve, reject) => {
        cp.exec(command, options, (error, stdout, stderr) => {
            if (error) {
                reject({ error, stdout, stderr });
            }
            resolve({ stdout, stderr });
        });
    });
}
