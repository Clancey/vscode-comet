using System;

namespace DotNetWorkspaceAnalyzer;

public class AppleDeviceService : IAppleDeviceService
{
	public event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	public Task<IEnumerable<DeviceData>> GetDevices(bool includeUsb = true, bool includeWifi = true)
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

