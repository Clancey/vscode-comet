/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Xamarin.HotReload;

namespace VSCodeDebug.HotReload
{
	public class VSCodeLogger : ILogger
	{
		public void Log(LogMessage message)
		{
			Program.Log(message.Message);
		}
	}
}
