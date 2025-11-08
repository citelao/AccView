using Windows.Win32.UI.Accessibility;
using System.Collections.Generic;
using Shared;

namespace AccView.ViewModels
{
    using RuntimeIdT = int[];

    class AutomationElementViewModelFactory
    {
        private readonly Dictionary<RuntimeIdT, AutomationElementViewModel> _cache = new();

        public AutomationElementViewModel GetOrCreate(IUIAutomation uia, IUIAutomationElement element, AutomationElementViewModel? parent)
        {
            var runtimeId = (RuntimeIdT)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
            if (_cache.TryGetValue(runtimeId, out var existingViewModel))
            {
                return existingViewModel;
            }

            var newViewModel = new AutomationElementViewModel(uia, element, parent);
            _cache[runtimeId] = newViewModel;
            return newViewModel;
        }
    }
}
