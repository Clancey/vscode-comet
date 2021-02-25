/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Mono.Debugging.Soft;
using System;
using System.Linq;
using System.Reflection;

namespace VSCodeDebug.HotReload
{
	public class HotReloadManager
	{
		//private IdeManager _ideManager;

		public HotReloadManager()
		{
		}

		public void Start(SoftDebuggerSession debugger)
		{
			var untypedStartInfo = debugger.GetStartInfo();

			// Initialize and start hot reload plugins here
		}

		public void DocumentChanged(string fullPath, string relativePath)
		{
			// TODO: Notify hot reload of changed file
		}
	}
}
