using System;
using AndroidSdk;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AndroidDevices
{

	class DeviceData
	{
		public string Name { get; set; }
		public string Serial { get; set; }
		public bool IsEmulator { get; set; }
		public bool IsRunning { get; set; }
	}

	class MainClass
	{
		public static void Main(string[] args)
		{
			var outputPath = args.FirstOrDefault();
			if(string.IsNullOrWhiteSpace(outputPath)) {
				Console.WriteLine("Please pass in the output path");
				return;
			}
			// Make sure all of the tools we need are created and installed
			var sdk = new AndroidSdkManager();

			// Ensure all the SDK components are installed
			sdk.Acquire();

			var devices = sdk.Adb.GetDevices();

			var emulators = sdk.AvdManager.ListAvds();
			//var emulators = sdk.Emulator.ListAvds();
			var foundDevices = new List<DeviceData>();
			var deviceSerials = new Dictionary<string, string>();

			foreach(var device in devices) {
				var name = sdk.Adb.GetDeviceName(device.Serial);
				if (device.IsEmulator)
					deviceSerials[name] = device.Serial;
				else
					foundDevices.Add(new DeviceData {
						IsRunning = true,
						Name = name,
						Serial = device.Serial,
					});
			}
			foreach(var emulator in emulators) {
				var isRunning = deviceSerials.TryGetValue(emulator.Name, out var serial);
				foundDevices.Add(new DeviceData {
					IsEmulator = true,
					IsRunning = isRunning,
					Serial = serial,
					Name = emulator.Name,
				});
			}

			//foreach(var device in foundDevices) {
			//	Console.WriteLine($"{device.Name} - {device.IsRunning} - {device.Serial}");
			//}

			var json = Newtonsoft.Json.JsonConvert.SerializeObject(foundDevices);

			File.WriteAllText(outputPath, json);

		}
	}
}
