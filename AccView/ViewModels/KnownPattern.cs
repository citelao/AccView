using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    public class KnownPattern
    {
        public required UIA_PATTERN_ID PatternId { get; init; }
        public required UIA_PROPERTY_ID IsAvailableId { get; init; }
        public required Type Type { get; init; }

        public static Dictionary<UIA_PATTERN_ID, KnownPattern> All { get; } = new()
        {
            {
                UIA_PATTERN_ID.UIA_ExpandCollapsePatternId,
                new KnownPattern<IUIAutomationExpandCollapsePattern>
                {
                    PatternId = UIA_PATTERN_ID.UIA_ExpandCollapsePatternId,
                    IsAvailableId = UIA_PROPERTY_ID.UIA_IsExpandCollapsePatternAvailablePropertyId,
                    Type = typeof(IUIAutomationExpandCollapsePattern),
                }
            },
            {
                UIA_PATTERN_ID.UIA_InvokePatternId,
                new KnownPattern<IUIAutomationInvokePattern>
                {
                    PatternId = UIA_PATTERN_ID.UIA_InvokePatternId,
                    IsAvailableId = UIA_PROPERTY_ID.UIA_IsInvokePatternAvailablePropertyId,
                    Type = typeof(IUIAutomationInvokePattern),
                }
            },
            {
                UIA_PATTERN_ID.UIA_TextPatternId,
                new KnownPattern<IUIAutomationTextPattern>
                {
                    PatternId = UIA_PATTERN_ID.UIA_TextPatternId,
                    IsAvailableId = UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId,
                    Type = typeof(IUIAutomationTextPattern),
                }
            },
            {
                UIA_PATTERN_ID.UIA_ValuePatternId,
                new KnownPattern<IUIAutomationValuePattern>
                {
                    PatternId = UIA_PATTERN_ID.UIA_ValuePatternId,
                    IsAvailableId = UIA_PROPERTY_ID.UIA_IsValuePatternAvailablePropertyId,
                    Type = typeof(IUIAutomationValuePattern),
                }
            }
        };

        /// <summary>
        /// Check if the pattern is available on the given element (if cached).
        /// </summary>
        public bool CachedIsAvailable(IUIAutomationElement element)
        {
            var availableObj = element.GetCachedPropertyValue(IsAvailableId);
            if (availableObj.VarType == VarEnum.VT_BOOL)
            {
                return availableObj.As<bool>();
            }
            return false;
        }

        /// <summary>
        /// Get the pattern object from the element, if it is cached.
        /// </summary>
        public object CachedGetRaw(IUIAutomationElement element)
        {
            return element.GetCachedPattern(PatternId);
        }
    }

    public class KnownPattern<TPattern> : KnownPattern
    {
        public TPattern CachedGet(IUIAutomationElement element)
        {
            var patternObj = CachedGetRaw(element);
            return (TPattern)patternObj;
        }
    }
}
