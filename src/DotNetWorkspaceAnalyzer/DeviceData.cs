using Newtonsoft.Json;

namespace DotNetWorkspaceAnalyzer;

public class DeviceData
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("details")]
	public string Details { get; set; }

	[JsonProperty("serial")]
	public string Serial { get; set; }

	[JsonProperty("platforms")]
	public string[] Platforms { get; set; }

	[JsonProperty("version")]
	public string Version { get; set; }

	[JsonProperty("isEmulator")]
	public bool IsEmulator { get; set; }

	[JsonProperty("isRunning")]
	public bool IsRunning { get; set; }

	[JsonProperty("rid")]
	public string RuntimeIdentifier { get; set; }
}