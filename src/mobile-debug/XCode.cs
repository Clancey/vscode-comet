using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VsCodeMobileUtil
{
	public class XCode
	{
		public static List<DeviceData> GetDevices()
		{
			var xcode = GetBestXcode();

			var xcdevice = new FileInfo(Path.Combine(xcode, "Contents/Developer/usr/bin/xcdevice"));

			if (!xcdevice.Exists)
				throw new FileNotFoundException(xcdevice.FullName);

			var ir = ProcessRunner.Run(xcdevice,
				new ProcessArgumentBuilder()
					.Append("list"));

			var json = string.Join(Environment.NewLine, ir.StandardOutput);

			
			var xcdevices = JsonConvert.DeserializeObject<List<XcDevice>>(json);

			return xcdevices.Select(d => new DeviceData
			{
				IsEmulator = d.Simulator,
				IsRunning = false,
				Name = d.Name,
				Platform = d.Platform,
				Serial = d.Identifier,
				Version = d.OperatingSystemVersion
			}).ToList();
		}

		static string GetBestXcode()
		{
			var selected = GetSelectedXCodePath();

			if (!string.IsNullOrEmpty(selected))
				return selected;

			return FindXCodeInstalls()?.FirstOrDefault();
		}

		static string GetSelectedXCodePath()
		{
			var r = ProcessRunner.Run(new FileInfo("/usr/bin/xcode-select"), new ProcessArgumentBuilder().Append("-p"));

			var xcodeSelectedPath = string.Join(Environment.NewLine, r.StandardOutput)?.Trim();

			if (!string.IsNullOrEmpty(xcodeSelectedPath))
			{
				var infoPlist = Path.Combine(xcodeSelectedPath, "..", "Info.plist");
				if (File.Exists(infoPlist))
				{
					var info = GetXcodeInfo(
						Path.GetFullPath(
							Path.Combine(xcodeSelectedPath, "..", "..")), true);

					if (info != null)
						return info?.Path;
				}
			}

			return null;
		}

		static readonly string[] LikelyPaths = new[]
		{
			"/Applications/Xcode.app",
			"/Applications/Xcode-beta.app",
		};

		static IEnumerable<string> FindXCodeInstalls()
		{
			foreach (var p in LikelyPaths)
			{
				var i = GetXcodeInfo(p, false)?.Path;
				if (i != null)
					yield return i;
			}
		}

		static (string Path, bool Selected)? GetXcodeInfo(string path, bool selected)
		{
			var versionPlist = Path.Combine(path, "Contents", "version.plist");

			if (File.Exists(versionPlist))
			{
				return (path, selected);
			}
			else
			{
				var infoPlist = Path.Combine(path, "Contents", "Info.plist");

				if (File.Exists(infoPlist))
				{
					return (path, selected);
				}
			}
			return null;
		}
	}

	public class XcDevice
	{
		[JsonProperty("simulator")]
		public bool Simulator { get; set; }

		[JsonProperty("operatingSystemVersion")]
		public string OperatingSystemVersion { get; set; }

		[JsonProperty("available")]
		public bool Available { get; set; }

		[JsonProperty("platform")]
		public string Platform { get; set; }

		[JsonProperty("modelCode")]
		public string ModelCode { get; set; }

		[JsonProperty("identifier")]
		public string Identifier { get; set; }

		[JsonProperty("architecture")]
		public string Architecture { get; set; }

		[JsonProperty("modelUTI")]
		public string ModelUTI { get; set; }

		[JsonProperty("modelName")]
		public string ModelName { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}