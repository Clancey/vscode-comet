using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSCodeDebug.Debugger;
using VSCodeDebug.HotReload;
using VSCodeDebug.Protocol;
using VsCodeMobileUtil;

namespace VSCodeDebug;

internal class MauiDebugSession : MonoDebugSession
{
	public override async void Launch(Request request, Response response)
	{
		AttachMode = false;

		SetExceptionBreakpoints(request.arguments.__exceptionOptions);

		var launchOptions = new LaunchData(request.arguments);
		var valid = launchOptions.Validate();
		HotReloadManager?.SetLaunchData(launchOptions);

		if (!valid.success)
		{
			SendErrorResponse(response, 3002, valid.message);
			return;
		}

		int port = launchOptions.DebugPort;
		var host = GetString(request.arguments, "address");
		IPAddress address = string.IsNullOrWhiteSpace(host) ? IPAddress.Loopback : Utilities.ResolveIPAddress(host);
		if (address == null)
		{
			SendErrorResponse(response, 3013, "Invalid address '{address}'.", new { address = address });
			return;
		}


		if (launchOptions.ProjectType == ProjectType.Android)
		{
			//Android takes a few seconds to get the debugger ready....
			//await Task.Delay(3000);
		}

		if (launchOptions.ProjectType == ProjectType.iOS || launchOptions.ProjectType == ProjectType.MacCatalyst)
			port = Utilities.FindFreePort(55559);

		Connect(launchOptions, address, port);

		//on IOS we need to do the connect before we launch the sim.
		if (launchOptions.ProjectType == ProjectType.iOS || launchOptions.ProjectType == ProjectType.MacCatalyst)
		{
			var r = launchOptions.ProjectType == ProjectType.iOS
				? await LaunchiOS(response, launchOptions, port)
				: await LaunchCatalyst(launchOptions, port);

			if (!r.success)
			{
				SendErrorResponse(response, 3002, r.message);
				return;
			}
			SendResponse(response);
			return;
		}

		SendResponse(response);
	}

	public override void Disconnect(Request request, Response response)
	{
		try
		{
			if (!(iOSDebuggerProcess?.HasExited ?? true))
			{
				SendConsoleEvent($"Stopping iOS process...");

				iOSDebuggerProcess?.StandardInput?.WriteLine("\r\n");
				iOSDebuggerProcess?.Kill();
				iOSDebuggerProcess = null;

				SendConsoleEvent($"iOS Process was stopped.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}

		base.Disconnect(request, response);
	}

	//string[] GetLocalIps()
	//{
	//	var hostName = Dns.GetHostName();

	//	// Find host by name
	//	var addresses = Dns.GetHostAddresses(hostName);

	//	return addresses
	//		.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
	//		.Select(ip => ip.ToString())
	//		.Concat(new string[] { "127.0.0.1" })
	//		.ToArray();
	//}

	async Task<(bool success, string message)> LaunchCatalyst(LaunchData launchOptions, int port)
	{
		var projectDir = launchOptions.GetProjectPropertyPathValue("MSBuildProjectDirectory");
		var outputPath = launchOptions.GetProjectPropertyPathValue("OutputPath");

		if (!Path.IsPathRooted(outputPath))
			outputPath = Path.GetFullPath(outputPath, projectDir);

		var targetDir = launchOptions.GetProjectPropertyPathValue("TargetDir")
			?? projectDir
			?? launchOptions.WorkspaceDirectory;

		var targetName = launchOptions.GetProjectPropertyValue("TargetName")
			?? launchOptions.GetProjectPropertyValue("AssemblyName")
			?? launchOptions.AppName;

		var targetPath = Path.Combine(targetDir, targetName + ".app");

		// _XamarinSdkRootDirectory - /usr/local/share/dotnet/packs/Microsoft.MacCatalyst.Sdk/15.4.1180-rc.2/


		//Environment.SetEnvironmentVariable("__XAMARIN_DEBUG_HOSTS__", "127.0.0.1");
		//Environment.SetEnvironmentVariable("__XAMARIN_DEBUG_PORT__", port.ToString());

		var spr = new ShellProcessRunner("/usr/bin/open",
			$"\"{targetPath}\"",
			CancellationToken.None,
			launchOptions.WorkspaceDirectory,
			d => SendConsoleEvent(d), new Dictionary<string, string>
			{
				["__XAMARIN_DEBUG_HOSTS__"] = "127.0.0.1",
				["__XAMARIN_DEBUG_PORT__"] = port.ToString(),
			});

		var r = spr.WaitForExit();
		return (r.ExitCode == 0, string.Join("\r\n", r.StandardError));
	}

	async Task<(bool success, string message)> LaunchiOS(Response response, LaunchData launchOptions, int port)
	{
		if (System.Diagnostics.Debugger.IsAttached)
		{
			var sbProps = new StringBuilder();
			foreach (var p in launchOptions.ProjectProperties)
				sbProps.AppendLine(p.Key + " = " + p.Value);
			var dmp = sbProps.ToString();
			Console.WriteLine(dmp);
		}

		var xcodePath = Path.Combine(XCode.GetBestXcode(), "Contents", "Developer");
		if (!Directory.Exists(xcodePath))
		{
			var msg = "Failed to find Xcode";
			SendConsoleEvent(msg);
			SendErrorResponse(response, 3002, msg);
			return (false, msg);
		}

		SendConsoleEvent($"Found Xcode: {xcodePath}");

		var xcodeSdkRoot = $"-sdkroot {xcodePath}";
		var workingDir = launchOptions.GetProjectPropertyPathValue("TargetDir");
		
		string appPath = default;
		DateTime appPathLastWrite = DateTime.MinValue;
		var output = workingDir;
		if (Directory.Exists(output))
		{
			var ridDirs = Directory.GetDirectories(output) ?? new string[0];

			foreach (var ridDir in ridDirs)
			{
				// Find the newest .app generated and assume that's the one we want
				var appDirs = Directory.EnumerateDirectories(ridDir, "*.app", SearchOption.AllDirectories);

				foreach (var appDir in appDirs)
				{
					var appDirTime = Directory.GetLastWriteTime(appDir);
					if (appDirTime > appPathLastWrite)
					{
						appPath = appDir;
						appPathLastWrite = appDirTime;
					}
				}
			}
		}
		
		if (string.IsNullOrEmpty(appPath))
		{
			var msg = $"No .app found in folder: {output}";
			SendConsoleEvent(msg);
			SendErrorResponse(response, 3002, msg);
			return (false, "");
		}

		SendConsoleEvent($"Found .app: {appPath}");

		var dotnetSdkDir = Microsoft.DotNet.NativeWrapper.EnvironmentProvider.GetDotnetExeDirectory();
		var mlaunchPath = string.Empty;
		var dotnetSkdPath = Path.Combine(dotnetSdkDir, "packs", "Microsoft.iOS.Sdk"); // 14.4.100-ci.main.1192/tools/bin/mlaunch

		if (!Directory.Exists(dotnetSkdPath))
		{
			var msg = dotnetSkdPath + " is missing, please install the iOS SDK Workload";
			SendConsoleEvent(msg);
			SendErrorResponse(response, 3002, msg);
			return (false, "");
		}

		// TODO: Use WorkloadResolver to get pack info for `Microsoft.iOS.Sdk` to choose
		// the actual one that's being used - just not sure how to get dotnet sdk version in use
		SendConsoleEvent($"Looking for Microsoft.iOS.Sdk tools in: {dotnetSkdPath}");

		foreach (var dir in Directory.GetDirectories(dotnetSkdPath).Reverse())
		{
			var mlt = Path.Combine(dir, "tools", "bin", "mlaunch");

			if (File.Exists(mlt))
			{
				mlaunchPath = mlt;
				break;
			}
		}

		if (string.IsNullOrEmpty(mlaunchPath) || !File.Exists(mlaunchPath))
		{
			var msg = "Could not locate mlaunch tool in Microsoft.iOS.Sdk workload.";
			SendConsoleEvent(msg);
			SendErrorResponse(response, 3002, msg);
			return (false, msg);
		}

		SendConsoleEvent($"Found mlaunch: {mlaunchPath}");

		var success = await RunMlaumchComand(mlaunchPath,
			workingDir,
			xcodeSdkRoot,
			$"--launchsim \"{appPath}\"",
			$"--argument=-monodevelop-port --argument={port} --setenv=__XAMARIN_DEBUG_PORT__={port}",
			$"--device=:v2:udid={launchOptions.DeviceId}"
			//$"--sdk {launchOptions.iOSSimulatorVersion} --device=:v2:udid={launchOptions.DeviceId}"
			//$"--sdk {launchOptions.iOSSimulatorVersion} --device=:v2:runtime={launchOptions.iOSSimulatorDevice},devicetype={launchOptions.iOSSimulatorDeviceType}"
			);
		return success;
	}

	System.Diagnostics.Process iOSDebuggerProcess;
	public async Task<(bool Success, string Output)> RunMlaumchComand(string command, string workingDirectory, params string[] args)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		var p = new System.Diagnostics.Process();

		const string iOSRunningText = "Press enter to terminate the application";
		//await Task.Run (() => {
		try
		{
			p.StartInfo.CreateNoWindow = false;
			p.StartInfo.FileName = command;
			p.StartInfo.WorkingDirectory = workingDirectory;
			p.StartInfo.Arguments = Utilities.ConcatArgs(args, false);
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardInput = true;
			p.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
				if (e.Data == iOSRunningText)
					tcs.TrySetResult(true);
				tcs.TrySetResult(true);
			};
			p.Exited += (object sender, EventArgs e) => {
				tcs.TrySetResult(false);
			};
			p.Start();
			p.BeginOutputReadLine();
			//p.BeginErrorReadLine ();

		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			tcs.TrySetException(ex);
		}
		_ = Task.Run(() => {
			p.WaitForExit();
			tcs.TrySetResult(false);
		});
		iOSDebuggerProcess = p;

		var s = await tcs.Task;

		SendConsoleEvent($"mlaunch is running...");
		return (s, "");
	}
}
