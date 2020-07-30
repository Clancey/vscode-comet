/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Xamarin.HotReload;

namespace VSCodeDebug.HotReload
{
	public class VSCodeSettingsProvider : ISettingsProvider
	{
		public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

		public bool GetBool(string key, bool defaultValue)
		{
			return defaultValue;
		}

		public void SetBool(string key, bool value)
		{
			OnSettingsChanged(key, value);
		}

		public void OnSettingsChanged(string key, object newValue)
		{
			SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, newValue));
		}
	}
}
