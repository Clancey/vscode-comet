/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Mono.Debugging.Soft;
using System;
using System.Linq;
using System.Reflection;
using Xamarin.HotReload;
using Xamarin.HotReload.Ide;
using Xamarin.HotReload.Telemetry;
using Xamarin.HotReload.Translations;

namespace VSCodeDebug.HotReload
{
	public class HotReloadManager
	{
		private IdeManager _ideManager;

		public HotReloadManager()
		{
			var logger = new VSCodeLogger();
			var settingsProvider = new VSCodeSettingsProvider();
			var infoBarProvider = new VSCodeInfoBarProvider();
			var errorListProvider = new VSCodeErrorListProvider();
			var dialogProvider = new VSCodeDialogProvider();
			var threadingProvider = new VSCodeThreadingProvider();

			_ideManager = new IdeManager(logger, settingsProvider, infoBarProvider, errorListProvider, dialogProvider, threadingProvider, xaml: null);

			_ideManager.AgentStatusChanged += AgentStatusChanged;
			_ideManager.AgentReloadResultReceived += AgentXamlResultReceived;
			_ideManager.Logger.Log(LogLevel.Info, "Hot Reload IDE Extension Loaded");
		}

		public void StartHR(SoftDebuggerSession debugger)
		{
			if (!_ideManager.Settings.IsHotReloadEnabled())
				return;

#if LATER
			var debuggingFlavor = startupProject.GetProjectFlavor();
			if (debuggingFlavor == ProjectFlavor.Unsupported)
				return;

			// FIXME: For now, ensure we reference forms, otherwise no need to try hot reload at this point
			if (!startupProject.ReferencesAssembly("Xamarin.Forms.Core"))
			{
				ide.Logger.Log(Info, "Hot Reload disabled because project does not reference Xamarin.Forms");
				return;
			}
#endif

			var untypedStartInfo = debugger.GetStartInfo();

			var startInfo = untypedStartInfo as SoftDebuggerStartInfo;
			if (startInfo is null)
			{
				_ideManager.Logger.Log(LogLevel.Warn, "Hot Reload disabled due to unexpected type of DebuggerStartInfo");
				_ideManager.Telemetry.Post(TelemetryEventType.Fault, TelemetryEvents.SessionUnsupported,
					properties: new (string, TelemetryValue)[] {
						("Reason", "UnexpectedType"),
						("Expected", "Mono.Debugging.Soft.SoftDebuggerStartInfo"),
						("Actual", untypedStartInfo?.GetType ().FullName)
					});
				return;
			}

			bool isDevice;
			try
			{
				isDevice = startInfo.IsPhysicalDevice();
			}
			catch (Exception ex)
			{
				_ideManager.Logger.Log(ex);
				_ideManager.Telemetry.ReportException(TelemetryEvents.SessionUnsupported, ex);
				return;
			}

			var project = new VSCodeProject();

			// ide.StartHotReloadAsync checks if the project can run. If it can't, it'll return
			// and throw error bars for the user.
			_ideManager.StartHotReloadAsync(project).LogIfFaulted(_ideManager?.Logger);
		}

		private void AgentStatusChanged(object sender, AgentStatusMessage e)
		{
			string statusMessage = null;
			if (e.State == HotReloadState.Starting)
				statusMessage = CommonStrings.HotReloadStarting;
			else if (e.State == HotReloadState.Enabled)
				statusMessage = CommonStrings.HotReloadConnected;
			else if (e.State == HotReloadState.Failed)
				statusMessage = CommonStrings.HotReloadFailedInitialize;
			else if (e.State == HotReloadState.Disabled)
				statusMessage = CommonStrings.HotReloadDisabled;

			if (statusMessage != null)
				_ideManager.Logger.Log(LogLevel.Info, statusMessage);
		}

		private void AgentXamlResultReceived(object sender, ReloadTransactionMessage msg)
		{
			int rudeEdits = msg.Transactions.SelectMany(txn => txn.Result.RudeEdits).Count();
			string statusText = (rudeEdits > 0) ? CommonStrings.HotReloadReloadedRudeEditStatus(rudeEdits) : CommonStrings.HotReloadXAMLReloadSuccess;

			_ideManager.Logger.Log(LogLevel.Info, statusText);
		}
	}
}
