using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VSCodeDebug;

namespace VsCodeMobileUtil;

class UtilRunner
{
	const string helpCommand = "help";

	public static void UtilMain(string[] args)
	{
		var options = new OptionSet();

		var targetPlatformIdentifier = string.Empty;
		var command = helpCommand;
		var id = Guid.NewGuid().ToString();

		options.Add("c|command=", "command to execute", s => command = s?.ToLowerInvariant()?.Trim() ?? helpCommand);
		options.Add("h|help", "prints the help", s => command = helpCommand);
		options.Add("i|id=", "unique identifier of the command", s => id = s);
		options.Add("t|tpi=", "target platform identifier", s => targetPlatformIdentifier = s);

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
				"devices" => AllDevices(targetPlatformIdentifier),
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
				Platforms = new[] { "android" },
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
			var avdConfig = ParseAvdConfigIni(Path.Combine(a.Path, "config.ini"));

			var architecture = avdConfig?["hw.cpu.arch"] ?? string.Empty;
			var manufacturer = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(avdConfig?["hw.device.manufacturer"] ?? string.Empty);
			var model = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(avdConfig?["hw.device.name"] ?? string.Empty);

			// See if ADB returned a running instance
			var emulator = emulators.FirstOrDefault(e => e.EmulatorName == a.Name);

			results.Add((new DeviceData
			{
				IsEmulator = true,
				IsRunning = emulator != null,
				Name = a.Name,
				Details = emulator != null
					? emulator.Emulator.Product + " " + emulator.Emulator.Model
					: manufacturer + " " + model + " (" + architecture + ")",
				Platforms = new[] { "android" },
				Serial = emulator?.Emulator?.Serial ?? a.Name,
				Version = a.BasedOn,
				RuntimeIdentifier = architecture switch
				{
					"armeabi-v7a" => "android-arm",
					"armeabi" => "android-arm",
					"arm" => "android-arm",
					"arm64" => "android-arm64",
					"arm64-v8a" => "android-arm64",
					"x86" => "android-x86",
					"x86_64" => "android-x64",
					_ => "android-arm"
				}
			}, emulator == null ? LowPriority : MedPriority));
		}

		return results.OrderBy(t => t.Priority).Select(t => t.Device);
	}

	
	static IEnumerable<DeviceData> AllDevices(string targetPlatformId)
	{
		var results = new List<DeviceData>();

		if (string.IsNullOrEmpty(targetPlatformId) || targetPlatformId.Equals("android", StringComparison.OrdinalIgnoreCase))
		{
			var androidDevices = AndroidDevices();
			if (androidDevices?.Any() ?? false)
				results.AddRange(androidDevices);
		}

		if (Utilities.IsWindows)
			return results;

		if (string.IsNullOrEmpty(targetPlatformId) || targetPlatformId.Equals("ios", StringComparison.OrdinalIgnoreCase))
		{
			var iosDevices = XCode.GetDevices(targetPlatformId);
			if (iosDevices?.Any() ?? false)
				results.AddRange(iosDevices);
		}

		return results;
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

	static Dictionary<string, string> ParseAvdConfigIni(string file)
	{
		if (!File.Exists(file))
			return new Dictionary<string, string>();

		var r = new Dictionary<string, string>();

		foreach (var line in File.ReadAllLines(file))
		{

			if (!line.Contains('='))
				continue;

			var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

			if ((parts?.Length ?? 0) == 2)
			{
				r[parts[0].Trim()] = parts[1].Trim();
			}
		}

		return r;
	}
}
