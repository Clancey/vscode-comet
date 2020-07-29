using System;

namespace VsCodeXamarinUtil.Android
{
	public class AdbDevice
	{
		public string Serial { get; set; }

		public bool IsEmulator
			=> Serial?.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase) ?? false;

		public string Usb { get; set; }

		public string Product { get; set; }

		public string Model { get; set; }

		public string Device { get; set; }
	}
}