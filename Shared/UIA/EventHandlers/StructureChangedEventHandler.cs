using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Accessibility;

namespace Shared.UIA.EventHandlers;

[GeneratedComClass]
public partial class StructureChangedEventHandler : IUIAutomationStructureChangedEventHandler
{
    public struct StructureChangedEventArgs
    {
        public required readonly IUIAutomationElement Sender { get; init; }
        public required readonly StructureChangeType ChangeType { get; init; }
        public required readonly int[]? RuntimeId { get; init;  }
    }
    public event EventHandler<StructureChangedEventArgs>? StructureChanged;

    public unsafe void HandleStructureChangedEvent(IUIAutomationElement sender, StructureChangeType changeType, SAFEARRAY* runtimeId)
    {
        // TODO: this is only null when removing elements.
        int[]? id = null;
        var args = new StructureChangedEventArgs()
        {
            Sender = sender,
            ChangeType = changeType,
            RuntimeId = id,
        };
        StructureChanged?.Invoke(this, args);
    }
}
