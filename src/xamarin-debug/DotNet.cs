using System;
using System.IO;
using System.Linq;
using VsCodeXamarinUtil;

namespace VSCodeDebug
{
	public class DotNet
	{
		public static (bool Success, string Output) Run(Action<string> consoleOutputHandler, params string[] args)
		{
			var arglist = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

			var pr = new ShellProcessRunner("dotnet", arglist, System.Threading.CancellationToken.None, consoleOutputHandler);

			var r = pr.WaitForExit();
			var t = string.Join(Environment.NewLine, r.StandardOutput.Concat(r.StandardError));

			return (!r.StandardError.Any(), t);
		}
	}
}
