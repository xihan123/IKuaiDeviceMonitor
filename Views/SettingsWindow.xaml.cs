using System.Diagnostics;
using System.Windows;
using IKuaiDeviceMonitor.Helpers;
using IKuaiDeviceMonitor.Models;
using MessageBox = System.Windows.MessageBox;

namespace IKuaiDeviceMonitor.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();
        Config = config ?? throw new ArgumentNullException(nameof(config));
        LoadConfig();
    }

    public AppConfig Config { get; }

    private void LoadConfig()
    {
        try
        {
            HostTextBox.Text = Config.Router.Host;
            PortTextBox.Text = Config.Router.Port.ToString();
            HttpsCheckBox.IsChecked = Config.Router.Https;
            UsernameTextBox.Text = Config.Router.Username;
            PasswordBox.Password = Config.Router.Password;
            IntervalTextBox.Text = Config.Router.CheckInterval.ToString();
            NotificationCheckBox.IsChecked = Config.Notification.Enabled;
            FullscreenCheckBox.IsChecked = Config.Notification.IgnoreFullscreen;
            StartupCheckBox.IsChecked = Config.App.StartWithWindows;
            MinimizeCheckBox.IsChecked = Config.App.MinimizeToTray;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
            MessageBox.Show("加载设置失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(HostTextBox.Text))
            {
                MessageBox.Show("请输入路由器地址", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号 (1-65535)", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(IntervalTextBox.Text, out var interval) || interval < 1000)
            {
                MessageBox.Show("请输入有效的检查间隔 (≥ 1000 毫秒)", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Config.Router.Host = HostTextBox.Text.Trim();
            Config.Router.Port = port;
            Config.Router.Https = HttpsCheckBox.IsChecked ?? true;
            Config.Router.Username = UsernameTextBox.Text.Trim();
            Config.Router.Password = PasswordBox.Password;
            Config.Router.CheckInterval = interval;
            Config.Notification.Enabled = NotificationCheckBox.IsChecked ?? true;
            Config.Notification.IgnoreFullscreen = FullscreenCheckBox.IsChecked ?? true;
            Config.App.StartWithWindows = StartupCheckBox.IsChecked ?? false;
            Config.App.MinimizeToTray = MinimizeCheckBox.IsChecked ?? true;

            Debug.WriteLine($"Saving config with password length: {Config.Router.Password?.Length ?? 0}");
            await ConfigHelper.SaveAsync(Config);
            StartupHelper.SetStartup(Config.App.StartWithWindows);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
            MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}