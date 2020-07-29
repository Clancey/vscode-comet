using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VsCodeXamarinUtil
{
	public class XCode
	{
		const string patternInstrumentsDevices = @"(?<name>.*?)(\((?<version>[0-9.]+)\))?\s+\[(?<serial>[0-9a-zA-Z\-]+)\]\s{0,}(?<sim>\(Simulator\)){0,1}";

		static Regex rxInstrumentDevices = new Regex(patternInstrumentsDevices, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		public static List<DeviceData> GetInstrumentsDevices(bool onlyDevices = false)
		{
			var results = new List<DeviceData>();

			var instruments = new FileInfo("/usr/bin/instruments");

			if (!instruments.Exists)
				throw new FileNotFoundException(instruments.FullName);

			var ir = ProcessRunner.Run(instruments,
				new ProcessArgumentBuilder()
					.Append("-s")
					.Append("devices"));

			// Remove the "Known devices:" line of output
			if (ir.StandardOutput.Any())
				ir.StandardOutput.RemoveAt(0);

			foreach (var line in ir.StandardOutput)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				var match = rxInstrumentDevices.Match(line);

				if (match == null)
					continue;

				var name = match.Groups?["name"]?.Value;
				var serial = match.Groups?["serial"]?.Value;
				var version = match.Groups?["version"]?.Value;
				var sim = match.Groups?["sim"]?.Value;

				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrEmpty(serial))
				{
					if (string.IsNullOrWhiteSpace(sim))
					{
						var isSim = !string.IsNullOrWhiteSpace(sim);

						if (!onlyDevices || (onlyDevices && !isSim))
							results.Add(new DeviceData
							{
								Serial = serial,
								IsEmulator = isSim,
								IsRunning = !isSim,
								Name = name?.Trim(),
								Version = version,
								Platform = "ios"
							});
					}
				}
			}

			return results;
		}

		static List<SimCtlDeviceType> GetSimulatorDeviceTypes()
		{
			var xcrun = new FileInfo("/usr/bin/xcrun");

			if (!xcrun.Exists)
				throw new FileNotFoundException(xcrun.FullName);

			var ir = ProcessRunner.Run(xcrun,
				new ProcessArgumentBuilder()
					.Append("simctl")
					.Append("list")
					.Append("-j")
					.Append("devicetypes"));

			var json = string.Join(Environment.NewLine, ir.StandardOutput);

			var dict = JsonConvert.DeserializeObject<Dictionary<string, List<SimCtlDeviceType>>>(json);

			return dict?["devicetypes"] ?? new List<SimCtlDeviceType>();
		}

		static List<SimCtlRuntime> GetSimulatorRuntimes()
		{
			var xcrun = new FileInfo("/usr/bin/xcrun");

			if (!xcrun.Exists)
				throw new FileNotFoundException(xcrun.FullName);

			var ir = ProcessRunner.Run(xcrun,
				new ProcessArgumentBuilder()
					.Append("simctl")
					.Append("list")
					.Append("-j")
					.Append("runtimes"));

			var json = string.Join(Environment.NewLine, ir.StandardOutput);

			var dict = JsonConvert.DeserializeObject<Dictionary<string, List<SimCtlRuntime>>>(json);

			return dict?["runtimes"] ?? new List<SimCtlRuntime>();
		}

		static Dictionary<string, List<SimCtlDevice>> GetSimulatorDevices()
		{
			var xcrun = new FileInfo("/usr/bin/xcrun");

			if (!xcrun.Exists)
				throw new FileNotFoundException(xcrun.FullName);

			var results = new List<SimCtlDevice>();

			var ir = ProcessRunner.Run(xcrun,
				new ProcessArgumentBuilder()
					.Append("simctl")
					.Append("list")
					.Append("-j")
					.Append("devices"));

			var json = string.Join(Environment.NewLine, ir.StandardOutput);

			var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<SimCtlDevice>>>>(json);

			return dict?["devices"] ?? new Dictionary<string, List<SimCtlDevice>>();
		}

		static List<SimCtlDevice> GetSimulators()
		{
			var xcrun = new FileInfo("/usr/bin/xcrun");

			if (!xcrun.Exists)
				throw new FileNotFoundException(xcrun.FullName);

			var results = new List<SimCtlDevice>();

			var ir = ProcessRunner.Run(xcrun,
				new ProcessArgumentBuilder()
					.Append("simctl")
					.Append("list")
					.Append("-j")
					.Append("devices"));

			var json = string.Join(Environment.NewLine, ir.StandardOutput);

			var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<SimCtlDevice>>>>(json);

			var deviceSets = dict["devices"];

			//var deviceTypes = GetSimulatorDeviceTypes();
			var runtimes = GetSimulatorRuntimes();

			foreach (var deviceSet in deviceSets)
			{
				var runtime = runtimes.FirstOrDefault(r => r.Identifier.Equals(deviceSet.Key, StringComparison.OrdinalIgnoreCase));

				if (runtime != null)
				{
					foreach (var d in deviceSet.Value)
					{
						if (d.IsAvailable)
						{
							d.Runtime = runtime;
							results.Add(d);
						}
					}
				}
			}

			return results;
		}

		public static List<DeviceData> GetSimulatorsAndDevices()
		{
			var devices = GetInstrumentsDevices()
				// NOTE: Exclude emulators as we get a better version below
				// Also exclude devices without versions as all iDevices will have a version
				.Where(i => !i.IsEmulator && !string.IsNullOrWhiteSpace(i.Version));

			var simulators = GetSimulators()
				.Where(s => s.IsAvailable && (s.Runtime?.IsAvailable ?? false))
				.Select(s => new DeviceData
				{
					IsEmulator = true,
					Name = $"{s.Name} ({s.Runtime.Name})",
					IsRunning = s.State != null && s.State.ToLowerInvariant().Contains("booted"),
					Serial = s.Udid,
					Version = s.Runtime.Version,
					Platform = "ios"
				});

			return devices.Concat(simulators).ToList();
		}


		public static async Task<List<SimCtlDeviceType>> GetDevicePairs()
		{
			var results = new List<SimCtlDeviceType>();

			List<SimCtlDeviceType> deviceTypes = new List<SimCtlDeviceType>();
			List<SimCtlRuntime> runtimes = new List<SimCtlRuntime>();
			Dictionary<string, List<SimCtlDevice>> devices = new Dictionary<string, List<SimCtlDevice>>();

			await Task.WhenAll(
				Task.Run(() => deviceTypes = GetSimulatorDeviceTypes()),
				Task.Run(() => runtimes = GetSimulatorRuntimes()),
				Task.Run(() => devices = GetSimulatorDevices()));

			foreach (var deviceType in deviceTypes)
			{
				foreach (var kvp in devices)
				{
					var deviceRuntimeIdentifier = kvp.Key;

					// Find all the devices for all the runtimes
					foreach (var device in kvp.Value)
					{
						if (device.IsAvailable && device.Name.Equals(deviceType.Name))
						{
							var runtime = runtimes.FirstOrDefault(r =>
								r.IsAvailable
								&& r.Identifier.Equals(deviceRuntimeIdentifier, StringComparison.OrdinalIgnoreCase));

							if (runtime != null)
							{
								device.Runtime = runtime;
								deviceType.Devices.Add(device);
							}
						}
					}
				}

				if (deviceType.Devices.Any())
					results.Add(deviceType);
			}

			return results;
		}
	}

	public class AppleDevicesAndSimulators
	{
		[JsonProperty("devices")]
		public List<DeviceData> Devices { get; set; }

		[JsonProperty("simulators")]
		public List<SimCtlDeviceType> Simulators { get; set; }
	}

	public class SimCtlRuntime
	{
		[JsonProperty("bundlePath")]
		public string BundlePath { get; set; }

		[JsonProperty("buildVersion")]
		public string BuildVersion { get; set; }

		[JsonProperty("runtimeRoot")]
		public string RuntimeRoot { get; set; }

		[JsonProperty("identifier")]
		public string Identifier { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("isAvailable")]
		public bool IsAvailable { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}

	public class SimCtlDeviceType
	{
		[JsonProperty("minRuntimeVersion")]
		public long MinRuntimeVersion { get; set; }

		[JsonProperty("bundlePath")]
		public string BundlePath { get; set; }

		[JsonProperty("maxRuntimeVersion")]
		public long MaxRuntimeVersion { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("identifier")]
		public string Identifier { get; set; }

		[JsonProperty("productFamily")]
		public string ProductFamily { get; set; }

		[JsonProperty("devices")]
		public List<SimCtlDevice> Devices { get; set; } = new List<SimCtlDevice>();
	}

	public class SimCtlDevice
	{
		[JsonProperty("dataPath")]
		public string DataPath { get; set; }

		[JsonProperty("logPath")]
		public string LogPath { get; set; }

		[JsonProperty("udid")]
		public string Udid { get; set; }

		[JsonProperty("isAvailable")]
		public bool IsAvailable { get; set; }

		[JsonProperty("deviceTypeIdentifier")]
		public string DeviceTypeIdentifier { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("availabilityError")]
		public string AvailabilityError { get; set; }

		[JsonProperty("deviceType")]
		public SimCtlDeviceType DeviceType { get; set; }

		[JsonProperty("runtime")]
		public SimCtlRuntime Runtime { get; set; }
	}
}