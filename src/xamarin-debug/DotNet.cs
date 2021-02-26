using System;
using System.IO;
using System.Linq;
using VsCodeXamarinUtil;

namespace VSCodeDebug
{
	public class DotNet
	{
		public static (bool Success, string Output) Run(params string[] args)
		{
			var arglist = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

			Util.LogToFile("Running dotnet " + arglist);

			var pr = new ShellProcessRunner("dotnet", arglist, System.Threading.CancellationToken.None);
			var r = pr.WaitForExit();
			var t = string.Join(Environment.NewLine, r.StandardOutput.Concat(r.StandardError));

			Util.LogToFile(t);

			return (!r.StandardError.Any(), t);
		}
	}
}
