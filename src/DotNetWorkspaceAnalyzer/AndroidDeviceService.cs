using AndroidSdk;

namespace DotNetWorkspaceAnalyzer;

public class AndroidDeviceService : IAndroidDeviceService
{
	public event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	public Task<IEnumerable<DeviceData>> GetDevices(params string[] targetPlatformIdentifiers)
	{
		const int HighPriority = 0;
		const int MedPriority = 1;
		const int LowPriority = 2;

		var results = new List<(DeviceData Device, int Priority)>();

		// Get ADB Devices (includes emulators)
		var adb = new AndroidSdk.Adb();
		var adbDevices = adb.GetDevices() ?? Enumerable.Empty<AndroidSdk.Adb.AdbDevice>();

		// Split out emulators and physical devices
		var emulators = adbDevices?.Where(d => d.IsEmulator)?.Select(e => new { EmulatorName = adb.GetEmulatorName(e.Serial), Emulator = e });
		var devices = adbDevices?.Where(d => !d.IsEmulator) ?? Enumerable.Empty<Adb.AdbDevice>();

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
			var emulator = emulators?.FirstOrDefault(e => e.EmulatorName == a.Name);

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

		return Task.FromResult(results.OrderBy(t => t.Priority).ThenBy(t => t.Device.Name).Select(t => t.Device));
	}

	public Task StartWatching(params string[] targetPlatformIdentifiers)
	{
		return Task.CompletedTask;
	}

	public Task StopWatching()
	{
		return Task.CompletedTask;
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
				var key = parts?[0]?.Trim();
				var val = parts?[1]?.Trim();

				if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
					r[key] = val;
			}
		}

		return r;
	}
}