using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Shared.UIA.EventHandlers;

[GeneratedComClass]
public partial class NotificationEventHandler : IUIAutomationNotificationEventHandler
{
    public struct NotificationEventArgs
    {
        public required readonly IUIAutomationElement Sender { get; init; }
        public required readonly NotificationKind NotificationKind { get; init; }
        public required readonly NotificationProcessing NotificationProcessing { get; init; }
        public required readonly string DisplayString { get; init; }
        public required readonly string ActivityId { get; init; }
    }
    public event EventHandler<NotificationEventArgs>? NotificationReceived;

    public void HandleNotificationEvent(IUIAutomationElement sender, NotificationKind notificationKind, NotificationProcessing notificationProcessing, BSTR displayString, BSTR activityId)
    {
        var args = new NotificationEventArgs()
        {
            Sender = sender,
            NotificationKind = notificationKind,
            NotificationProcessing = notificationProcessing,
            DisplayString = displayString.ToString(),
            ActivityId = activityId.ToString(),
        };
        NotificationReceived?.Invoke(this, args);
    }
}
