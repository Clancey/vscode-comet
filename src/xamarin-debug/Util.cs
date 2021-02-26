using System;
using System.IO;

namespace VsCodeXamarinUtil
{
	public static class Util
	{
		public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

		public static void LogToFile(string message)
		{
			var desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "xamarin-debug.txt");

			File.AppendAllLines(desktop, new[] { message });
		}
	}
}
