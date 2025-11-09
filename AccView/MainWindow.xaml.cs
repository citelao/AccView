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
        public ObservableCollection<AutomationElementViewModel> AccessibilityTree => avmFactory.Tree;

        private IUIAutomation6 _uia;
        private IUIAutomationCondition _condition;
        private IUIAutomationEventHandlerGroup? _eventHandlerGroup = null;
        AutomationTreeViewModel avmFactory;

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
            avmFactory = new AutomationTreeViewModel(_uia, _condition);

            // https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomationeventhandlergroup
            _uia.CreateEventHandlerGroup(out _eventHandlerGroup);

            // TODO: cache
            _uia.AddFocusChangedEventHandler(_uia.CreateCacheRequest(), _focusChangedHandler);

            overlayWindow = window;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            avmFactory.LoadRoot();

            // _focusChangedHandler.FocusChanged += FocusChanged;
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
            // We need to load UIA stuff on a different thread. Otherwise looking at our current window will hang.
            throw new NotImplementedException("Ooof");

            var focusedElement = e.Element!;

            // Find the corresponding view model.
            await DispatcherQueue.EnqueueAsync(() =>
            {
                // TODO: load on non-UI thread?
                var tempVm = avmFactory.GetOrCreateNormalizedWithParents(focusedElement);
                OutputTextBlock.Text += $"\nFocus changed to element: {tempVm.Name} ({tempVm.LocalizedControlType}, {tempVm.RuntimeIdString})";
            });
        }

        private async void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            // Get current mouse cursor coordinates.
            var point = CursorHelpers.GetCursorPosition();

            var rawElement = _uia.ElementFromPoint(point);

            // Expand tree to the selected item.
            //ElementsTreeView.SelectedItem = currentViewModel;
            //var container = ElementsTreeView.ContainerFromItem(currentViewModel);
            //var rootNode = ElementsTreeView.NodeFromContainer(container);
            //ElementsTreeView.Expand(node);

            var element = avmFactory.GetOrCreateNormalizedWithParents(rawElement);

            await Task.Delay(100);

            var chain = new Stack<AutomationElementViewModel>();
            var current = element;
            while (current != null)
            {
                chain.Push(current);
                current = current.Parent;
            }

            // Expand tree to the focused item.
            var rootVm = chain.Pop();
            var rootContainer = ElementsTreeView.ContainerFromItem(rootVm);
            var rootNode = ElementsTreeView.NodeFromContainer(rootContainer);

            var currentNode = rootNode;
            bool shouldContinue = true;
            while (chain.Count > 0 && shouldContinue)
            {
                var vm = chain.Pop();

                if (currentNode.Children == null)
                {
                    // Testing.
                    shouldContinue = false;
                    continue;
                }

                var applicableChildNode = currentNode.Children.FirstOrDefault((c) => (AutomationElementViewModel)c.Content == vm);

                if (applicableChildNode == null)
                {
                    // Testing.
                    shouldContinue = false;
                    continue;
                }

                currentNode.IsExpanded = true;
                currentNode = applicableChildNode;

                // TODO: testing.
                await Task.Delay(100);
            }

            ElementsTreeView.SelectedItem = element;
            var finalContainer = ElementsTreeView.ContainerFromItem(element) as UIElement;
            finalContainer?.StartBringIntoView();
            ElementDetail.Navigate(typeof(ElementDetailPage), element);
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
