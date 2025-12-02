using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Accessibility;
using Shared.UIA;

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

        public UIA_CONTROLTYPE_ID ControlType => _element.get_CachedControlType();
        public bool HasKeyboardFocus => _element.get_CachedHasKeyboardFocus();
        public bool IsEnabled => _element.get_CachedIsEnabled();
        public bool IsOffscreen => _element.get_CachedIsOffscreen();

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

            Name = _element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
            LocalizedControlType = _element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;

            var rect = _element.get_CachedBoundingRectangle();
            BoundingRect = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

            var runtimeIdObj = GetCachedRuntimeId(element);
            RuntimeId = runtimeIdObj;

            // TODO: register for change events
        }

        public static RuntimeIdT GetCachedRuntimeId(IUIAutomationElement element)
        {
            var rawValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
            return rawValue.AsArray<int>();
        }

        public static RuntimeIdT GetCurrentRuntimeId(IUIAutomationElement element)
        {
            var rawValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
            return rawValue.AsArray<int>();
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
            for (int i = 0; i < children.get_Length(); i++)
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
            // TODO: cache
            _uia.PollForPotentialSupportedPatternsSafe(_element, out var patternIds, out var patternNames);
            return Array.Exists(patternIds, id => id == pattern.PatternId);
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
