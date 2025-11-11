using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace IKuaiDeviceMonitor;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutex = new Mutex(true, "IKuaiDeviceMonitor_SingleInstance", out var createdNew);
            if (!createdNew)
            {
                MessageBox.Show("应用程序已在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Mutex creation failed: {ex.Message}");
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}