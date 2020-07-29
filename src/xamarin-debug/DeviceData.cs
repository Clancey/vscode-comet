using System;
using System.Collections.Generic;
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
}
