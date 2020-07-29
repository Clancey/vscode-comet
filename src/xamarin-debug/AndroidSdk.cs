using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VsCodeXamarinUtil.Android;

namespace VsCodeXamarinUtil
{
	public class AndroidSdk
	{
		static string[] KnownLikelyPaths =>
			new string[] {
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "AndroidSdk"),
				Path.Combine("Library", "Developer", "AndroidSdk"),
				Path.Combine("Developer", "AndroidSdk"),
				Path.Combine("Developer", "Android", "android-sdk-macosx"),
			};

		public static DirectoryInfo FindHome(string specificHome = null, params string[] additionalPossibleDirectories)
		{
			var candidates = new List<string>();

			if (specificHome != null)
			{
				candidates.Add(specificHome);
			}
			else
			{
				candidates.Add(Environment.GetEnvironmentVariable("ANDROID_HOME"));
				if (additionalPossibleDirectories != null)
					candidates.AddRange(additionalPossibleDirectories);
				candidates.AddRange(KnownLikelyPaths);
			}

			foreach (var c in candidates)
			{
				if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
					return new DirectoryInfo(c);
			}

			return null;
		}

		public static string StartEmulatorAndWaitForBoot(DirectoryInfo sdkHome, string avdName)
		{
			var emulator = new Emulator(sdkHome);
			var e = emulator.Start(avdName);

			if (e.WaitForBootComplete(TimeSpan.FromSeconds(60)))
				return e.Serial;

			return null;
		}

		public static async Task<List<DeviceData>> GetEmulatorsAndDevices(DirectoryInfo sdkHome)
		{
			var adb = new Adb(sdkHome);

			List<DeviceData> adbDevices = new List<DeviceData>();
			List<DeviceData> emulatorAvds = new List<DeviceData>();

			await Task.WhenAll(
				Task.Run(() =>
					adbDevices = adb
						.GetDevices()
						.Select(d => new DeviceData
						{
							IsEmulator = d.IsEmulator,
							Name = d.IsEmulator ? adb.GetEmulatorName(d.Serial) : d.Model,
							IsRunning = true,
							Platform = "android",
							Serial = d.Serial
						}).ToList()),
				Task.Run(() =>
					emulatorAvds = new Emulator(sdkHome)
						.ListAvds()
						.Select(a => new DeviceData
						{
							IsEmulator = true,
							IsRunning = false,
							Name = a.Trim(),
							Platform = "android",
							Serial = string.Empty
						}).ToList()));

			// Check adb results if they are missing any emulator AVD's
			// this would be the case if the avd isn't actually running
			foreach (var avd in emulatorAvds)
			{
				if (avd.IsEmulator)
				{
					var existing = adbDevices.FirstOrDefault(d => d.Name.Equals(avd.Name));

					if (existing == null)
						adbDevices.Add(avd);
				}
			}

			return adbDevices;
		}
	}
}