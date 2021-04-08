using System;
using System.IO;
using System.Linq;
using VsCodeMobileUtil;

namespace VSCodeDebug
{
	public class DotNet
	{
		public static (bool Success, string Output) Run(Action<string> consoleOutputHandler, string workingDir, params string[] args)
		{
			var arglist = string.Join(" ", args);

			var pr = new DotnetRunner(arglist, workingDir, System.Threading.CancellationToken.None, consoleOutputHandler);

			var r = pr.WaitForExit();
			var t = string.Join(Environment.NewLine, r.StandardOutput.Concat(r.StandardError));

			return (!r.StandardError.Any(), t);
		}
	}
}
