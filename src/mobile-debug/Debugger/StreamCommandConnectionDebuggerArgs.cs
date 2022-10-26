﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;
using Mono.Debugger.Soft;


namespace VSCodeDebug.Debugger;

class StreamCommandConnectionDebuggerArgs : SoftDebuggerStartArgs, ISoftDebuggerConnectionProvider {
	readonly string appName;

	public StreamCommandConnectionDebuggerArgs (string appName, StreamCommandConnection commandConnection)
	{
		this.appName = appName;
		CommandConnection = commandConnection;
	}

	public StreamCommandConnection CommandConnection {
		get; private set;
	}

	public override ISoftDebuggerConnectionProvider ConnectionProvider {
		get { return this; }
	}

	IAsyncResult ISoftDebuggerConnectionProvider.BeginConnect (DebuggerStartInfo dsi, AsyncCallback callback)
	{
		return CommandConnection.BeginStartDebugger (callback, null);
	}

	void ISoftDebuggerConnectionProvider.EndConnect (IAsyncResult result, out VirtualMachine vm, out string appName)
	{
		appName = this.appName;

		Stream transport, output;
		CommandConnection.EndStartDebugger (result, out transport, out output);
		var transportConnection = new IPhoneTransportConnection (
									  CommandConnection,
									  transport);
		var outputReader = new StreamReader (output);
		vm = VirtualMachineManager.Connect (transportConnection, outputReader, null);

		
	}

	void ISoftDebuggerConnectionProvider.CancelConnect (IAsyncResult result)
	{
		CommandConnection.CancelStartDebugger (result);
	}

	bool ISoftDebuggerConnectionProvider.ShouldRetryConnection (Exception ex)
	{
		return false;
	}
}
