using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VsCodeMobileUtil
{
	class UtilRunner
	{
		const string helpCommand = "help";

		public static void UtilMain(string[] args)
		{
			var options = new OptionSet();

			var command = helpCommand;
			var id = Guid.NewGuid().ToString();

			options.Add("c|command=", "get the tool version", s => command = s?.ToLowerInvariant()?.Trim() ?? helpCommand);
			options.Add("h|help", "prints the help", s => command = helpCommand);
			options.Add("i|id=", "unique identifier of the command", s => id = s);

			var extras = options.Parse(args);

			if (command.Equals(helpCommand))
			{
				ShowHelp(options);
				return;
			}

			var response = new CommandResponse
			{
				Id = id,
				Command = command
			};

			object responseObject = null;

			try
			{
				responseObject = command switch
				{
					"version" => Version(),
					"devices" => AllDevices(),
					"android-devices" => AndroidDevices(),
					"ios-devices" => XCode.GetDevices(),
					"android-start-emulator" => AndroidStartEmulator(extras),
					"debug" => Debug(),
					_ => Version()
				};
			}
			catch (Exception ex)
			{
				response.Error = ex.Message;
			}

			response.Response = responseObject;

			var json = Newtonsoft.Json.JsonConvert.SerializeObject(response,
				Newtonsoft.Json.Formatting.None, new Newtonsoft.Json.JsonSerializerSettings
				{
					Formatting = Newtonsoft.Json.Formatting.None,
					NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
				});
			
			Console.WriteLine(json);
		}

		static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: vscode-mobile-util [OPTIONS]+");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Out);
		}

		static object Version()
			=> new { Version = "0.2.0" };

		static IEnumerable<DeviceData> AndroidDevices()
		{
			const int HighPriority = 0;
			const int MedPriority = 1;
			const int LowPriority = 2;

			var results = new List<(DeviceData Device, int Priority)>();

			// Get ADB Devices (includes emulators)
			var adb = new AndroidSdk.Adb();
			var adbDevices = adb.GetDevices() ?? Enumerable.Empty<AndroidSdk.Adb.AdbDevice>();

			// Split out emulators and physical devices
			var emulators = adbDevices?.Where(d => d.IsEmulator)?.Select(e => new { EmulatorName = adb.GetEmulatorName(e.Serial), Emulator = e});
			var devices = adbDevices?.Where(d => !d.IsEmulator);

			// Physical devices are easy
			foreach (var d in devices)
			{
				results.Add((new DeviceData
				{
					IsEmulator = false,
					IsRunning = true,
					Name = d.Device,
					Platform = "android",
					Serial = d.Serial,
					Version = d.Model
				}, HighPriority));
			}

			// Get all Avds
			var avdManager = new AndroidSdk.AvdManager();
			var avds = avdManager.ListAvds() ?? Enumerable.Empty<AndroidSdk.AvdManager.Avd>();

			// Look through all avd's to list, but let's be smart and see if any of them
			// are already running (so were listed in the adb devices output)
			foreach (var a in avds)
			{
				// See if ADB returned a running instance
				var emulator = emulators.FirstOrDefault(e => e.EmulatorName == a.Name);

				results.Add((new DeviceData
				{
					IsEmulator = true,
					IsRunning = emulator != null,
					Name = a.Device,
					Platform = "android",
					Serial = emulator?.Emulator?.Serial ?? a.Name,
					Version = a.BasedOn
				}, emulator == null ? LowPriority : MedPriority));
			}

			return results.OrderBy(t => t.Priority).Select(t => t.Device);
		}

		
		static IEnumerable<DeviceData> AllDevices()
		{
			var result = AndroidDevices();

			if (Util.IsWindows)
				return result;

			var iosDevices = XCode.GetDevices();

			return result.Concat(iosDevices);
		}

		static SimpleResult AndroidStartEmulator(IEnumerable<string> args)
		{
			var avdName = args?.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(avdName))
				throw new ArgumentNullException("AvdName");

			var emu = new AndroidSdk.Emulator();
			var p = emu.Start(avdName);
			var success = p.WaitForBootComplete(TimeSpan.FromSeconds(200));

			return new SimpleResult { Success = success };
		}

		static SimpleResult Debug()
		{
			var json = Console.ReadLine();

			return new SimpleResult { Success = true };
		}
	}
}
