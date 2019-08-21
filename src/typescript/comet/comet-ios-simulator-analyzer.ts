import * as _ from "lodash";
import * as cp from 'child_process';
let fs = require("fs");
let path = require('path')

import { parseString, selectPropertyPathItems } from "./xml-parsing";

export interface ISimulatorVersion {
    name: string;
    id: string;
    versions: { name: string, id: string }[];
}
export class CometiOSSimulatorAnalyzer {
    private xml: string;
    private parsedXml: any;
    constructor(storagePath: string) {
        this.xml = path.join(storagePath, 'iosSimulators.xml');
    }
    static mLaunchPath: string = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
    public async RefreshSimulators():Promise<void>{

        var result = await exec(`${CometiOSSimulatorAnalyzer.mLaunchPath} --listsim=${this.xml}`,{ } );
        this.parsedXml = await parseString(this.xml);
        console.log(this.parsedXml);
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
