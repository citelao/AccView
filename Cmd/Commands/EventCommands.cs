using Windows.Win32.UI.Accessibility;
using Shared;
using Shared.UIA.EventHandlers;

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
                Console.WriteLine("Hi!");
                var uia = UIAHelpers.CreateUIAutomationInstance();

                var cache = uia.CreateCacheRequest();
                cache.AddProperty(UIA_PROPERTY_ID.UIA_NamePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);
                cache.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);

                var handler = new FocusChangedEventHandler();
                handler.FocusChanged += (sender, e) =>
                {
                    var element = e.Element;
                    var name = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
                    var automationId = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                    var controlType = (int)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                    var localizedControlType = (string)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);
                    Console.WriteLine($"Focus Changed: Name='{name}', AutomationId='{automationId}', ControlType='{controlType}', LocalizedControlType='{localizedControlType}'");
                };

                uia.AddFocusChangedEventHandler(cache, handler);

                exitEvent.WaitOne();

                uia.RemoveFocusChangedEventHandler(handler);

                Console.WriteLine("Done!");
            });

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Exiting...");
                exitEvent.Set();
            };

            watcherThread.IsBackground = true;
            watcherThread.Start();
            Console.WriteLine("Watching for focus changed events. Press Ctrl-C to exit.");

            // Keep the application running
            exitEvent.WaitOne();

            Console.WriteLine("Waiting for watcher thread to exit...");

            watcherThread.Join();
        }
    }
}
