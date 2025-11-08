using Microsoft.UI.Xaml;
using System.Linq;
using Windows.Foundation;
using WinUIEx;
using WinUIEx.Messaging;

namespace AccView;

public sealed partial class OverlayWindow : WinUIEx.WindowEx
{
    private WindowMessageMonitor _messageMonitor;

    public OverlayWindow()
    {
        InitializeComponent();

        this.IsTitleBarVisible = false;
        this.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent | ExtendedWindowStyle.NoActivate | ExtendedWindowStyle.TopMost);
        MoveFocusRing(new Rect(0, 0, 100, 100));

        _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
        _messageMonitor.WindowMessageReceived += MessageMonitor_WindowMessageReceived;

        // Make the window full-screen on the primary monitor.
        // TODO: handle other screens
        // TODO: handle window changes
        //this.Maximize();
        //var monitors = MonitorInfo.GetDisplayMonitors();
        //var primaryMonitor = monitors.First(m => m.IsPrimary);
        //this.MoveAndResize(primaryMonitor.RectMonitor.X, primaryMonitor.RectMonitor.Y, primaryMonitor.RectMonitor.Width, primaryMonitor.RectMonitor.Height);
    }

    private void MessageMonitor_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        var WM_NCHITTEST = 0x0084;
        if (e.Message.MessageId == WM_NCHITTEST)
        {
            e.Handled = true;
            var HTNOWHERE = 0;
            e.Result = HTNOWHERE;
        }
    }

    public void MoveFocusRing(Rect regionInPhysicalPixels)
    {
        var rect = RectHelper.FromCoordinatesAndDimensions(
                    (float)regionInPhysicalPixels.X,
                    (float)regionInPhysicalPixels.Y,
                    (float)regionInPhysicalPixels.Width,
                    (float)regionInPhysicalPixels.Height);
        //this.SetRegion(Region.CreateRectangle(regionInPhysicalPixels));
    }

    private double ToDp(double pixels)
    {
        var dpi = this.GetDpiForWindow();
        return (pixels * 96 / dpi);
    }
}
