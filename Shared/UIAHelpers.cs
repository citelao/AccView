using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.UI.Accessibility;

namespace Shared
{
    public class UIAHelpers
    {
        private static T CreateInstance<T>(Type cls) where T : class
        {
            T? instance = null;
            PInvokeAcc.CoCreateInstance<T?>(
                cls.GUID,
                null,
                Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                out instance);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {cls}.");
            }
            return instance;
        }

        [SupportedOSPlatform("windows10.0.17763")]
        public static IUIAutomation6 CreateUIAutomationInstance()
        {
            // https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-creatingcuiautomation
            return CreateInstance<IUIAutomation6>(typeof(CUIAutomation8));
        }
         
        // UIAutomation4 is also on 14393. Anything older is out of support.
        [SupportedOSPlatform("windows10.0.14393")]
        public static IUIAutomation5 CreateUIAutomation5Instance()
        {
            return CreateInstance<IUIAutomation5>(typeof(CUIAutomation));
        }
    }
}
