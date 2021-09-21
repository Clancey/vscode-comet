interface CommandResponse<T> {
	id: string;
	command: string;
	error?: string;
	response?: T;
}

export interface SimpleResult {
	sucess: boolean;
}

export interface AppleDevicesAndSimulators {
	devices: DeviceData[];
	simulators: SimCtlDeviceType[];
}
export interface SimCtlRuntime {
	bundlePath: string;
	buildVersion: string;
	runtimeRoot: string;
	identifier: string;
	version: string;
	isAvailable: boolean;
	name: string;
}

export interface SimCtlDeviceType {

	minRuntimeVersion: number;
	bundlePath: string;
	maxRuntimeVersion: number;
	name: string;
	identifier: string;
	productFamily: string;
	devices: SimCtlDevice[];
}

export interface SimCtlDevice {
	dataPath: string;
	logPath: string;
	udid: string;
	isAvailable: boolean;
	deviceTypeIdentifier: string;
	state: string;
	name: string;
	availabilityError: string;
	deviceType: SimCtlDeviceType;
	runtime: SimCtlRuntime;
}

export class DeviceData {
	name: string;
	details: string;
	serial: string;
	platforms: string[];
	version: string;
	isEmulator: boolean;
	isRunning: boolean;
}

const path = require('path');
const execa = require('execa');

import * as vscode from 'vscode';
import { LookupOneOptions } from 'dns';
import { stringify } from 'querystring';

export class MobileUtil
{
	public UtilPath: string;

	isUnix: boolean = true;

	constructor()
	{
		var thisExtension = vscode.extensions.getExtension('Clancey.comet-debug');

		var os = require('os');

		var plat = os.platform();

		if (plat.indexOf('win32') >= 0)
			this.isUnix = false;

		var extPath = thisExtension.extensionPath;

		this.UtilPath = path.join(extPath, 'src', 'mobile-debug', 'bin', 'Debug', 'net6.0', 'mobile-debug.dll');
	}

	async RunCommand<TResult>(cmd: string, args: string[] = null)
	{
		
		var stdargs = [`util`, `-c=${cmd}`];
		
		if (args && args.length > 0)
		{
			for (var a in args)
				stdargs.push(args[a]);
		}

		var proc: any;

		if (this.isUnix)
			proc = await execa('dotnet', [ this.UtilPath ].concat(stdargs));
		else
			proc = await execa(this.UtilPath, stdargs);

		var txt = proc['stdout'];

		return JSON.parse(txt) as CommandResponse<TResult>;
	}

	public async Debug(jsonConfig: string): Promise<SimpleResult>
	{
		var proc: any;

		if (this.isUnix)
			proc = await execa('dotnet', [ this.UtilPath, `util`, `-c=debug` ], { input: jsonConfig + '\r\n' });
		else
			proc = await execa(this.UtilPath, [ `util`, `-c=debug` ], { input: jsonConfig + '\r\n' });

		var txt = proc['stdout'];

		var result = JSON.parse(txt) as CommandResponse<SimpleResult>;

		return result.response;
	}

	public async GetDevices(targetPlatformIdentifier: string)
	{
		var args = [ ];
		if (targetPlatformIdentifier)
			args = [ "-t=" + targetPlatformIdentifier ];

		var r = await this.RunCommand<Array<DeviceData>>("devices", args);
		return r.response;
	}

	public async StartAndroidEmulator(name: string)
	{
		var r = await this.RunCommand<SimpleResult>("android-start-emulator", [ name ]);
		return r.response;
	}

	public async DebugProject(config: any)
	{
		var r = await this.RunCommand<SimpleResult>("debug", [ config.toJSON() ]);
		return r.response;
	}
}
