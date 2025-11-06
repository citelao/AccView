using Windows.Win32.UI.Accessibility;
using Shared;

//unsafe
//{
//    PInvoke.EnumWindows((hwnd, lParam) =>
//    {
//        const int maxLength = 256;
//        Span<char> buffer = stackalloc char[maxLength];

//        fixed (char* pBuffer = buffer)
//        {
//            var pwstr = new PWSTR(pBuffer);
//            int length = PInvoke.GetWindowText(hwnd, pwstr, maxLength);
//            string title = length > 0 ? new string(pBuffer, 0, length) : string.Empty;
//            Console.WriteLine($"HWND: 0x{hwnd:X}, Title: '{title}'");
//        }

//        return true; // continue enumeration
//    }, IntPtr.Zero);
//}

// List all top-level windows with their titles
var automation = UIAHelpers.CreateUIAutomationInstance();
var root = automation.GetRootElement();
var condition = automation.CreateTrueCondition();
var children = root.FindAll(TreeScope.TreeScope_Children, condition);
for (int i = 0; i < children.Length; i++)
{
    var element = children.GetElement(i);
    var name = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
    var automationId = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);

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