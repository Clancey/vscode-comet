namespace DotNetWorkspaceAnalyzer;

interface IAppleDeviceService
{
	event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	Task StartWatching();
	Task StopWatching();

	Task<IEnumerable<DeviceData>> GetDevices(bool includeUsb = true, bool includeWifi = true);
}
