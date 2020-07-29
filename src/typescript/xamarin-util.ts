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

	isUnix: boolean = true;

	constructor()
	{
		var thisExtension = vscode.extensions.getExtension('ms-vscode.xamarin');

		var os = require('os');

		var plat = os.platform();

		if (plat.indexOf('win32') >= 0)
			this.isUnix = false;

		var extPath = thisExtension.extensionPath;

		this.UtilPath = path.join(extPath, 'src', 'xamarin-debug', 'bin', 'Debug', 'net472', 'xamarin-debug.exe');
	}

	async RunCommand<TResult>(cmd: string, args: string[] = null)
	{
		
		var stdargs = [`util`, `-c=${cmd}`];
		
		if (args && args.length > 0)
		{
			for (var a in args)
				stdargs.push(a);
		}

		var proc: any;

		if (this.isUnix)
			proc = await execa('mono', [ this.UtilPath ].concat(stdargs));
		else
			proc = await execa(this.UtilPath, stdargs);

		var txt = proc['stdout'];

		return JSON.parse(txt) as CommandResponse<TResult>;
	}

	public async Debug(jsonConfig: string): Promise<SimpleResult>
	{
		var proc: any;

		if (this.isUnix)
			proc = await execa('mono', [ this.UtilPath, `util`, `-c=debug` ], { input: jsonConfig + '\r\n' });
		else
			proc = await execa(this.UtilPath, [ `util`, `-c=debug` ], { input: jsonConfig + '\r\n' });

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
