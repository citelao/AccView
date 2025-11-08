using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Foundation;
using WinUIEx;
using WinUIEx.Messaging;

namespace AccView;

public sealed partial class OverlayWindow : WinUIEx.WindowEx
{
    public OverlayWindow()
    {
        InitializeComponent();

        this.IsTitleBarVisible = false;
        this.SetExtendedWindowStyle(ExtendedWindowStyle.Transparent | ExtendedWindowStyle.NoActivate | ExtendedWindowStyle.TopMost | ExtendedWindowStyle.Layered);
        MoveFocusRing(new Rect(0, 0, 100, 100));

        // Make the window full-screen on the primary monitor.
        // TODO: handle other screens
        // TODO: handle window changes
        // TODO: draw over the Taskbar.
        this.Maximize();
        //var monitors = MonitorInfo.GetDisplayMonitors();
        //var primaryMonitor = monitors.First(m => m.IsPrimary);
        //this.MoveAndResize(primaryMonitor.RectMonitor.X, primaryMonitor.RectMonitor.Y, primaryMonitor.RectMonitor.Width, primaryMonitor.RectMonitor.Height);
    }

    public void MoveFocusRing(Rect regionInPhysicalPixels)
    {
        // TODO: this won't work across multiple monitors with different DPIs
        Canvas.SetLeft(ItemBorder, ToDp(regionInPhysicalPixels.X));
        Canvas.SetTop(ItemBorder, ToDp(regionInPhysicalPixels.Y));

        // The border draws on the inside, so widen.
        var marginIncrease = Math.Abs(ItemBorder.Margin.Left) + Math.Abs(ItemBorder.Margin.Right);

        ItemBorder.Height = ToDp(regionInPhysicalPixels.Height) + marginIncrease;
        ItemBorder.Width = ToDp(regionInPhysicalPixels.Width) + marginIncrease;

        Canvas.SetLeft(SizeBorder, ToDp(regionInPhysicalPixels.X));
        Canvas.SetTop(SizeBorder, ToDp(regionInPhysicalPixels.Y));
        SizeTextBlock.Text = $"({regionInPhysicalPixels.X},{regionInPhysicalPixels.Y})";
    }

    private double ToDp(double pixels)
    {
        var dpi = this.GetDpiForWindow();
        return (pixels * 96 / dpi);
    }
}
