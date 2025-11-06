using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    [DebuggerDisplay("{Name} (ControlType = {LocalizedControlType})")]
    public class AutomationElementViewModel
    {
        public string Name { get; private set; }
        public string LocalizedControlType { get; private set; }
        public Rectangle BoundingRect { get; private set; }

        public string RuntimeId { get; private set; }

        public UIA_CONTROLTYPE_ID ControlType => _element.CachedControlType;
        public bool HasKeyboardFocus => _element.CachedHasKeyboardFocus;
        public bool IsEnabled => _element.CachedIsEnabled;
        public bool IsOffscreen => _element.CachedIsOffscreen;

        // Must be requested!
        public ObservableCollection<AutomationElementViewModel>? Children { get; private set; } = null;

        private readonly IUIAutomation _uia;
        private IUIAutomationElement _element;

        // https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-control-pattern-availability-propids
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

        public AutomationElementViewModel(IUIAutomation uia, IUIAutomationElement element)
        {
            _uia = uia;

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

            var runtimeIdObj = _element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
        }

        public bool IsElement(IUIAutomationElement element)
        {
            return _uia.CompareElements(_element, element);
        }

        public void LoadChildren()
        {
            var condition = _uia.CreateTrueCondition();
            var children = _element.FindAll(TreeScope.TreeScope_Children, condition);
            Children ??= new ObservableCollection<AutomationElementViewModel>();
            for (int i = 0; i < children.Length; i++)
            {
                var childElement = children.GetElement(i);

                // TODO: Merge with existing children.
                //if (Children?.Count < knownChildrenIndex)

                var childViewModel = new AutomationElementViewModel(_uia, childElement);
                Children?.Add(childViewModel);
            }
        }

        public void LoadDetailedProperties()
        {
            var cache = _uia.CreateCacheRequest();
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
    }
}
