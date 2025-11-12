using AccView.ViewModels;
using CommunityToolkit.Common.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
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
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using WinUIEx;


namespace AccView
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public ObservableCollection<AutomationElementViewModel> AccessibilityTree => avmFactory.Tree;

        // TODO:
        // [GeneratedDependencyProperty]
        private bool followKeyboardFocus { get; set; } = true;

        private IUIAutomation6 _uia;
        private IUIAutomationCondition _condition;
        private IUIAutomationEventHandlerGroup? _eventHandlerGroup = null;
        private AutomationTreeViewModel avmFactory;
        private AutomationElementViewModel windowUiaElement;

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

            // TODO: ignore all events?
            var hwnd = this.GetWindowHandle();
            var rawWindowUiaElement = _uia.ElementFromHandle(new HWND(hwnd));
            windowUiaElement = avmFactory.GetOrCreateNormalized(rawWindowUiaElement);

            // https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomationeventhandlergroup
            _uia.CreateEventHandlerGroup(out _eventHandlerGroup);

            // TODO: cache
            _uia.AddFocusChangedEventHandler(_uia.CreateCacheRequest(), _focusChangedHandler);

            overlayWindow = window;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            avmFactory.LoadRoot();

            _focusChangedHandler.FocusChanged += FocusChanged;
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
            if (!followKeyboardFocus)
            {
                // Ignore focus changes.
                return;
            }

            var focusedElement = e.Element!;

            var ogName = focusedElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId) as string;
            var ogLct = focusedElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId) as string;
            var ogRid = AutomationElementViewModel.GetCurrentRuntimeId(focusedElement);

            if (ogRid == null)
            {
                throw new InvalidOperationException($"RID cannot be null ({ogName} {ogRid})");
            }

            // Find the corresponding view model.
            var tempVm = avmFactory.GetOrCreateNormalized(focusedElement);

            // Is this the current window?
            var isInCurrentWindow = false;
            var parent = tempVm;
            while (parent != null)
            {
                if (windowUiaElement.IsElement(parent))
                {
                    isInCurrentWindow = true;
                    break;
                }
                parent = parent.Parent;
            }

            if (isInCurrentWindow)
            {
                // Ignore focus changes in the current window.
                return;
            }

            // TODO: better UI thread handling.
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                // TODO: ignore from current window windowUiaElement

                OutputTextBlock.Text += $"\nFocus changed to element: {tempVm.Name} ({tempVm.LocalizedControlType}, {tempVm.RuntimeIdString})";
                OutputTextBlock.Text += $"\n\t{ogName} ({ogLct}, {ogRid})";

                await JumpToNodeAsync(tempVm);
            });
        }

        private async void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            // Get current mouse cursor coordinates.
            var point = CursorHelpers.GetCursorPosition();

            var rawElement = _uia.ElementFromPoint(point);

            var element = avmFactory.GetOrCreateNormalized(rawElement);

            await Task.Delay(100);

            await JumpToNodeAsync(element);
        }

        private async Task JumpToNodeAsync(AutomationElementViewModel element)
        {
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
                await Task.Delay(1);
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

        private void FollowKeyboardFocus_Click(object sender, RoutedEventArgs e)
        {
            var newValue = FollowKeyboardFocusButton.IsChecked ?? false;
            followKeyboardFocus = newValue;
        }
    }
}
