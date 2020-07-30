/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Threading.Tasks;
using Xamarin.HotReload.Ide;

namespace VSCodeDebug.HotReload
{
	class VSCodeDialogProvider : IDialogProvider
	{
		public void ShowMessage(string title, string text)
		{
		}

		public Task<bool> AskYesNoQuestionAsync(string title, string text)
		{
			return Task.FromResult(false);
		}

		public void CloseWaitDialog(string id, string successText)
		{
		}

		public string ShowWaitDialog(string title, string text)
		{
			return null;
		}
	}
}
