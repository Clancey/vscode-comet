namespace DotNetWorkspaceAnalyzer;

public class AndroidDeviceService : IAndroidDeviceService
{
	public event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	public Task<IEnumerable<DeviceData>> GetDevices()
	{
		throw new NotImplementedException();
	}

	public Task StartWatching()
	{
		throw new NotImplementedException();
	}

	public Task StopWatching()
	{
		throw new NotImplementedException();
	}
}