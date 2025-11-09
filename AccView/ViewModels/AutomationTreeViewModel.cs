using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    using RuntimeIdT = int[];

    public class AutomationElementViewModelFactory
    {
        /// <summary>
        /// Cache of all viewmodels, indexed by runtime ID.
        /// </summary>
        private readonly Dictionary<RuntimeIdT, AutomationElementViewModel> cache = new();

        private IUIAutomation6 uia;
        private IUIAutomationCondition walkerCondition;
        private IUIAutomationTreeWalker treeWalker;

        private IUIAutomationCacheRequest runtimeIdOnlyRequest;

        public AutomationElementViewModelFactory(IUIAutomation6 uia, IUIAutomationCondition walkerCondition)
        {
            this.uia = uia;
            this.walkerCondition = walkerCondition;
            treeWalker = uia.CreateTreeWalker(walkerCondition);

            runtimeIdOnlyRequest = uia.CreateCacheRequest();
            runtimeIdOnlyRequest.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);
        }

        /// <summary>
        /// Get or create a ViewModel for the given element, normalized to the tree condition.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public AutomationElementViewModel GetOrCreateNormalized(IUIAutomationElement element, AutomationElementViewModel? parent)
        {
            var runtimeId = AutomationElementViewModel.GetCurrentRuntimeId(element);
            if (cache.TryGetValue(runtimeId, out var existingViewModel))
            {
                return existingViewModel;
            }

            // Try once more, normalized.
            var normalizedElement = treeWalker.NormalizeElementBuildCache(element, runtimeIdOnlyRequest);
            runtimeId = AutomationElementViewModel.GetCachedRuntimeId(normalizedElement);
            if (cache.TryGetValue(runtimeId, out existingViewModel))
            {
                return existingViewModel;
            }

            // If not found, create it.
            // TODO: auto-fetch parent?
            // AutomationElementViewModel? parent = null;
            var newViewModel = new AutomationElementViewModel(uia, normalizedElement, parent: parent, factory: this);
            cache[runtimeId] = newViewModel;

            return newViewModel;
        }
    }
}
