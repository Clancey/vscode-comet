/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Mono.Debugging.Soft;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.HotReload;
using Xamarin.HotReload.Ide;

namespace VSCodeDebug.HotReload
{
	public class VSCodeProject : IProject
	{
		public VSCodeProject(SoftDebuggerSession debugger)
        {
			// Hardcode for now
			RunConfiguration = new RunConfiguration(this, false, "");

			Debugger = debugger;
        }

		public string Name => "HotReloadProject";

		public LinkerSetting LinkerSetting => LinkerSetting.None;

		public RunConfiguration RunConfiguration { get; set; }

		public ISettingsProvider ProjectSettings { get; }

		public HotReloadBridge Bridge { get; set; }

		public SoftDebuggerSession Debugger { get; }

		public ILogger Logger { get; } = new VSCodeLogger();

		public bool MonoInterpreterEnabled => true;

		public IEnumerable<string> TargetFrameworkMonikers => new List<string>();

		public ProjectFlavor ProjectFlavor => ProjectFlavor.Android;

		public Task ShowOptionsAsync(ProjectOptions options)
		{
			switch (options)
			{
				case ProjectOptions.Linker:
				case ProjectOptions.Interpreter:
					ShowProjectBuildOptions();
					break;
			}
			return Task.CompletedTask;
		}

		void ShowProjectBuildOptions()
		{
#if LATER
			switch (ProjectFlavor)
			{
				case ProjectFlavor.Android:
					Project.OpenPropertyPage(new Guid(Constants.AndroidOptionsPropertyPageGuidString));
					break;
				case ProjectFlavor.iOS:
					Project.OpenPropertyPage(new Guid(Constants.iOSBuildPageGuidString));
					break;
			}
#endif
		}

		public async Task<bool> ReferencesAssemblyAsync(string assemblyName) => true;

		public string GetPackageVersionInstalled(string packageId) => "1.1.1.1";

		public string GetPackageInstallPath(string packageId)
		{
			throw new NotImplementedException();
		}

		public bool HasMinimumPackageVersionInstalled(string packageId, string minimumPackageVersion, string[] blacklistedVersions = null) => true;

		public Task UpdatePackagesForSolution(string[] packageIds)
		{
			return Task.CompletedTask;
		}
	}
}
