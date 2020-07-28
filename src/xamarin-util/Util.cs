using System;

namespace VsCodeXamarinUtil
{
	public static class Util
	{
		public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
	}
}
