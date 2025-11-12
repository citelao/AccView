using Windows.Win32.UI.Accessibility;
using Shared;
using Shared.UIA.EventHandlers;
using static Crayon.Output;

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
                Console.WriteLine("Watching for focus changed events. Press Ctrl-C to exit.");
                var uia = UIAHelpers.CreateUIAutomationInstance();

                var cache = uia.CreateCacheRequest();
                cache.AddProperty(UIA_PROPERTY_ID.UIA_NamePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);

                var focusHandler = new FocusChangedEventHandler();
                focusHandler.FocusChanged += (sender, e) =>
                {
                    var element = e.Element;
                    var name = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
                    var automationId = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                    var controlType = (UIA_CONTROLTYPE_ID)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                    var localizedControlType = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);

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

                    var name = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
                    var automationId = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                    var controlType = (UIA_CONTROLTYPE_ID)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                    var localizedControlType = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);

                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    Console.WriteLine($"{Dim(timestamp)} {Dim(Yellow("[StructureChanged]"))} {Green($"'{name}'")} {Dim($"({Green(localizedControlType)} [{controlType}] - Id='{Blue(automationId)}')")} ChangeType={Yellow(changeType.ToString())}");
                };
                group.AddStructureChangedEventHandler(TreeScope.TreeScope_Descendants, cache, structureChangedHandler);

                var rootElement = uia.GetRootElement();
                uia.AddEventHandlerGroup(rootElement, group);
                uia.AddFocusChangedEventHandler(cache, focusHandler);

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
