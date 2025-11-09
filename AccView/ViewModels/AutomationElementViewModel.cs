using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    using RuntimeIdT = int[];

    [DebuggerDisplay("{Name} (ControlType = {LocalizedControlType})")]
    public partial class AutomationElementViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; private set; }

        [ObservableProperty]
        public partial string LocalizedControlType { get; private set; }

        [ObservableProperty]
        public partial Rectangle BoundingRect { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RuntimeIdString))]
        public partial RuntimeIdT RuntimeId { get; private set; }
        public string RuntimeIdString => $"[{string.Join(",", RuntimeId)}]";

        public UIA_CONTROLTYPE_ID ControlType => _element.CachedControlType;
        public bool HasKeyboardFocus => _element.CachedHasKeyboardFocus;
        public bool IsEnabled => _element.CachedIsEnabled;
        public bool IsOffscreen => _element.CachedIsOffscreen;

        // Must be requested!
        [ObservableProperty]
        public partial ObservableCollection<AutomationElementViewModel>? Children { get; private set; } = null;

        [ObservableProperty]
        public partial AutomationElementViewModel? Parent { get; private set; } = null;

        private readonly IUIAutomation _uia;
        private readonly AutomationElementViewModelFactory _factory;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ControlType))]
        [NotifyPropertyChangedFor(nameof(HasKeyboardFocus))]
        [NotifyPropertyChangedFor(nameof(IsEnabled))]
        [NotifyPropertyChangedFor(nameof(IsOffscreen))]
        [NotifyPropertyChangedFor(nameof(IsInvokePatternAvailable))]
        private partial IUIAutomationElement _element { get; set; }

        /// <summary>
        /// Properties for checking pattern availability
        /// https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-control-pattern-availability-propids
        /// </summary>
        public static readonly List<UIA_PROPERTY_ID> AvaiblePatternProperties = [
            UIA_PROPERTY_ID.UIA_IsDockPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsExpandCollapsePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsGridItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsGridPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsInvokePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsMultipleViewPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsRangeValuePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsScrollPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsScrollItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSelectionItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSelectionPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTablePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTableItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTogglePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTransformPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsValuePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsWindowPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsLegacyIAccessiblePatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsItemContainerPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsVirtualizedItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSynchronizedInputPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsObjectModelPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsAnnotationPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTextPattern2AvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsStylesPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSpreadsheetPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSpreadsheetItemPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTransformPattern2AvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTextChildPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsDragPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsDropTargetPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsTextEditPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsCustomNavigationPatternAvailablePropertyId,
            UIA_PROPERTY_ID.UIA_IsSelectionPattern2AvailablePropertyId,
        ];

        public static readonly List<UIA_PROPERTY_ID> DefaultCachedProperties = [
            // name
            UIA_PROPERTY_ID.UIA_NamePropertyId,
            // control type
            UIA_PROPERTY_ID.UIA_ControlTypePropertyId,
            // localized control type
            UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId,
            // bounding rectangle
            UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId,
            // runtime id
            UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId,
        ];

        public static readonly List<UIA_PROPERTY_ID> DefaultDetailedCachedProperties = [
            // has keyboard focus
            UIA_PROPERTY_ID.UIA_HasKeyboardFocusPropertyId,
            // is enabled
            UIA_PROPERTY_ID.UIA_IsEnabledPropertyId,
            // is offscreen
            UIA_PROPERTY_ID.UIA_IsOffscreenPropertyId,
        ];

        public AutomationElementViewModel(IUIAutomation uia, IUIAutomationElement element, AutomationElementViewModel? parent, AutomationElementViewModelFactory factory)
        {
            _uia = uia;
            _factory = factory;

            Parent = parent;

            var cache = _uia.CreateCacheRequest();
            foreach (var propertyId in DefaultCachedProperties)
            {
                cache.AddProperty(propertyId);
            }
            _element = element.BuildUpdatedCache(cache);

            Name = (string)_element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
            LocalizedControlType = (string)_element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);

            var rect = _element.CachedBoundingRectangle;
            BoundingRect = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

            var runtimeIdObj = GetCachedRuntimeId(element);
            RuntimeId = runtimeIdObj;

            // TODO: register for change events
        }

        public static RuntimeIdT GetCachedRuntimeId(IUIAutomationElement element)
        {
            return (RuntimeIdT)element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
        }

        public static RuntimeIdT GetCurrentRuntimeId(IUIAutomationElement element)
        {
            return (RuntimeIdT)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
        }

        public bool IsElement(IUIAutomationElement element)
        {
            return _uia.CompareElements(_element, element);
        }

        public void LoadChildren()
        {
            // TODO: condition...
            var condition = _uia.CreateTrueCondition();
            var children = _element.FindAll(TreeScope.TreeScope_Children, condition);
            Children ??= new ObservableCollection<AutomationElementViewModel>();
            for (int i = 0; i < children.Length; i++)
            {
                var childElement = children.GetElement(i);

                // TODO: Merge with existing children.
                //if (Children?.Count < knownChildrenIndex)

                var childViewModel = _factory.GetOrCreateNormalized(childElement, this);
                Children?.Add(childViewModel);
            }
        }

        public void LoadDetailedProperties()
        {
            var cache = _uia.CreateCacheRequest();
            foreach (var propertyId in AvaiblePatternProperties)
            {
                cache.AddProperty(propertyId);
            }
            foreach (var knownPattern in KnownPattern.All.Values)
            {
                cache.AddProperty(knownPattern.IsAvailableId);
                cache.AddPattern(knownPattern.PatternId);
            }
            foreach (var propertyId in DefaultCachedProperties)
            {
                cache.AddProperty(propertyId);
            }
            foreach (var propertyId in DefaultDetailedCachedProperties)
            {
                cache.AddProperty(propertyId);
            }

            _element = _element.BuildUpdatedCache(cache);
        }

        public bool IsPatternAvailable(KnownPattern pattern)
        {
            return pattern.CachedIsAvailable(_element);
        }

        public bool IsInvokePatternAvailable => IsPatternAvailable(KnownPattern.All[UIA_PATTERN_ID.UIA_InvokePatternId]);
            
        // TODO: play with this.
        public void Invoke()
        {
            var invoke = KnownPattern.All[UIA_PATTERN_ID.UIA_InvokePatternId];
            if (!IsPatternAvailable(invoke))
            {
                throw new InvalidOperationException("Invoke pattern is not available on this element.");
            }

            // TODO: use type introspection?
            var pattern = (IUIAutomationInvokePattern)invoke.CachedGetRaw(_element);
            pattern.Invoke();
        }
    }
}
