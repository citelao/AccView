using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.UI.Accessibility;

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
                var variant = element.GetCachedPropertyValue(propertyId);
                if (variant is T value)
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
    }
}
