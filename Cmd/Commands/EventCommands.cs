using Windows.Win32.UI.Accessibility;
using Shared;
using Shared.UIA.EventHandlers;
using static Crayon.Output;
using Windows.Win32;

namespace Cmd.Commands
{
    public class EventCommands
    {
        public static void WatchEvents()
        {
            // Handle Ctrl-C to exit
            var exitEvent = new System.Threading.ManualResetEvent(false);

            var watcherThread = new Thread(() =>
            {
                Console.WriteLine("Watching for events. Press Ctrl-C to exit.");
                var uia = UIAHelpers.CreateUIAutomationInstance();

                var cache = uia.CreateCacheRequest();
                cache.AddProperty(UIA_PROPERTY_ID.UIA_NamePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);

                // This doesn't work for Windows Terminal: GetConsoleWindow returns a proxy HWND.
                // https://github.com/microsoft/terminal/blob/19a85010fe23d486dbc7b832c9c1e54069a1b233/doc/specs/%2312570%20-%20Show%20Hide%20operations%20on%20GetConsoleWindow%20via%20PTY.md
                //
                //IUIAutomationElement? consoleUiaElement = null;
                //var windowHandle = PInvoke.GetConsoleWindow();
                //if (windowHandle == IntPtr.Zero)
                //{
                //    Console.WriteLine(Red("Failed to get console window handle."));
                //}
                //else
                //{
                //    consoleUiaElement = uia.ElementFromHandle(windowHandle);
                //    if (consoleUiaElement == null)
                //    {
                //        Console.WriteLine(Red("Failed to get UIA element from console window handle."));
                //    }
                //}

                var focusHandler = new FocusChangedEventHandler();
                focusHandler.FocusChanged += (sender, e) =>
                {
                    var element = e.Element;
                    var name = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>();
                    var automationId = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId).As<string>() ?? string.Empty;
                    var controlType = (UIA_CONTROLTYPE_ID)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId).As<int>();
                    var localizedControlType = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;

                    // 09:54:12.123
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Console.WriteLine($"{Dim(timestamp)} {Dim(Blue("[Focus]"))} {Green($"'{name}'")} {Dim($"({Green(localizedControlType)} [{controlType}] - Id='{Blue(automationId)}')")}");
                };

                uia.CreateEventHandlerGroup(out var group);

                var structureChangedHandler = new StructureChangedEventHandler();
                structureChangedHandler.StructureChanged += (sender, e) =>
                {
                    var element = e.Sender;
                    var changeType = e.ChangeType;

                    var name = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
                    string automationId = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId).As<string>() ?? string.Empty;
                    var controlType = (UIA_CONTROLTYPE_ID)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId).As<int>();
                    string localizedControlType = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;

                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Console.WriteLine($"{Dim(timestamp)} {Dim(Yellow("[StructureChanged]"))} {Green($"'{name}'")} {Dim($"({Green(localizedControlType)} [{controlType}] - Id='{Blue(automationId)}')")} ChangeType={Yellow(changeType.ToString())}");
                };
                group.AddStructureChangedEventHandler(TreeScope.TreeScope_Descendants | TreeScope.TreeScope_Element, cache, structureChangedHandler);

                var notificationHandler = new NotificationEventHandler();
                notificationHandler.NotificationReceived += (sender, e) =>
                {
                    var element = e.Sender;

                    // Avoid notifications from children of our own console window
                    // Doesn't work; see above.
                    //if (consoleUiaElement != null)
                    //{
                    //    var consoleRuntimeId = (int[])consoleUiaElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
                    //    var bound = consoleUiaElement.CurrentBoundingRectangle;
                    //    var currentAncestor = element;
                    //    var walker = uia.RawViewWalker;
                    //    while (currentAncestor != null)
                    //    {
                    //        var ancestorRuntimeId = (int[])currentAncestor.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
                    //        Console.WriteLine($"ancestor: {string.Join(",", ancestorRuntimeId)} {currentAncestor.CurrentLocalizedControlType} vs {consoleUiaElement.CurrentLocalizedControlType} {string.Join(",", consoleRuntimeId)} {bound.left},{bound.top},{bound.right},{bound.bottom}");
                    //        if (uia.CompareElements(currentAncestor, consoleUiaElement))
                    //        {
                    //            // Ignoring notification from console window
                    //            return;
                    //        }
                    //        currentAncestor = walker.GetParentElement(currentAncestor);
                    //    }
                    //}

                    var name = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
                    var automationId = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId).As<string>() ?? string.Empty;
                    var controlType = (UIA_CONTROLTYPE_ID)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId).As<int>();
                    var localizedControlType = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;

                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    // Console.WriteLine($"{Dim(timestamp)} {Dim(Magenta("[Notification]"))} {Green($"'{name}'")} {Dim($"({Green(localizedControlType)} [{controlType}] - Id='{Blue(automationId)}')")} Kind={Magenta(e.NotificationKind.ToString())} Processing={Magenta(e.NotificationProcessing.ToString())} Message='{e.DisplayString}'");
                    Console.WriteLine($"{Dim(timestamp)} {Dim(Magenta("[Notification]"))} {Green($"'{name}'")} {Dim($"({Green(localizedControlType)} [{controlType}] - Id='{Blue(automationId)}')")} Kind={Magenta(e.NotificationKind.ToString())} Processing={Magenta(e.NotificationProcessing.ToString())} ");
                    Console.WriteLine(Dim($"    '{e.DisplayString}'"));
                };
                group.AddNotificationEventHandler(TreeScope.TreeScope_Descendants | TreeScope.TreeScope_Element, cache, notificationHandler);

                var rootElement = uia.GetRootElement();
                var rootName = rootElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
                var rootAutomationId = rootElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId).As<string>() ?? string.Empty;
                var rootControlType = (UIA_CONTROLTYPE_ID)rootElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId).As<int>();
                var rootLocalizedControlType = rootElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;
                Console.WriteLine(Dim($"Root Element: '{rootName}' ({rootLocalizedControlType} [{rootControlType}] - Id='{rootAutomationId}')"));

                uia.AddEventHandlerGroup(rootElement, group);
                uia.AddFocusChangedEventHandler(cache, focusHandler);
                // var properties = new UIA_PROPERTY_ID[] { UIA_PROPERTY_ID.UIA_NamePropertyId };
                // uia.AddPropertyChangedEventHandlerNativeArray(rootElement, TreeScope.TreeScope_Descendants | TreeScope.TreeScope_Element, cache, null, properties, properties.Length);

                exitEvent.WaitOne();

                uia.RemoveFocusChangedEventHandler(focusHandler);
                uia.RemoveEventHandlerGroup(rootElement, group);

                Console.WriteLine("Done!");
            });

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Exiting...");
                exitEvent.Set();
            };

            Console.WriteLine(Dim("Starting watcher..."));
            watcherThread.IsBackground = true;
            watcherThread.Name = "UIA Event Watcher";
            watcherThread.Start();

            // Keep the application running
            exitEvent.WaitOne();

            Console.WriteLine(Dim("Waiting for watcher thread to exit..."));

            watcherThread.Join();
        }
    }
}
