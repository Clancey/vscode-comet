using System;
using System.Collections.Generic;
using System.Text;

namespace VsCodeXamarinUtil
{
	public class DeviceData
	{
		public string Name { get; set; }
		public string Serial { get; set; }
		public bool IsEmulator { get; set; }
		public bool IsRunning { get; set; }
		public string Version { get; set; }
		public string Platform { get; set; }
	}

	public class SimpleResult
	{
		public bool Success { get; set; }
	}
}
