using System;

namespace VSCodeDebug
{
	public enum ProjectType
	{
		Mono,
		Android,
		iOS,
		UWP,
		Unknown,
		WPF,
		Blazor,

	}

	public class XamarinOptions
	{
		public string CSProj { get; set; }
		public string Configuration { get; set; }
		public string Platform { get; set; }
		public ProjectType ProjectType { get; set; }
		public bool IsSim { get; set; }
		public string AppName { get; set; }

		public bool IsComet { get; set; }

		public string OutputFolder { get; set; }

		public string iOSDeviceId { get; set; }
		public string iOSSimulatorDeviceOS { get; set; }
		public string iOSSimulatorDeviceType { get; set; }
		public string AdbDeviceName { get; set; }
		public string AdbDeviceId { get; set; }
	}
}
