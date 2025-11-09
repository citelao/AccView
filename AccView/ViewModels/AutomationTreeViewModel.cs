using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Win32.UI.Accessibility;

namespace AccView.ViewModels
{
    using RuntimeIdT = int[];

    public class AutomationTreeViewModel
    {
        /// <summary>
        /// The known tree of automation elements. Each item in this collection is a separate root element (typically a window).
        /// </summary>
        public readonly ObservableCollection<AutomationElementViewModel> Tree = new();

        /// <summary>
        /// Cache of all viewmodels, indexed by runtime ID.
        /// </summary>
        private readonly Dictionary<RuntimeIdT, AutomationElementViewModel> cache = new();

        private IUIAutomation6 uia;
        private IUIAutomationCondition walkerCondition;
        private IUIAutomationTreeWalker treeWalker;

        private IUIAutomationCacheRequest runtimeIdOnlyRequest;

        public AutomationTreeViewModel(IUIAutomation6 uia, IUIAutomationCondition walkerCondition)
        {
            this.uia = uia;
            this.walkerCondition = walkerCondition;
            treeWalker = uia.CreateTreeWalker(walkerCondition);

            runtimeIdOnlyRequest = uia.CreateCacheRequest();
            runtimeIdOnlyRequest.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);

            var root = this.uia.GetRootElement();
            var children = root.FindAll(TreeScope.TreeScope_Children, this.walkerCondition);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var vm = new AutomationElementViewModel(this.uia, element, parent: null, treeViewModel: this);
                vm.LoadChildren();

                Tree.Add(vm);
                cache.Add(vm.RuntimeId, vm);
            }
        }

        public void LoadChildren(AutomationElementViewModel parent)
        {
            var children = parent.RawElement.FindAllBuildCache(TreeScope.TreeScope_Children, walkerCondition, runtimeIdOnlyRequest);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var runtimeId = AutomationElementViewModel.GetCachedRuntimeId(element);
                if (!cache.ContainsKey(runtimeId))
                {
                    var vm = new AutomationElementViewModel(uia, element, parent, this);
                    cache.Add(vm.RuntimeId, vm);
                    parent.Children.Add(vm);
                }
            }
        }

        /// <summary>
        /// Get or create a ViewModel for the given element, normalized to the tree condition.
        /// </summary>
        /// <param name="uia"></param>
        /// <param name="element"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public AutomationElementViewModel GetOrCreateNormalized(IUIAutomationElement element)
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

            // If not found in the cache, we need to add this item (and all its parents) to the tree.
            var path = new Stack<IUIAutomationElement>();
            var currentElement = element;
            while (currentElement != null)
            {
                path.Push(currentElement);
                currentElement = treeWalker.GetParentElementBuildCache(currentElement, runtimeIdOnlyRequest);
            }

            var rootUiaElement = path.Pop();
            var isRoot = uia.CompareElements(uia.GetRootElement(), rootUiaElement);
            if (!isRoot)
            {
                throw new InvalidOperationException("Could not find root element.");
            }

            // Find the base element in the tree.
            var baseElement = path.Pop();
            var baseViewModel = Tree.First((vm) => vm.IsElement(baseElement));

            // Now, walk the parents, creating view models as needed.
            AutomationElementViewModel? parentViewModel = baseViewModel;
            while (path.Count > 0)
            {
                var nextElement = path.Pop();
                var nextRuntimeId = AutomationElementViewModel.GetCachedRuntimeId(nextElement);
                if (cache.TryGetValue(nextRuntimeId, out var nextViewModel))
                {
                    parentViewModel = nextViewModel;
                }
                else
                {
                    var newViewModel = new AutomationElementViewModel(uia, nextElement, parentViewModel);
                    cache[nextRuntimeId] = newViewModel;
                    parentViewModel = newViewModel;

                    newViewModel.LoadChildren();

                    // TODO
                    throw new NotImplementedException("Need to add new view model to parent's children.");
                }
            }

            throw new NotImplementedException("Need to add new view model to parent's children.");
        }
    }
}
