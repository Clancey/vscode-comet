/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Mono.Debugging.Soft;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using VsCodeMobileUtil;
using System.Threading;
using System.Threading.Tasks;

namespace VSCodeDebug.HotReload
{
	public class HotReloadManager
	{
		//private IdeManager _ideManager;

		public HotReloadManager()
		{
		}
		DotnetRunner runner;
		public void SetLaunchData(LaunchData launchData)
		{
			var root =  Path.GetDirectoryName(typeof(HotReloadManager).Assembly.Location);
			var reloadifyPath = Path.Combine(root, "Reloadify", "Reloadify.dll");
			if (!File.Exists(reloadifyPath))
				return;

			var projectDir = Path.GetDirectoryName(launchData.Project);
			if (!Directory.Exists(projectDir))
				projectDir = launchData.WorkspaceDirectory;

			var args = new ProcessArgumentBuilder();
			args.AppendQuoted(reloadifyPath);

			args.AppendQuoted(launchData.Project);
			args.Append($"-t={launchData.ProjectTargetFramework}");
			args.Append($"-c={launchData.Configuration}");
			args.Append($"-f=\"{launchData.WorkspaceDirectory}\"");
			var runCommand = args.ToString();
 			runner = new DotnetRunner(runCommand, projectDir, CancellationToken.None, OutputHandler);
		}

		public void Start(SoftDebuggerSession debugger)
		{
			var untypedStartInfo = debugger.GetStartInfo();

			// Initialize and start hot reload plugins here
		}

		public void DocumentChanged(string fullPath, string relativePath)
		{

		}
		public async void Stop()
		{
			try
			{
				runner?.StandardInput?.WriteLine("exit");
				await Task.Delay(500);
				runner?.Kill();
			}
			catch(Exception ex)
			{

			}
			runner = null;
		}

		public Action<string> OutputHandler {get;set;}
	}
}
