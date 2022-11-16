interface CommandResponse<T> {
	id: string;
	command: string;
	error?: string;
	response?: T;
}

export interface SimpleResult {
	sucess: boolean;
}

export class DeviceData {
	name: string;
	details: string;
	serial: string;
	platforms: string[];
	version: string;
	isEmulator: boolean;
	isRunning: boolean;
	rid: string;
}

const path = require('path');
const execa = require('execa');

import * as vscode from 'vscode';
import { extensionId, extensionPath, mobileDebugPath } from './extensionInfo';

export class MobileUtil
{
	public UtilPath: string;

	isUnix: boolean = true;

	constructor()
	{
		var os = require('os');

		var plat = os.platform();

		if (plat.indexOf('win32') >= 0)
			this.isUnix = false;

		this.UtilPath = mobileDebugPath;
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

		proc = await execa.execa('dotnet', [ this.UtilPath ].concat(stdargs));

		var txt = proc['stdout'];

		var result = JSON.parse(txt) as CommandResponse<TResult>;
		if (result.error) {
			throw new Error(result.error);
		}		
		return result;
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
