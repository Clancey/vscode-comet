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
        if (!fs.existsSync(storagePath)){
            fs.mkdirSync(storagePath);
        }
        this.xml = path.join(storagePath, 'iosSimulators.xml');
    }
    static mLaunchPath: string = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
    public async RefreshSimulators():Promise<void>{

        try{
        var result = await exec(`${CometiOSSimulatorAnalyzer.mLaunchPath} --listsim="${this.xml}"`,{ } );


        var projXml = fs.readFileSync(this.xml);
        this.parsedXml = await parseString(projXml);
        console.log(this.parsedXml);
        }
        catch(ex)
        {
            console.exception(ex);
        }
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
