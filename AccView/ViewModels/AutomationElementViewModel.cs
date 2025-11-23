using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
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
        public partial ObservableCollection<AutomationElementViewModel> Children { get; private set; } = [];

        [ObservableProperty]
        public partial AutomationElementViewModel? Parent { get; private set; } = null;

        private readonly IUIAutomation _uia;
        private readonly AutomationTreeViewModel _factory;
        private readonly DispatcherQueue _dispatcherQueue;

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
            UIA_PROPERTY_ID.UIA_NamePropertyId,
            UIA_PROPERTY_ID.UIA_AutomationIdPropertyId,
            UIA_PROPERTY_ID.UIA_ControlTypePropertyId,
            UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId,
            UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId,
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

        public AutomationElementViewModel(IUIAutomation uia, IUIAutomationElement element, AutomationElementViewModel? parent, AutomationTreeViewModel factory, DispatcherQueue? dispatcherQueue)
        {
            _uia = uia;
            _factory = factory;
            _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

            Parent = parent;

            var cache = BuildDefaultCacheRequest(uia);
            _element = element.BuildUpdatedCache(cache);

            Name = (string)_element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
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

        public static IUIAutomationCacheRequest BuildDefaultCacheRequest(IUIAutomation uia)
        {
            var cache = uia.CreateCacheRequest();
            foreach (var propertyId in DefaultCachedProperties)
            {
                cache.AddProperty(propertyId);
            }
            return cache;
        }

        public bool IsElement(IUIAutomationElement element)
        {
            return _uia.CompareElements(_element, element);
        }

        public bool IsElement(AutomationElementViewModel element)
        {
            return IsElement(element._element);
        }

        private static void MergeChildren(ObservableCollection<AutomationElementViewModel> existingChildren, IList<AutomationElementViewModel> newChildren)
        {
            CollectionHelpers.UpdateObservableCollection(existingChildren, newChildren, (a, b) => a.IsElement(b._element));
        }

        public async Task LoadChildrenAsync()
        {
            //if (!_dispatcherQueue.HasThreadAccess)
            //{
            //    throw new Exception("LoadChildren must be called on the UI thread.");
            //}

            var children = _element.FindAll(TreeScope.TreeScope_Children, _factory.TreeCondition);
            var childVMs = new List<AutomationElementViewModel>();
            for (int i = 0; i < children.Length; i++)
            {
                var childElement = children.GetElement(i);
                var childViewModel = await _factory.GetOrCreateNormalizedWithKnownParent(childElement, parent: this);
                childVMs.Add(childViewModel);
            }

            // TODO: I don't like jumping between the UI thread.
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                MergeChildren(Children, childVMs);
            });
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

        public override string ToString()
        {
            return $"{Name} ({LocalizedControlType})";
        }

        public bool IsPatternAvailable(KnownPattern pattern)
        {
            return pattern.CachedIsAvailable(_element);
        }

        public bool IsDescendant(AutomationElementViewModel possibleAncestor)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.IsElement(possibleAncestor))
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
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
