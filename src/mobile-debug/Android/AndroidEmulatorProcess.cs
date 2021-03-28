using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace VsCodeMobileUtil.Android
{
	public class AndroidEmulatorProcess
	{
		internal AndroidEmulatorProcess(ProcessRunner p, string avdName, DirectoryInfo sdkHome)
		{
			process = p;
			androidSdkHome = sdkHome;
			AvdName = avdName;
		}

		readonly ProcessRunner process;
		readonly DirectoryInfo androidSdkHome;
		ProcessResult result;

		public string Serial { get; private set; }

		public string AvdName { get; private set; }

		public int WaitForExit()
		{
			result = process.WaitForExit();

			return result.ExitCode;
		}

		public bool Shutdown()
		{
			var success = false;

			if (!string.IsNullOrWhiteSpace(Serial))
			{
				var adb = new Adb(androidSdkHome);

				try { success = adb.EmuKill(Serial); }
				catch { }
			}

			if (process != null && !process.HasExited)
			{
				try { process.Kill(); success = true; }
				catch { }
			}

			return success;
		}

		public IEnumerable<string> GetStandardOutput()
			=> result?.StandardOutput ?? new List<string>();


		public bool WaitForBootComplete()
			=> WaitForBootComplete(TimeSpan.Zero);

		public bool WaitForBootComplete(TimeSpan timeout)
		{
			var cts = new CancellationTokenSource();

			if (timeout != TimeSpan.Zero)
				cts.CancelAfter(timeout);

			return WaitForBootComplete(cts.Token);
		}

		public bool WaitForBootComplete(CancellationToken token)
		{
			var adb = new Adb(androidSdkHome);

			var booted = false;
			Serial = null;

			// Keep trying to see if the boot complete prop is set
			while (string.IsNullOrEmpty(Serial) && !token.IsCancellationRequested)
			{
				if (process.HasExited)
					return false;

				Thread.Sleep(1000);

				// Get a list of devices, we need to find the device we started
				var devices = adb.GetDevices();

				// Find the device we just started and get it's adb serial
				foreach (var d in devices)
				{
					try
					{
						var name = adb.GetEmulatorName(d.Serial);
						if (name.Equals(AvdName, StringComparison.OrdinalIgnoreCase))
						{
							Serial = d.Serial;
							break;
						}
					}
					catch { }
				}
			}

			while (!token.IsCancellationRequested)
			{
				if (process.HasExited)
					return false;

				if (adb.Shell("getprop dev.bootcomplete", Serial).Any(l => l.Contains("1")))
				{
					booted = true;
					break;
				}
				else
				{
					Thread.Sleep(1000);
				}
			}

			return booted;
		}
	}
}
