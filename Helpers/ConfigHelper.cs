using System.Diagnostics;
using System.IO;
using IKuaiDeviceMonitor.Models;
using Newtonsoft.Json;

namespace IKuaiDeviceMonitor.Helpers;

public static class ConfigHelper
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IKuaiDeviceMonitor", "config.json");

    public static async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        return new AppConfig();
    }

    public static async Task SaveAsync(AppConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save config: {ex.Message}");
            throw;
        }
    }
}