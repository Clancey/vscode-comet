/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Threading.Tasks;
using Xamarin.HotReload.Ide;

namespace VSCodeDebug.HotReload
{
	public class VSCodeThreadingProvider : IThreadingProvider
	{
		public Task InvokeOnMainThreadAsync(Func<Task> work)
		{
			throw new NotImplementedException();
		}
	}
}
