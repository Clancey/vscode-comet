/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Microsoft.VisualStudio.DesignTools.Markup;
using System;
using System.Threading.Tasks;
using Xamarin.HotReload;
using Xamarin.HotReload.Ide;

namespace VSCodeDebug.HotReload
{
	class VSCodeErrorListProvider : IErrorListProvider
	{
		public event EventHandler<IMarkupError> MarkupErrorRecieved;
		public event EventHandler<IMarkupError> MarkupErrorRemoved;

		public Task AddRudeEditsAsync(RudeEdit[] rudeEdits)
		{
			return Task.CompletedTask;
		}

		public Task ShowAsync()
		{
			return Task.CompletedTask;
		}

		public Task ClearAllAsync()
		{
			return Task.CompletedTask;
		}

		public Task AddMarkupErrorAsync(IMarkupError markupError, int linePositionStart, int lineStart)
		{
			return Task.CompletedTask;
		}
	}
}
