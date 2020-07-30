using System;
using System.Linq;
using VsCodeXamarinUtil;

namespace VSCodeDebug {
	public class MSBuild {
		static string exePath = "/bin/bash";
		static MSBuild ()
		{
			if (Util.IsWindows)
				exePath = GetWindowsMSBuildPath ();
		}
		static string GetWindowsMSBuildPath ()
		{
			//%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
			// -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

			var p = new System.Diagnostics.Process {
				StartInfo = {
					CreateNoWindow = true,
					FileName = "vswhere.exe",
					WorkingDirectory = "%ProgramFiles(x86)%\\Microsoft Visual Studio\\Installer",
					Arguments = " -latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
					UseShellExecute = false,
					RedirectStandardOutput = true,
				}
			};
			p.Start ();
			p.WaitForExit ();
			return p.StandardOutput.ReadToEnd ();
		}
		public static (bool Success, string Output) Run (string workingDirectory, params string [] args)
		{
			//On non windows platforms, we run this through bash
			var newArgs = Util.IsWindows ? args :
				new [] { "msbuild" }.Union (args).ToArray ();
			var p = new System.Diagnostics.Process ();

			try {
				p.StartInfo.CreateNoWindow = false;
				p.StartInfo.FileName = exePath;
				p.StartInfo.WorkingDirectory = workingDirectory;
				//p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.Arguments = "-c \"" + Utilities.ConcatArgs (newArgs) + "\"";
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.Start ();
				p.WaitForExit ();
				return (true, p.StandardOutput.ReadToEnd());

			} catch (Exception ex) {

				Console.WriteLine (ex);
			}

			return (false, "");
		}

		
	}
}
