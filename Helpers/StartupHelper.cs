using System.Diagnostics;
using Microsoft.Win32;

namespace IKuaiDeviceMonitor.Helpers;

public static class StartupHelper
{
    private const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "IKuaiDeviceMonitor";

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
            if (key == null)
            {
                Debug.WriteLine("Failed to open registry key");
                return;
            }

            if (enable)
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                    key.SetValue(AppName, processPath);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set startup: {ex.Message}");
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check startup status: {ex.Message}");
            return false;
        }
    }
}