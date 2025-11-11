using System.Diagnostics;
using System.Windows.Threading;
using IKuaiDeviceMonitor.Helpers;
using IKuaiDeviceMonitor.Models;

namespace IKuaiDeviceMonitor.Services;

public class DeviceMonitorService : IDisposable
{
    private readonly IKuaiClient _client;
    private readonly object _lock = new();
    private readonly DispatcherTimer _timer;
    private Dictionary<string, Device> _devices = new();
    private bool _disposed;
    private HashSet<string> _knownMacs;
    private Task? _saveTask;

    public DeviceMonitorService(IKuaiClient client, int interval, HashSet<string> knownMacs)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _knownMacs = knownMacs;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
        _timer.Tick += OnTimerTick;
    }

    public async void Dispose()
    {
        if (_disposed) return;
        _timer.Tick -= OnTimerTick;
        _timer.Stop();
        if (_saveTask != null) await _saveTask;
        _disposed = true;
    }

    public event Action<Device>? DeviceOnline;
    public event Action<Device>? DeviceOffline;
    public event Action<List<Device>>? DevicesUpdated;

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await CheckDevicesAsync();
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private async Task CheckDevicesAsync()
    {
        try
        {
            var param = new
                { TYPE = "data,total", ORDER_BY = "ip_addr_int", orderType = "IP", limit = "0,20", ORDER = "" };
            var response = await _client.ExecAsync<DeviceResponse>("monitor_lanip", "show", param);

            if (response?.Data?.Data == null) return;

            var currentDevices = response.Data.Data
                .Where(d => d.ConnectNum > 0)
                .ToDictionary(d => d.Mac);
            var currentMacs = currentDevices.Keys.ToHashSet();

            var hasChanges = false;
            lock (_lock)
            {
                foreach (var device in currentDevices.Values.Where(d => !_knownMacs.Contains(d.Mac)))
                {
                    DeviceOnline?.Invoke(device);
                    hasChanges = true;
                }

                foreach (var device in _devices.Values.Where(d =>
                             !currentMacs.Contains(d.Mac) && _knownMacs.Contains(d.Mac)))
                {
                    DeviceOffline?.Invoke(device);
                    hasChanges = true;
                }

                _devices = currentDevices;
                _knownMacs = currentMacs;
            }

            if (hasChanges) _saveTask = DeviceStateHelper.SaveAsync(_knownMacs);

            DevicesUpdated?.Invoke(response.Data.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Device check failed: {ex.Message}");
        }
    }
}