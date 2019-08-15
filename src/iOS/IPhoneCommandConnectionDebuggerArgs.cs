using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;

namespace VSCodeDebug
{
	class IPhoneSimulatorDebuggerArgs : SoftDebuggerStartArgs, ISoftDebuggerConnectionProvider
	{
		IPhoneCommandConnection commandConnection;
		string appName;

		public string AppName {
			get { return appName; }
		}

		public IPhoneCommandConnection CommandConnection {
			get { return commandConnection; }
		}

		public IPhoneSimulatorDebuggerArgs(string appName, IPhoneCommandConnection commandConnection)
		{
			this.appName = appName;
			this.commandConnection = commandConnection;
		}

		public override ISoftDebuggerConnectionProvider ConnectionProvider {
			get { return this; }
		}

		IAsyncResult ISoftDebuggerConnectionProvider.BeginConnect(DebuggerStartInfo dsi, AsyncCallback callback)
		{
			return commandConnection.BeginStartDebugger(callback, null);
		}

		void ISoftDebuggerConnectionProvider.EndConnect(IAsyncResult result, out Mono.Debugger.Soft.VirtualMachine vm, out string appName)
		{
			appName = this.appName;

			Stream transport, output;
			commandConnection.EndStartDebugger(result, out transport, out output);
			var transportConnection = new StreamTransportConnection(transport);
			var outputReader = new StreamReader(output);
			vm = Mono.Debugger.Soft.VirtualMachineManager.Connect(transportConnection, outputReader, null);
		}

		void ISoftDebuggerConnectionProvider.CancelConnect(IAsyncResult result)
		{
			commandConnection.CancelStartDebugger(result);
		}

		bool ISoftDebuggerConnectionProvider.ShouldRetryConnection(Exception ex)
		{
			return false;
		}
	}
}
