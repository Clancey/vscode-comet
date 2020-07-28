using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VsCodeXamarinUtil
{
	public class XCode
	{
		const string patternInstrumentsDevices = @"(?<name>.*?)(\((?<version>[0-9.]+)\))?\s+\[(?<serial>[0-9A-Z\-]+)\]\s+?(?<sim>\(Simulator\)){0,1}";

		static Regex rxInstrumentDevices = new Regex(patternInstrumentsDevices, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		public static List<DeviceData> GetEmulatorsAndDevices()
		{
			var results = new List<DeviceData>();

			var instruments = new FileInfo(Path.Combine("usr", "bin", "instruments"));

			if (!instruments.Exists)
				return results;

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

				var match = rxInstrumentDevices.Matches(line)?.FirstOrDefault();

				if (match == null)
					continue;

				var name = match.Groups?["name"]?.Value;
				var serial = match.Groups?["serial"]?.Value;
				var version = match.Groups?["version"]?.Value;
				var sim = match.Groups?["sim"]?.Value;

				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrEmpty(serial))
				{
					results.Add(new DeviceData
					{
						Serial = serial,
						IsEmulator = !string.IsNullOrWhiteSpace(sim),
						IsRunning = false,
						Name = name
					});
				}
			}

			return results;
		}
	}
}