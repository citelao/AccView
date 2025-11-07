using Microsoft.UI.Xaml;
using System.Linq;
using Windows.Foundation;
using WinUIEx;

namespace AccView;

public sealed partial class OverlayWindow : WinUIEx.WindowEx
{
    public OverlayWindow()
    {
        InitializeComponent();

        this.IsTitleBarVisible = false;
        this.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent);
        MoveFocusRing(new Rect(0, 0, 100, 100));

        // Make the window full-screen on the primary monitor.
        // TODO: handle other screens
        // TODO: handle window changes
        this.Maximize();
        //var monitors = MonitorInfo.GetDisplayMonitors();
        //var primaryMonitor = monitors.First(m => m.IsPrimary);
        //this.MoveAndResize(primaryMonitor.RectMonitor.X, primaryMonitor.RectMonitor.Y, primaryMonitor.RectMonitor.Width, primaryMonitor.RectMonitor.Height);
    }

    public void MoveFocusRing(Rect regionInPhysicalPixels)
    {
        var rect = RectHelper.FromCoordinatesAndDimensions(
                    (float)regionInPhysicalPixels.X,
                    (float)regionInPhysicalPixels.Y,
                    (float)regionInPhysicalPixels.Width,
                    (float)regionInPhysicalPixels.Height);
        this.SetRegion(Region.CreateRectangle(regionInPhysicalPixels));
    }

    private double ToDp(double pixels)
    {
        var dpi = this.GetDpiForWindow();
        return (pixels * 96 / dpi);
    }
}
