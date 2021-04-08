using System;
using System.IO;
using System.Threading;
using VsCodeMobileUtil;

namespace VSCodeDebug
{
	public class DotnetRunner : ShellProcessRunner
	{
		static DotnetRunner()
		{
			var r = new Microsoft.DotNet.DotNetSdkResolver.NETCoreSdkResolver();
			sdkRoot = r.GetDotnetExeDirectory();
			DotNetExecutablePath = Path.Combine(sdkRoot, DotNetExeName);
		}
		public static string DotNetExecutablePath { get; private set; }
		static string sdkRoot;
		static string DotNetExeName
			=> Util.IsWindows ? "dotnet.exe" : "dotnet";
		
		public DotnetRunner(string args, string workingDir, CancellationToken cancellationToken, Action<string> outputHandler) : base(DotNetExecutablePath, args, cancellationToken, workingDir, outputHandler)
		{

		}
	}
}
