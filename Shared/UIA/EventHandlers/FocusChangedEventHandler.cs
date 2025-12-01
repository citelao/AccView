using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.UI.Accessibility;

namespace Shared.UIA.EventHandlers;

[GeneratedComClass]
public partial class FocusChangedEventHandler : IUIAutomationFocusChangedEventHandler
{
    public class FocusChangedEventArgs : EventArgs
    {
        public required IUIAutomationElement Element { get; set; }
    }

    public event EventHandler<FocusChangedEventArgs>? FocusChanged;

    public void HandleFocusChangedEvent(IUIAutomationElement sender)
    {
        FocusChanged?.Invoke(this, new FocusChangedEventArgs { Element = sender });
    }
}
