using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.CodeAnalysis.Operations;
using Newtonsoft.Json;

namespace DotNetWorkspaceAnalyzer;

public class XCode
{
	public static async Task<List<DeviceData>> GetDevices(params string[] targetPlatformIdentifiers)
	{
		var xcode = await GetBestXcode().ConfigureAwait(false);

		var xcdevice = Path.Combine(xcode, "Contents/Developer/usr/bin/xcdevice");

		if (!File.Exists(xcdevice))
			throw new FileNotFoundException(xcdevice);

		var json = new StringBuilder();
		var ir = await Cli.Wrap(xcdevice)
			.WithArguments("list")
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(json))
			.ExecuteAsync();
		
		var xcdevices = JsonConvert.DeserializeObject<List<XcDevice>>(json.ToString()) ?? Enumerable.Empty<XcDevice>();

		var tpidevices = xcdevices
			.Where(d => (targetPlatformIdentifiers == null || targetPlatformIdentifiers.Length <= 0)
				|| (targetPlatformIdentifiers.Intersect(d.DotNetPlatforms)?.Any() ?? false));

		var filteredDevices = tpidevices
			.Select(d => new DeviceData
			{
				IsEmulator = d.Simulator,
				IsRunning = false,
				Name = d.Name,
				Details = d.ModelName + " (" + d.Architecture + ")",
				Platforms = d.DotNetPlatforms,
				Serial = d.Identifier,
				Version = d.OperatingSystemVersion
			})
			.OrderBy(d => d.IsEmulator)
			.ThenBy(d => d.Name);

		return filteredDevices.ToList();
	}

	public static async Task ObserveDevices(Action<IEnumerable<DeviceData>> callback, bool usb, bool wifi, CancellationToken cancelToken, params string[] targetPlatformIdentifiers)
	{
		var xcode = await GetBestXcode().ConfigureAwait(false);

		var xcdevice = Path.Combine(xcode, "Contents/Developer/usr/bin/xcdevice");

		if (!File.Exists(xcdevice))
			throw new FileNotFoundException(xcdevice);

		// Get the initial list/cache of devices
		var deviceCache = await GetDevices(targetPlatformIdentifiers).ConfigureAwait(false);

		var cmd = Cli.Wrap(xcdevice).WithArguments(args =>
		{
			args.Add("observe");

			if (usb && wifi)
				args.Add("--both");
			else if (usb && !wifi)
				args.Add("--usb");
			else if (!usb && wifi)
				args.Add("--wifi");
		});

		await foreach (var cmdEvent in cmd.ListenAsync(cancelToken))
		{
			if (cmdEvent is StandardOutputCommandEvent stdoutEvent)
			{
				var txt = stdoutEvent.Text.Trim();

				if (txt.StartsWith("Attach:", StringComparison.OrdinalIgnoreCase))
				{
					var udid = txt.Substring(7).Trim();
					deviceCache = await GetDevices(targetPlatformIdentifiers).ConfigureAwait(false);
					callback?.Invoke(deviceCache);
				}
				else if (txt.StartsWith("Detach:", StringComparison.OrdinalIgnoreCase))
				{
					var udid = txt.Substring(7).Trim();
					if (deviceCache.RemoveAll(d => d.Serial.Equals(udid)) > 0)
						callback?.Invoke(deviceCache);
				}
			}
		}
	}

	internal static async Task<string> GetBestXcode()
	{
		var selected = await GetSelectedXCodePath().ConfigureAwait(false);

		if (!string.IsNullOrEmpty(selected))
			return selected;

		return FindXCodeInstalls()?.FirstOrDefault();
	}

	static async Task<string> GetSelectedXCodePath()
	{
		var xcselectPath = "/usr/bin/xcode-select";
		if (!File.Exists(xcselectPath))
			return null;

		var stdout = new StringBuilder();
		await Cli.Wrap(xcselectPath)
			.WithArguments("-p")
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
			.ExecuteAsync();

		var xcodeSelectedPath = stdout.ToString().Trim();

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

	public const string PlatformMacOsx = "com.apple.platform.macosx";
	public const string PlatformiPhoneSimulator = "com.apple.platform.iphonesimulator";
	public const string PlatformAppleTvSimulator = "com.apple.platform.appletvsimulator";
	public const string PlatformAppleTv = "com.apple.platform.appletvos";
	public const string PlatformWatchSimulator = "com.apple.platform.watchsimulator";
	public const string PlatformiPhone = "com.apple.platform.iphoneos";
	public const string PlatformWatch = "com.apple.platform.watchos";

	[JsonProperty("simulator")]
	public bool Simulator { get; set; }

	[JsonProperty("operatingSystemVersion")]
	public string OperatingSystemVersion { get; set; }

	[JsonProperty("available")]
	public bool Available { get; set; }

	[JsonProperty("platform")]
	public string Platform { get; set; }

	public bool IsiOS
		=> !string.IsNullOrEmpty(Platform) && (Platform.Equals(PlatformiPhone) || Platform.Equals(PlatformiPhoneSimulator));
	public bool IsTvOS
		=> !string.IsNullOrEmpty(Platform) && (Platform.Equals(PlatformAppleTv) || Platform.Equals(PlatformAppleTvSimulator));
	public bool IsWatchOS
		=> !string.IsNullOrEmpty(Platform) && (Platform.Equals(PlatformWatch) || Platform.Equals(PlatformWatchSimulator));
	public bool IsOsx
		=> !string.IsNullOrEmpty(Platform) && Platform.Equals(PlatformMacOsx);

	public string[] DotNetPlatforms
		=> Platform switch {
			PlatformiPhone => new[] { "ios" },
			PlatformiPhoneSimulator => new[] { "ios" },
			PlatformAppleTv => new[] { "tvos" },
			PlatformAppleTvSimulator => new[] { "tvos" },
			PlatformWatch => new [] { "watchos" },
			PlatformWatchSimulator => new[] { "watchos" },
			PlatformMacOsx => new[] {"macos", "maccatalyst"},
			_ => new string[0]
		};

	[JsonProperty("modelCode")]
	public string ModelCode { get; set; }

	[JsonProperty("identifier")]
	public string Identifier { get; set; }

	[JsonProperty("architecture")]
	public string Architecture { get; set; }

	public string RuntimeIdentifier
		=> Architecture switch
		{
			_ => "iossimulator-x64"
		};

	[JsonProperty("modelUTI")]
	public string ModelUTI { get; set; }

	[JsonProperty("modelName")]
	public string ModelName { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("interface")]
	public string Interface { get; set; }
}
