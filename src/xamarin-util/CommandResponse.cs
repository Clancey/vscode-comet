using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VsCodeXamarinUtil
{
	public class CommandResponse
	{
		public CommandResponse()
		{
		}

		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("command")]
		public string Command { get; set; }

		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("response")]
		public object Response { get; set; }
	}
}
