/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Mono.Debugging.Soft;
using System;
using System.Reflection;

namespace VSCodeDebug.HotReload
{
    public static class Extensions
	{
		public static object GetStartInfo(this SoftDebuggerSession session)
			=> session.GetType().GetProperty("StartInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
								?.GetValue(session);

		public static bool IsPhysicalDevice(this SoftDebuggerStartInfo startInfo)
		{
			var startInfoType = startInfo.GetType();

			// Android
			var device = startInfoType.GetProperty("Device")?.GetValue(startInfo);
			if (device is null)
			{
				// iOS
				var rsi = startInfoType.GetProperty("RunSessionInfo")?.GetValue(startInfo);
				if (rsi is null)
					throw new NotSupportedException("Cannot get RunSessionInfo");
				device = rsi.GetType().GetProperty("Device")?.GetValue(rsi);
			}
			if (device is null)
				throw new NotSupportedException("Cannot get Device");

			var deviceType = device.GetType();
			var isEmulatorProp = deviceType.GetProperty("IsEmulator");
			if (isEmulatorProp != null)
				return !(bool)isEmulatorProp.GetValue(device);

			var isPhysicalDeviceProp = deviceType.GetProperty("IsPhysicalDevice");
			if (isPhysicalDeviceProp != null)
				return (bool)isPhysicalDeviceProp.GetValue(device);

			throw new NotSupportedException("Cannot find either IsEmulator or IsPhysicalDevice");
		}
	}
}
