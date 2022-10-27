namespace DotNetWorkspaceAnalyzer;

public class AppleDeviceService : IDeviceService
{
	public event EventHandler<IEnumerable<DeviceData>> DevicesChanged;

	public async Task<IEnumerable<DeviceData>> GetDevices(params string[] targetPlatformIdentifiers)
	{
		return await XCode.GetDevices(targetPlatformIdentifiers);
	}

	Task observeTask = Task.CompletedTask;

	CancellationTokenSource ctsObserve = new CancellationTokenSource();

	public Task StartWatching(params string[] targetPlatformIdentifiers)
	{
		if (!ctsObserve.IsCancellationRequested)
		{
			ctsObserve.Cancel();
			ctsObserve = new CancellationTokenSource();
		}

		observeTask = XCode.ObserveDevices(devices => DevicesChanged?.Invoke(this, devices), true, true, ctsObserve.Token, targetPlatformIdentifiers);

		return Task.CompletedTask;
	}

	public Task StopWatching()
	{
		if (!ctsObserve.IsCancellationRequested)
		{
			ctsObserve.Cancel();
			ctsObserve = new CancellationTokenSource();
		}
		return observeTask ?? Task.CompletedTask;
	}
}

