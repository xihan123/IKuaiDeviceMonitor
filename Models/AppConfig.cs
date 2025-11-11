namespace IKuaiDeviceMonitor.Models;

public class AppConfig
{
    public RouterSettings Router { get; set; } = new();
    public NotificationSettings Notification { get; set; } = new();
    public AppSettings App { get; set; } = new();
}

public class RouterSettings
{
    public string Host { get; set; } = "192.168.1.1";
    public int Port { get; set; } = 443;
    public bool Https { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "";
    public int CheckInterval { get; set; } = 30000;
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public int Duration { get; set; } = 5000;
    public bool IgnoreFullscreen { get; set; } = true;
}

public class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public string Theme { get; set; } = "Dark";
}