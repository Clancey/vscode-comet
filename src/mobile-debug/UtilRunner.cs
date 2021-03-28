using Mono.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
					"ios-devices" => iOSDevices(),
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
			=> new { Version = "0.1.1" };

		static DirectoryInfo GetAndroidSdkHome()
		{
			var sdkHome = AndroidSdk.FindHome();

			if (sdkHome == null)
				throw new Exception("Android SDK Not Found");

			return sdkHome;
		}

		static IEnumerable<DeviceData> AndroidDevices()
			=> AndroidSdk.GetEmulatorsAndDevices(GetAndroidSdkHome()).Result;

		static AppleDevicesAndSimulators iOSDevices()
		{
			var result = new AppleDevicesAndSimulators();

			Task.WhenAll(
				Task.Run(() => result.Devices = XCode.GetInstrumentsDevices(true)),
				Task.Run(async () => result.Simulators = await XCode.GetDevicePairs()))
				.Wait();

			return result;
		}

		static IEnumerable<DeviceData> AllDevices()
		{
			var result = AndroidDevices();

			if (Util.IsWindows)
				return result;

			var iosDevices = XCode.GetSimulatorsAndDevices();

			return result.Concat(iosDevices);
		}

		static SimpleResult AndroidStartEmulator(IEnumerable<string> args)
		{
			var avdName = args?.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(avdName))
				throw new ArgumentNullException("AvdName");

			var serial = AndroidSdk.StartEmulatorAndWaitForBoot(GetAndroidSdkHome(), avdName);

			return new SimpleResult { Success = !string.IsNullOrWhiteSpace(serial) };
		}

		static SimpleResult Debug()
		{
			var json = Console.ReadLine();

			return new SimpleResult { Success = true };
		}
	}
}
