namespace DotNetWorkspaceAnalyzer;

public interface IAppleDeviceService : IDeviceService
{
}

public interface IAndroidDeviceService : IDeviceService
{
}

public interface IDeviceService
{
	event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	Task StartWatching(params string[] targetPlatformIdentifiers);
	Task StopWatching();

	Task<IEnumerable<DeviceData>> GetDevices(params string[] targetPlatformIdentifiers);
}
