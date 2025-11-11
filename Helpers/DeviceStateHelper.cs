using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace IKuaiDeviceMonitor.Helpers;

public static class DeviceStateHelper
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IKuaiDeviceMonitor", "devices.json");

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task<HashSet<string>> LoadAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (File.Exists(StatePath))
                {
                    var json = await File.ReadAllTextAsync(StatePath);
                    return JsonConvert.DeserializeObject<HashSet<string>>(json) ?? new HashSet<string>();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load device state: {ex.Message}");
        }

        return new HashSet<string>();
    }

    public static async Task SaveAsync(HashSet<string> macs)
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var directory = Path.GetDirectoryName(StatePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(macs, Formatting.Indented);
                await File.WriteAllTextAsync(StatePath, json);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save device state: {ex.Message}");
        }
    }
}