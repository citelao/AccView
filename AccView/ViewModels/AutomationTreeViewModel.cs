using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.System;
using Windows.Win32.UI.Accessibility;

#nullable enable

namespace AccView.ViewModels
{
    using RuntimeIdT = int[];

    public class AutomationTreeViewModel
    {
        /// <summary>
        /// Known accessbility tree; root elements are typically individual windows.
        /// </summary>
        public readonly ObservableCollection<AutomationElementViewModel> Tree = new();

        /// <summary>
        /// Cache of all viewmodels, indexed by runtime ID.
        /// </summary>
        private readonly Dictionary<RuntimeIdT, AutomationElementViewModel> cache = new();

        private IUIAutomation6 uia;
        public IUIAutomationCondition TreeCondition;
        private IUIAutomationTreeWalker treeWalker;

        private IUIAutomationCacheRequest runtimeIdOnlyRequest;

        private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

        public AutomationTreeViewModel(IUIAutomation6 uia, IUIAutomationCondition walkerCondition)
        {
            this.uia = uia;
            this.TreeCondition = walkerCondition;
            treeWalker = uia.CreateTreeWalker(walkerCondition);

            runtimeIdOnlyRequest = uia.CreateCacheRequest();
            runtimeIdOnlyRequest.AddProperty(UIA_PROPERTY_ID.UIA_RuntimeIdPropertyId);

            dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }

        /// <summary>
        /// Load all the root elements, like windows. Should probably be your first call.
        /// </summary>
        public void LoadRoot()
        {
            var root = uia.GetRootElement();
            var children = root.FindAllBuildCache(TreeScope.TreeScope_Children, TreeCondition, runtimeIdOnlyRequest);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var runtimeId = AutomationElementViewModel.GetCachedRuntimeId(element);

                AutomationElementViewModel vm;
                if (cache.TryGetValue(runtimeId, out var existingViewModel))
                {
                    // If we already have a ViewModel for this element, use it.
                    vm = existingViewModel;

                    // Move it to the current position in the tree if needed.
                    var currentIndex = Tree.IndexOf(vm);
                    if (currentIndex != i)
                    {
                        Tree.Move(currentIndex, i);
                    }
                }
                else
                {
                    // Otherwise, create a new ViewModel.
                    vm = new AutomationElementViewModel(uia, element, parent: null, factory: this);
                    cache[runtimeId] = vm;

                    // Insert it at the current position.
                    Tree.Insert(i, vm);
                }

                // Load the immediate children for all root elements: this lets us know which ones are expandable.
                vm.LoadChildren();
            }

            // Are there more elements?
            if (Tree.Count > children.Length)
            {
                // Remove the excess elements.
                for (int i = Tree.Count - 1; i >= children.Length; i--)
                {
                    var vmToRemove = Tree[i];
                    Tree.RemoveAt(i);
                    var runtimeIdToRemove = vmToRemove.RuntimeId;
                    cache.Remove(runtimeIdToRemove);
                }
            }
        }

        /// <summary>
        /// Get or create a ViewModel for the given element, normalized to the tree condition, with a known parent. If you don't know the parent, use GetOrCreateNormalized.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public AutomationElementViewModel GetOrCreateNormalizedWithKnownParent(IUIAutomationElement element, AutomationElementViewModel? parent)
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
            // Create all ViewModels on the UI thread.
            //if (!dispatcherQueue.HasThreadAccess)
            //{
            //    throw new InvalidOperationException("GetOrCreateNormalizedWithKnownParent must be called on the UI thread.");
            //}

            var newViewModel = new AutomationElementViewModel(uia, normalizedElement, parent: parent, factory: this);
            cache[runtimeId] = newViewModel;

            return newViewModel;
        }

        public AutomationElementViewModel GetOrCreateNormalized(IUIAutomationElement element)
        {
            // Do we already have it?
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

            // If not found, create it along with its parents.
            // Find all applicable ancestors.
            var ancestors = new Stack<IUIAutomationElement>();
            var current = normalizedElement;
            while (current != null)
            {
                ancestors.Push(current);
                current = treeWalker.GetParentElementBuildCache(current, runtimeIdOnlyRequest);
            }

            var rootUiaElement = ancestors.Pop();
            var isRoot = uia.CompareElements(uia.GetRootElement(), rootUiaElement);
            if (!isRoot)
            {
                throw new InvalidOperationException("Could not find root element.");
            }

            // Now walk down the tree to find the corresponding view model.
            var rootWindowUiaElement = ancestors.Pop();
            var rootViewModel = Tree.FirstOrDefault(vm => vm.IsElement(rootWindowUiaElement));
            if (rootViewModel == null)
            {
                // If the root window view model doesn't exist yet, create it (it may be a new window).
                var newViewModel = new AutomationElementViewModel(uia, rootWindowUiaElement, parent: null, factory: this);
                cache[runtimeId] = newViewModel;
                rootViewModel = newViewModel;
            }

            var currentViewModel = rootViewModel;
            while (ancestors.Count > 0)
            {
                var nextUiaElement = ancestors.Pop();

                // This will load all the children, including the next descendant.
                currentViewModel.LoadChildren();

                var childViewModel = currentViewModel.Children!.FirstOrDefault(vm => vm.IsElement(nextUiaElement));
                if (childViewModel == null)
                {
                    throw new InvalidOperationException("Could not find child element in the accessibility tree.");
                }

                currentViewModel = childViewModel;
            }

            return currentViewModel;
        }

        //public void Remove(AutomationElementViewModel element)
        //{
        //    cache.Remove(element.RuntimeId);

        //    // If this is a root element, that's unexpected.
        //    var index = Tree.IndexOf(element);
        //    if (index >= 0)
        //    {
        //        throw new NotImplementedException("Removing root elements not implemented yet.");
        //    }
        //}
    }
}
