/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace VSCodeDebug
{
	internal class Program
	{
		const int DEFAULT_PORT = 4711;

		private static bool trace_requests;
		private static bool trace_responses;
		static string LOG_FILE_PATH = null;

		static string msbuildBinDir = "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/msbuild/Current/bin";
		public static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}
		private static void Main(string[] argv)
		{
			if (IsRunningOnMono()) {
				AppDomain.CurrentDomain.AssemblyResolve += MSBuildAssemblyResolver;
				Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(msbuildBinDir, "MSBuild.dll"));
				Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(msbuildBinDir, "Sdks"));
			}
			int port = -1;

			// parse command line arguments
			foreach (var a in argv) {
				switch (a) {
				case "--trace":
					trace_requests = true;
					break;
				case "--trace=response":
					trace_requests = true;
					trace_responses = true;
					break;
				case "--server":
					port = DEFAULT_PORT;
					break;
				default:
					if (a.StartsWith("--server=")) {
						if (!int.TryParse(a.Substring("--server=".Length), out port)) {
							port = DEFAULT_PORT;
						}
					}
					else if( a.StartsWith("--log-file=")) {
						LOG_FILE_PATH = a.Substring("--log-file=".Length);
					}
					break;
				}
			}

			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("mono_debug_logfile")) == false) {
				LOG_FILE_PATH = Environment.GetEnvironmentVariable("mono_debug_logfile");
				trace_requests = true;
				trace_responses = true;
			}

			if (port > 0) {
				// TCP/IP server
				Program.Log("waiting for debug protocol on port " + port);
				RunServer(port);
			} else {
				// stdin/stdout
				Program.Log("waiting for debug protocol on stdin/stdout");
				RunSession(Console.OpenStandardInput(), Console.OpenStandardOutput());
			}
		}

		static Assembly MSBuildAssemblyResolver(object sender, ResolveEventArgs args)
		{
			var msbuildAssemblies = new string[] {
				"Microsoft.Build",
					"Microsoft.Build.Engine",
					"Microsoft.Build.Framework",
					"Microsoft.Build.Tasks.Core",
					"Microsoft.Build.Utilities.Core",
					"System.Reflection.Metadata"};

			var asmName = new AssemblyName(args.Name);
			if (!msbuildAssemblies.Any(n => string.Compare(n, asmName.Name, StringComparison.OrdinalIgnoreCase) == 0))
				return null;

			// Temporary workaround: System.Reflection.Metadata.dll is required in msbuildBinDir, but it is present only
			// in $msbuildBinDir/Roslyn .
			//
			// https://github.com/xamarin/bockbuild/commit/3609dac69598f10fbfc33281289c34772eef4350
			//
			// Adding this till we have a release out with the above fix!
			if (String.Compare(asmName.Name, "System.Reflection.Metadata") == 0) {
				string fixedPath = Path.Combine(msbuildBinDir, "Roslyn", "System.Reflection.Metadata.dll");
				if (File.Exists(fixedPath))
					return Assembly.LoadFrom(fixedPath);
				return null;
			}

			string fullPath = Path.Combine(msbuildBinDir, asmName.Name + ".dll");
			if (File.Exists(fullPath)) {
				// If the file exists under the msbuild bin dir, then we need
				// to load it only from there. If that fails, then let that exception
				// escape
				return Assembly.LoadFrom(fullPath);
			} else
				return null;
		}

		static TextWriter logFile;

		public static void Log(bool predicate, string format, params object[] data)
		{
			if (predicate)
			{
				Log(format, data);
			}
		}
		
		public static void Log(string format, params object[] data)
		{
			try
			{
				Console.Error.WriteLine(format, data);

				if (LOG_FILE_PATH != null)
				{
					if (logFile == null)
					{
						logFile = File.CreateText(LOG_FILE_PATH);
					}

					string msg = string.Format(format, data);
					logFile.WriteLine(string.Format("{0} {1}", DateTime.UtcNow.ToLongTimeString(), msg));
				}
			}
			catch (Exception ex)
			{
				if (LOG_FILE_PATH != null)
				{
					try
					{
						File.WriteAllText(LOG_FILE_PATH + ".err", ex.ToString());
					}
					catch
					{
					}
				}

				throw;
			}
		}

		private static void RunSession(Stream inputStream, Stream outputStream)
		{
			DebugSession debugSession = new MonoDebugSession();
			debugSession.TRACE = trace_requests;
			debugSession.TRACE_RESPONSE = trace_responses;
			debugSession.Start(inputStream, outputStream).Wait();

			if (logFile!=null)
			{
				logFile.Flush();
				logFile.Close();
				logFile = null;
			}
		}

		private static void RunServer(int port)
		{
			TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
			serverSocket.Start();

			new System.Threading.Thread(() => {
				while (true) {
					var clientSocket = serverSocket.AcceptSocket();
					if (clientSocket != null) {
						Program.Log(">> accepted connection from client");

						new System.Threading.Thread(() => {
							using (var networkStream = new NetworkStream(clientSocket)) {
								try {
									RunSession(networkStream, networkStream);
								}
								catch (Exception e) {
									Console.Error.WriteLine("Exception: " + e);
								}
							}
							clientSocket.Close();
							Console.Error.WriteLine(">> client connection closed");
						}).Start();
					}
				}
			}).Start();
		}
	}
}
