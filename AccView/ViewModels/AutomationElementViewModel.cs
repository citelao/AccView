using System.Collections.ObjectModel;
using System.Drawing;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    public class AutomationElementViewModel
    {
        public string Name { get; private set; }
        public string LocalizedControlType { get; private set; }
        public Rectangle BoundingRect { get; private set; }

        // Must be requested!
        public ObservableCollection<AutomationElementViewModel>? Children { get; private set; } = null;

        private readonly IUIAutomation _uia;
        private readonly IUIAutomationElement _element;

        public AutomationElementViewModel(IUIAutomation uia, IUIAutomationElement element)
        {
            _uia = uia;
            _element = element;

            // TODO: cache
            Name = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
            LocalizedControlType = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);

            var rect = element.CurrentBoundingRectangle;
            BoundingRect = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        public void LoadChildren()
        {
            var condition = _uia.CreateTrueCondition();
            var children = _element.FindAll(TreeScope.TreeScope_Children, condition);
            Children = new ObservableCollection<AutomationElementViewModel>();
            for (int i = 0; i < children.Length; i++)
            {
                var childElement = children.GetElement(i);
                var childViewModel = new AutomationElementViewModel(_uia, childElement);
                Children?.Add(childViewModel);
            }
        }
    }
}
