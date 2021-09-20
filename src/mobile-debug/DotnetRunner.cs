using System;
using System.IO;
using System.Linq;
using System.Threading;
using VsCodeMobileUtil;

namespace VSCodeDebug
{
	public class DotnetRunner : ShellProcessRunner
	{
		static DotnetRunner()
		{
			sdkRoot = Microsoft.DotNet.NativeWrapper.EnvironmentProvider.GetDotnetExeDirectory();
			DotNetExecutablePath = Path.Combine(sdkRoot, DotNetExeName);
		}
		public static (bool Success, string Output) Run(Action<string> consoleOutputHandler, string workingDir, params string[] args)
		{
			var dr = new DotnetRunner(string.Join(" ", args), workingDir, CancellationToken.None, consoleOutputHandler);
			var r = dr.WaitForExit();
			var t = string.Join(Environment.NewLine, r.StandardError.Concat(r.StandardOutput));

			return (!r.StandardError.Any(), t);
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
