using Microsoft.UI.Xaml;
using Windows.Foundation;
using WinUIEx;

namespace AccView;

public sealed partial class OverlayWindow : WinUIEx.WindowEx
{
    public OverlayWindow()
    {
        InitializeComponent();

        this.IsTitleBarVisible = false;
        this.SetRegion(Region.CreateRectangle(new Rect(0, 0, 100, 100)));
    }
}
