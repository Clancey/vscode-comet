
interface CommandResponse<T> {
	id: string;
	command: string;
	error?: string;
	response?: T;
}

export interface DeviceData {
	name: string;
	serial: string;
	isEmulator: boolean;
	isRunning: boolean;
}

const path = require('path');
const execa = require('execa');

import * as vscode from 'vscode';

export class XamarinUtil
{
	public UtilPath: string;

	constructor()
	{
		var thisExtension = vscode.extensions.getExtension('ms-vscode.xamarin');

		var extPath = thisExtension.extensionPath;

		this.UtilPath = path.join(extPath, 'src', 'xamarin-util', 'bin', 'Release', 'netcoreapp3.1', 'xamarin-util.exe');
	}
	
	async RunCommand<TResult>(cmd: string, args: string[] = null)
	{
		var stdargs = [`-c=${cmd}`];
		
		if (args && args.length > 0)
		{
			for (var a in args)
				stdargs.push(a);
		}

		var proc = await execa(this.UtilPath, stdargs);
		var txt = proc['stdout'];

		return JSON.parse(txt) as CommandResponse<TResult>;
	}

	public async GetAndroidDevices()
	{
		var r = await this.RunCommand<Array<DeviceData>>("android-devices");
		return r.response;
	}
}
