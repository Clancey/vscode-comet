using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AndroidDevices
{

	

	class MainClass
	{
		public static void Main(string[] args)
		{
			var outputPath = args.FirstOrDefault();

			var sdkHome = AndroidSdk.FindHome();

			if (sdkHome == null)
			{
				Console.WriteLine("Android SDK Not Found");
				return;
			}

			var devices = AndroidSdk.GetEmulatorsAndDevices(sdkHome);

			foreach(var device in devices)
				Console.WriteLine($"{device.Name} - {device.IsRunning} - {device.Serial}");

			if (!string.IsNullOrEmpty(outputPath))
			{
				var json = Newtonsoft.Json.JsonConvert.SerializeObject(devices);

				File.WriteAllText(outputPath, json);
			}
		}
	}
}
