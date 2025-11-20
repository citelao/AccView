using Shared;
using Windows.Win32.UI.Accessibility;
using WinRT;

namespace Cmd.Commands
{
    public class TreeCommands
    {
        public static void ListTopLevelTree()
        {
            // List all top-level windows with their titles
            var automation = UIAHelpers.CreateUIAutomationInstance();
            var root = automation.GetRootElement();
            var condition = automation.CreateTrueCondition();
            var children = root.FindAll(TreeScope.TreeScope_Children, condition);
            for (int i = 0; i < children.get_Length(); i++)
            {
                var element = children.GetElement(i);
                var name = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>();
                var automationId = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);

                // Check to see if this element has children
                var childCondition = automation.CreateTrueCondition();
                var childElements = element.FindFirst(TreeScope.TreeScope_Children, childCondition);
                var hasChildren = childElements != null;

                Console.WriteLine($"{i}: Name='{name}', AutomationId='{automationId}'");
                if (hasChildren)
                {
                    Console.WriteLine("   + [...]");
                }
            }

        }
    }
}
