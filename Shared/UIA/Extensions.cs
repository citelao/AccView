using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Win32.System.Com;
using Windows.Win32;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Shared.UIA
{
    public static class Extensions
    {
        /// <summary>
        /// Safely get a cached property value and cast it to the desired type. Throws if invalid type or not cached.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public static T? GetCachedPropertyValue<T>(this IUIAutomationElement element, UIA_PROPERTY_ID propertyId)
        {
            try
            {
                var value = element.GetCachedPropertyValue(propertyId).As<T>();
                if (value is not null)
                {
                    return value;
                }
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"{propertyId} is not cached", ex);
            }
            throw new InvalidCastException($"Failed to cast property {propertyId} value to type {typeof(T)}.");
        }

        public unsafe static void PollForPotentialSupportedPatternsSafe(this IUIAutomation uia, IUIAutomationElement element, out UIA_PATTERN_ID[] patternIds, out string[] patternNames)
        {
            uia.PollForPotentialSupportedPatterns(element, out SAFEARRAY* patternIdsSafeArray, out SAFEARRAY* patternNamesSafeArray);
            try
            {
                var patternIdInts = SafeArrayHelpers.ToArray<int>(patternIdsSafeArray);
                patternIds = patternIdInts.Select(id => (UIA_PATTERN_ID)id).ToArray();

                var patternNamesBstr = SafeArrayHelpers.ToArray<BSTR>(patternNamesSafeArray);
                patternNames = patternNamesBstr.Select(bstr => Marshal.PtrToStringBSTR(bstr)).ToArray();
            }
            finally
            {
                PInvokeAcc.SafeArrayDestroy(patternIdsSafeArray);
                PInvokeAcc.SafeArrayDestroy(patternNamesSafeArray);
            }
        }
    }
}
