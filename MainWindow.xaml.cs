using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using IKuaiDeviceMonitor.Helpers;
using IKuaiDeviceMonitor.Models;
using IKuaiDeviceMonitor.Services;
using IKuaiDeviceMonitor.Views;
using ModernWpf;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;

namespace IKuaiDeviceMonitor;

public partial class MainWindow : Window
{
    private readonly NotifyIcon _notifyIcon;
    private IKuaiClient? _client;
    private AppConfig _config;
    private DeviceMonitorService? _monitor;

    public MainWindow()
    {
        InitializeComponent();
        _config = new AppConfig();

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "爱快设备监控"
        };
        _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
        _notifyIcon.ContextMenuStrip = CreateContextMenu();

        Loaded += OnLoaded;
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _config = await ConfigHelper.LoadAsync();
            ThemeManager.Current.ApplicationTheme =
                _config.App.Theme == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light;
            UpdateThemeButton();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _client?.Dispose();
            _monitor?.Dispose();

            _client = new IKuaiClient(_config.Router.Host, _config.Router.Port, _config.Router.Https);
            if (await _client.LoginAsync(_config.Router.Username, _config.Router.Password))
            {
                StatusText.Text = "● 已连接";
                StatusText.Foreground = Brushes.Green;

                var knownMacs = await DeviceStateHelper.LoadAsync();
                _monitor = new DeviceMonitorService(_client, _config.Router.CheckInterval, knownMacs);
                _monitor.DeviceOnline += OnDeviceOnline;
                _monitor.DeviceOffline += OnDeviceOffline;
                _monitor.DevicesUpdated += OnDevicesUpdated;
                _monitor.Start();
            }
            else
            {
                StatusText.Text = "● 连接失败";
                StatusText.Foreground = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Initialization failed: {ex.Message}");
            StatusText.Text = "● 连接错误";
            StatusText.Foreground = Brushes.Red;
        }
    }

    private void OnDeviceOnline(Device device)
    {
        try
        {
            if (_config.Notification.Enabled && (!_config.Notification.IgnoreFullscreen || !ToastWindow.IsFullscreen()))
                Dispatcher.BeginInvoke(() =>
                    new ToastWindow("设备上线", $"{device.Hostname}\n{device.IpAddr}", _config.Notification.Duration)
                        .Show());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show online notification: {ex.Message}");
        }
    }

    private void OnDeviceOffline(Device device)
    {
        try
        {
            if (_config.Notification.Enabled && (!_config.Notification.IgnoreFullscreen || !ToastWindow.IsFullscreen()))
                Dispatcher.BeginInvoke(() =>
                    new ToastWindow("设备下线", $"{device.Hostname}\n{device.IpAddr}", _config.Notification.Duration)
                        .Show());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show offline notification: {ex.Message}");
        }
    }

    private void OnDevicesUpdated(List<Device> devices)
    {
        try
        {
            Dispatcher.BeginInvoke(() =>
            {
                DeviceList.ItemsSource = devices;
                DeviceCountText.Text = $"设备数: {devices.Count}";
                LastUpdateText.Text = $"最后更新: {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to update device list: {ex.Message}");
        }
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) =>
        {
            Show();
            WindowState = WindowState.Normal;
        });
        menu.Items.Add("设置", null, (_, _) => Settings_Click(null!, null!));
        menu.Items.Add("-");
        menu.Items.Add("退出", null, (_, _) => Application.Current.Shutdown());
        return menu;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _monitor?.Stop();
        await InitializeAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(_config) { Owner = this };
        if (settings.ShowDialog() == true)
        {
            _config = settings.Config;
            _monitor?.Stop();
            _ = InitializeAsync();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _config.App.MinimizeToTray)
            Hide();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            _monitor?.Stop();

            if (_monitor != null)
            {
                _monitor.DeviceOnline -= OnDeviceOnline;
                _monitor.DeviceOffline -= OnDeviceOffline;
                _monitor.DevicesUpdated -= OnDevicesUpdated;
                _monitor.Dispose();
            }

            _client?.Dispose();

            _notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            Loaded -= OnLoaded;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    private async void Theme_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var isDark = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
            ThemeManager.Current.ApplicationTheme = isDark ? ApplicationTheme.Light : ApplicationTheme.Dark;
            _config.App.Theme = isDark ? "Light" : "Dark";
            await ConfigHelper.SaveAsync(_config);
            UpdateThemeButton();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to change theme: {ex.Message}");
        }
    }

    private void UpdateThemeButton()
    {
        ThemeButton.Content = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? "☀️" : "🌙";
    }
}