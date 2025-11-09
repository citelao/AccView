using AccView.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32.UI.Accessibility;
using WinUIEx;

using CommunityToolkit.Common.Extensions;
using CommunityToolkit.WinUI;


namespace AccView
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public ObservableCollection<AutomationElementViewModel> AccessibilityTree = new();

        private IUIAutomation6 _uia;
        private IUIAutomationCondition _condition;
        private IUIAutomationEventHandlerGroup? _eventHandlerGroup = null;
        AutomationElementViewModelFactory avmFactory;

        private class FocusChangedEventHandler : IUIAutomationFocusChangedEventHandler
        {
            public class FocusChangedEventArgs : EventArgs
            {
                public IUIAutomationElement? Element { get; set; }
            }

            public event EventHandler<FocusChangedEventArgs>? FocusChanged;

            public void HandleFocusChangedEvent(IUIAutomationElement sender)
            {
                FocusChanged?.Invoke(this, new FocusChangedEventArgs { Element = sender });
            }
        }
        private FocusChangedEventHandler _focusChangedHandler = new();

        private OverlayWindow? overlayWindow = null;

        public MainWindow(OverlayWindow? window)
        {
            InitializeComponent();
            _uia = UIAHelpers.CreateUIAutomationInstance();
            _condition = _uia.ControlViewCondition;
            avmFactory = new AutomationElementViewModelFactory(_uia, _condition);

            // https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomationeventhandlergroup
            _uia.CreateEventHandlerGroup(out _eventHandlerGroup);

            // TODO: cache
            _focusChangedHandler.FocusChanged += FocusChanged;
            _uia.AddFocusChangedEventHandler(_uia.CreateCacheRequest(), _focusChangedHandler);

            overlayWindow = window;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var root = _uia.GetRootElement();
            var children = root.FindAll(TreeScope.TreeScope_Children, _condition);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var vm = avmFactory.GetOrCreateNormalized(element, parent: null);
                AccessibilityTree.Add(vm);
            }
        }

        private void ElementsTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            var expandingItem = args.Item as AutomationElementViewModel;
            if (expandingItem == null)
            {
                throw new InvalidOperationException("Expanding item is not an AutomationElementViewModel.");
            }

            // Load all children, then load their children as well (so that the expander shows up)
            if (expandingItem.Children == null)
            {
                expandingItem.LoadChildren();

            }

            foreach (var child in expandingItem.Children ?? [])
            {
                child.LoadChildren();
            }
        }

        private void ElementsTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.AddedItems.FirstOrDefault() as AutomationElementViewModel;
            ElementDetail.Navigate(typeof(ElementDetailPage), selectedItem, new SuppressNavigationTransitionInfo());
        }

        private async void FocusChanged(object sender, FocusChangedEventHandler.FocusChangedEventArgs e)
        {
            var focusedElement = e.Element!;

            // Find the corresponding view model.
            // TODO: parent.
            AutomationElementViewModel? parent = null;
            var tempVm = avmFactory.GetOrCreateNormalized(focusedElement, parent: parent);
            await DispatcherQueue.EnqueueAsync(() =>
            {
                OutputTextBlock.Text += $"\nFocus changed to element: {tempVm.Name} ({tempVm.LocalizedControlType}, {tempVm.RuntimeIdString})";
            });
        }

        private async void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            // Get current mouse cursor coordinates.
            var point = CursorHelpers.GetCursorPosition();

            var rawElement = _uia.ElementFromPoint(point);

            // Find element's ancestors.
            var treeWalker = _uia.CreateTreeWalker(_condition);
            var element = treeWalker.NormalizeElement(rawElement);

            var path = new Stack<IUIAutomationElement>();
            var currentElement = element;
            while (currentElement != null)
            {
                path.Push(currentElement);
                currentElement = treeWalker.GetParentElement(currentElement);
            }

            var rootUiaElement = path.Pop();
            var isRoot = _uia.CompareElements(_uia.GetRootElement(), rootUiaElement);
            if (!isRoot)
            {
                throw new InvalidOperationException("Could not find root element.");
            }

            // Now walk down the tree to find the corresponding view model.
            var rootWindowUiaElement = path.Pop();
            var rootViewModel = AccessibilityTree.FirstOrDefault(vm => vm.IsElement(rootWindowUiaElement));
            if (rootViewModel == null)
            {
                throw new InvalidOperationException("Could not find root element in the accessibility tree.");
            }

            var currentViewModel = rootViewModel;
            var currentContainer = (TreeViewItem)ElementsTreeView.ContainerFromItem(currentViewModel);
            var currentNode = ElementsTreeView.NodeFromContainer(currentContainer);
            while (path.Count > 0)
            {
                var nextUiaElement = path.Pop();

                // TODO: don't load all children.
                currentViewModel.LoadChildren();

                // Expand the current node.
                currentNode.IsExpanded = true;

                var nextViewModel = currentViewModel.Children?.FirstOrDefault(vm => vm.IsElement(nextUiaElement));
                if (nextViewModel == null)
                {
                    throw new InvalidOperationException("Could not find element in the accessibility tree.");
                }

                //var nextNode = currentNode.Children.FirstOrDefault(n => n.Content == nextViewModel);
                //if (nextNode == null)
                //{
                //    throw new InvalidOperationException("Could not find tree node for the element.");
                //}

                currentViewModel = nextViewModel;
                //currentNode = nextNode;
            }

            await Task.Delay(100); // Allow UI to update.

            // Expand tree to the selected item.
            //ElementsTreeView.SelectedItem = currentViewModel;
            //var container = ElementsTreeView.ContainerFromItem(currentViewModel);
            //var node = ElementsTreeView.NodeFromContainer(container);
            //ElementsTreeView.Expand(node);

            ElementDetail.Navigate(typeof(ElementDetailPage), currentViewModel);
        }

        // TODO: move highlight on focus, too.
        private void TreeViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            var vm = ElementsTreeView.ItemFromContainer(item) as AutomationElementViewModel;
            var rect = RectHelper.FromCoordinatesAndDimensions(
                (float)vm!.BoundingRect.X,
                (float)vm!.BoundingRect.Y,
                (float)vm!.BoundingRect.Width,
                (float)vm!.BoundingRect.Height);
            overlayWindow?.MoveFocusRing(rect);
        }

        private void WindowEx_Closed(object sender, WindowEventArgs args)
        {
            overlayWindow?.Close();
            overlayWindow = null;
        }
    }
}
