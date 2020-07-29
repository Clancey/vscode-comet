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
	serial: string;
	platform: string;
	version: string;
	isEmulator: boolean;
	isRunning: boolean;
	iosSimulatorDevice?: SimCtlDevice;
}

const path = require('path');
const execa = require('execa');

import * as vscode from 'vscode';
import { LookupOneOptions } from 'dns';

export class XamarinUtil
{
	public UtilPath: string;

	constructor()
	{
		var thisExtension = vscode.extensions.getExtension('ms-vscode.xamarin');

		var extPath = thisExtension.extensionPath;

		this.UtilPath = path.join(extPath, 'src', 'xamarin-util', 'bin', 'Debug', 'netcoreapp3.1', 'xamarin-util.dll');
	}
	
	async RunCommand<TResult>(cmd: string, args: string[] = null)
	{
		var stdargs = [`-c=${cmd}`];
		
		if (args && args.length > 0)
		{
			for (var a in args)
				stdargs.push(a);
		}

		var proc = await execa('dotnet', [ this.UtilPath ].concat(stdargs));
		var txt = proc['stdout'];

		return JSON.parse(txt) as CommandResponse<TResult>;
	}

	public async Debug(jsonConfig: string): Promise<SimpleResult>
	{
		var proc = await execa('dotnet', [ this.UtilPath, `-c=debug` ], { input: jsonConfig + '\r\n' });

		var txt = proc['stdout'];

		var result = JSON.parse(txt) as CommandResponse<SimpleResult>;

		return result.response;
	}

	public async GetAndroidDevices()
	{
		var r = await this.RunCommand<Array<DeviceData>>("android-devices");
		return r.response;
	}

	public async GetiOSDevices()
	{
		var r = await this.RunCommand<AppleDevicesAndSimulators>("ios-devices");
		return r.response;
	}

	public async GetDevices()
	{
		var r = await this.RunCommand<Array<DeviceData>>("devices");
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
