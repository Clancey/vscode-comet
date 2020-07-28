using System.IO;

namespace VsCodeXamarinUtil.Android
{
	internal class AdbRunner
	{
		public AdbRunner(FileInfo adbTool)
		{
			AdbTool = adbTool;
		}

		FileInfo AdbTool { get; }

		internal void AddSerial(string serial, ProcessArgumentBuilder builder)
		{
			if (!string.IsNullOrEmpty(serial))
			{
				builder.Append("-s");
				builder.AppendQuoted(serial);
			}
		}

		internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder)
			=> RunAdb(androidSdkHome, builder, System.Threading.CancellationToken.None);

		internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken)
		{
			var adbToolPath = AdbTool;
			if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
				throw new FileNotFoundException("Could not find adb", adbToolPath?.FullName);

			var p = new ProcessRunner(adbToolPath, builder, cancelToken);

			return p.WaitForExit();
		}
	}
}