using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace VsCodeXamarinUtil
{
	public class DeviceData
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("serial")]
		public string Serial { get; set; }

		[JsonProperty("platform")]
		public string Platform { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("isEmulator")]
		public bool IsEmulator { get; set; }

		[JsonProperty("isRunning")]
		public bool IsRunning { get; set; }
	}

	public class SimpleResult
	{
		[JsonProperty("success")]
		public bool Success { get; set; }
	}
	public enum ProjectType {
		Mono,
		Android,
		iOS,
		UWP,
		Unknown,
		WPF,
		Blazor,

	}
	public class LaunchData {
		public string AppName { get; set; }
		public string Project { get; set; }
		public string Configuration { get; set; }
		public string Platform { get; set; }
		public ProjectType ProjectType { get; set; }
		public string OutputDirectory { get; set; }
		public bool IsSim { get; set; }
		public bool EnableHotReload { get; set; }
		public string iOSDeviceId { get; set; }
		public string iOSSimulatorDeviceOS { get; set; }
		public string iOSSimulatorDeviceType { get; set; }
		public string AdbDeviceName { get; set; }
		public string AdbDeviceId { get; set; }


		public LaunchData ()
		{

		}
		public LaunchData(dynamic args)
		{
			Project = getString (args, nameof(Project));
			Configuration = getString (args, nameof(Configuration));
			Platform = getString (args, nameof(Platform), "AnyCPU");
			OutputDirectory = getString (args, nameof (OutputDirectory));
			IsSim = getBool (args, nameof (IsSim));
			EnableHotReload = getBool (args, nameof (EnableHotReload));
			iOSDeviceId = getString (args, nameof (iOSDeviceId));
			iOSSimulatorDeviceOS = getString (args, nameof (iOSSimulatorDeviceOS));
			iOSSimulatorDeviceType = getString (args, nameof (iOSSimulatorDeviceType));
			AdbDeviceName = getString (args, nameof (AdbDeviceName));
			AdbDeviceId = getString (args, nameof (AdbDeviceId));
			var projectTypeString = getString (args, nameof (ProjectType));
			ProjectType = Enum.Parse (typeof(ProjectType), projectTypeString);
		}

		public (bool success, string message) Validate ()
		{
			(bool success, string message) validateString (string value, string name)
				=> string.IsNullOrWhiteSpace(value) ? (false, $"{name} is not valid") : (true, "");
			var checks = new[] {
				validateString(Project,nameof(Project)),
				validateString(Configuration,nameof(Configuration)),
				validateString(OutputDirectory,nameof(OutputDirectory)),
			};
			var failed = checks.FirstOrDefault (x => !x.success);	
			if (!failed.success)
				return failed;
			if(ProjectType == ProjectType.iOS) {
				if (IsSim) {
					if (string.IsNullOrWhiteSpace (iOSSimulatorDeviceOS) || string.IsNullOrWhiteSpace (iOSSimulatorDeviceType))
						return (false, "iOS simulator is not valid");

				} else if (string.IsNullOrWhiteSpace (iOSDeviceId))
					return (false, $"{nameof (iOSDeviceId)} is not valid");
			}
			else if(ProjectType == ProjectType.Android) {

			}
		
			return (true, "");
		}

		private static bool getBool (dynamic container, string propertyName, bool dflt = false)
		{
			try {
				return (bool)container [propertyName];
			} catch (Exception) {
				// ignore and return default value
			}
			return dflt;
		}

		private static int getInt (dynamic container, string propertyName, int dflt = 0)
		{
			try {
				return (int)container [propertyName];
			} catch (Exception) {
				// ignore and return default value
			}
			return dflt;
		}

		private static string getString (dynamic args, string property, string dflt = null)
		{
			var s = (string)args [property];
			if (s == null) {
				return dflt;
			}
			s = s.Trim ();
			if (s.Length == 0) {
				return dflt;
			}
			return s;
		}

	}
}
