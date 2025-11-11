using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace IKuaiDeviceMonitor.Views;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer;

    public ToastWindow(string title, string message, int duration)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(duration) };
        _timer.Tick += OnTimerTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth - 20;
            Top = workArea.Bottom - ActualHeight - 20;
            SlideIn();
            _timer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Toast window load failed: {ex.Message}");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Tick -= OnTimerTick;
        _timer.Stop();
        Loaded -= OnLoaded;
        Closed -= OnClosed;
    }

    private void SlideIn()
    {
        var anim = new DoubleAnimation(Top + 50, Top, TimeSpan.FromMilliseconds(300))
            { EasingFunction = new QuadraticEase() };
        BeginAnimation(TopProperty, anim);
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    public static bool IsFullscreen()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            if (!GetWindowRect(hwnd, out var rect)) return false;

            var screen = Screen.FromHandle(hwnd);
            return rect.Right - rect.Left >= screen.Bounds.Width &&
                   rect.Bottom - rect.Top >= screen.Bounds.Height;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fullscreen check failed: {ex.Message}");
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}