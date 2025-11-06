using Windows.Win32;
using Windows.Win32.UI.Accessibility;

namespace Shared
{
    public class UIAHelpers
    {
        public static IUIAutomation CreateUIAutomationInstance()
        {
            // https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-creatingcuiautomation
            IUIAutomation? automation = null;
            PInvokeAcc.CoCreateInstance<IUIAutomation>(
                typeof(CUIAutomation).GUID,
                null,
                Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                out automation);
            if (automation == null)
            {
                throw new InvalidOperationException("Failed to create IUIAutomation instance.");
            }

            return automation;
        }
    }
}
