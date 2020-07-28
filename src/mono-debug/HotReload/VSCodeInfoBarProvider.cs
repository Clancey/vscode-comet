/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Threading.Tasks;
using Xamarin.HotReload.Ide;

namespace VSCodeDebug.HotReload
{
    class VSCodeInfoBarProvider : IInfoBarProvider
	{
		public Task CloseAsync(InfoBar infoBar)
		{
			return Task.CompletedTask;
		}

		public Task ShowAsync(InfoBar infoBar)
		{
			return Task.CompletedTask;
		}
	}
}
