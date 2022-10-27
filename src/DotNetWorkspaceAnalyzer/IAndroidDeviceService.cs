namespace DotNetWorkspaceAnalyzer;

interface IAndroidDeviceService
{
	event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	Task StartWatching();
	Task StopWatching();

	Task<IEnumerable<DeviceData>> GetDevices();
}
