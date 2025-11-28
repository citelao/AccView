using AccView.ViewModels;
using CommunityToolkit.Common.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Shared;
using Shared.UIA;
using Shared.UIA.EventHandlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Accessibility;
using WinUIEx;


namespace AccView
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public ObservableCollection<AutomationElementViewModel> AccessibilityTree => avmFactory.Tree;

        // TODO:
        // [GeneratedDependencyProperty]
        private bool followKeyboardFocus { get; set; } = false;

        private IUIAutomation6 _uia;
        private IUIAutomationCondition _condition;
        private IUIAutomationEventHandlerGroup? _eventHandlerGroup = null;
        private AutomationTreeViewModel avmFactory;
        private AutomationElementViewModel windowUiaElement;

        private IUIAutomationElement rootWindow;

        private FocusChangedEventHandler _focusChangedHandler = new();
        private StructureChangedEventHandler _structureChangedHandler = new();
        private NotificationEventHandler _notificationEventHandler = new();

        private OverlayWindow? overlayWindow = null;

        private ObservableCollection<string> Logs = new();

        public MainWindow(OverlayWindow? window)
        {
            InitializeComponent();

            _uia = UIAHelpers.CreateUIAutomationInstance();
            _condition = _uia.get_ControlViewCondition();

            // NOTE: theoretically, any call to UIA can interact with our UI
            // thread, so it cannot be done on the UI thread (it'll hang). So we
            // use a dedicated thread for most calls. But in practice it's safe
            // to call UIA wherever, as long as you don't interact with your own
            // window.
            var uiaScheduler = new SingleThreadedTaskScheduler(CancellationToken.None);
            avmFactory = new AutomationTreeViewModel(_uia, _condition, uiaScheduler);

            // https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomationeventhandlergroup
            _uia.CreateEventHandlerGroup(out _eventHandlerGroup);

            var simpleInfoCacheRequest = AutomationElementViewModel.BuildDefaultCacheRequest(_uia);

            _uia.AddFocusChangedEventHandler(simpleInfoCacheRequest, _focusChangedHandler);

            //_eventHandlerGroup.AddActiveTextPositionChangedEventHandler();
            // _eventHandlerGroup.AddAutomationEventHandler()
            // _eventHandlerGroup.AddChangesEventHandler(TreeScope.TreeScope_Descendants, [9000],
            _eventHandlerGroup.AddNotificationEventHandler(TreeScope.TreeScope_Descendants, simpleInfoCacheRequest, _notificationEventHandler);
            // _eventHandlerGroup.AddPropertyChangedEventHandler()
            _eventHandlerGroup.AddStructureChangedEventHandler(TreeScope.TreeScope_Descendants, simpleInfoCacheRequest, _structureChangedHandler);
            // _eventHandlerGroup.AddTextEditTextChangedEventHandler(TreeScope.TreeScope_Descendants, )

            _uia.AddEventHandlerGroup(_uia.GetRootElement(), _eventHandlerGroup);
                
            overlayWindow = window;
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await avmFactory.LoadRootAsync();

            // TODO: ignore all events?
            var hwnd = this.GetWindowHandle();
            var rawWindowUiaElement = _uia.ElementFromHandle(new HWND(hwnd));
            windowUiaElement = await avmFactory.GetOrCreateNormalized(rawWindowUiaElement);
            rootWindow = _uia.GetRootElement();

            // TODO: hook up event handlers
            //foreach (var root in AccessibilityTree)
            //{
            //    _uia.AddEventHandlerGroup(root, _eventHandlerGroup);
            //}

            _focusChangedHandler.FocusChanged += FocusChanged;
            _structureChangedHandler.StructureChanged += StructureChanged;
            _notificationEventHandler.NotificationReceived += NotificationReceived;
        }

        private void StructureChanged(object? sender, StructureChangedEventHandler.StructureChangedEventArgs e)
        {
            try
            {
                var ogName = e.Sender.GetCachedPropertyValue<string>(UIA_PROPERTY_ID.UIA_NamePropertyId);
                var ogId = e.Sender.GetCachedPropertyValue<string>(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                var ogLct = e.Sender.GetCachedPropertyValue<string>(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId);
                var ogCt = e.Sender.GetCachedPropertyValue<UIA_CONTROLTYPE_ID?>(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
                var ogRid = AutomationElementViewModel.GetCachedRuntimeId(e.Sender);

                var isRoot = _uia.CompareElements(e.Sender, rootWindow);

                // DON'T await. Don't block the UIA threads on the UI.
                DispatcherQueue.EnqueueAsync(async () =>
                {
                    // TODO: try to avoid crashes
                    //if (e.ChangeType != StructureChangeType.StructureChangeType_ChildRemoved && e.ChangeType != StructureChangeType.StructureChangeType_ChildrenBulkRemoved)
                    try
                    {
                        var vm = await avmFactory.GetOrCreateNormalized(e.Sender);
                        if (vm.IsDescendant(windowUiaElement))
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: wayyyy too broad.
                        //element could have been removed.
                    }

                    if (isRoot)
                    {
                        LogOutput("Structure changed event on root element");
                    }
                    LogOutput($"Structure changed: {e.ChangeType} on element: {ogName} ({ogLct}, {ogId}, {ogCt}, {ogRid})");
                });
            }
            catch (COMException ex)
            {
                // If lots of things are getting removed, we could still be
                // processing an element that no longer exists. (??)
                int elementNotAvailableHr = unchecked((int)0x80040201);
                if (ex.HResult == elementNotAvailableHr)
                {
                    // TODO: better logging.
                    DispatcherQueue.EnqueueAsync(() =>
                    {
                        LogOutput($"Structure changed event on element that is no longer available.");
                    });
                }
                else
                {
                    throw;
                }
            }
        }

        private async void NotificationReceived(object? sender, NotificationEventHandler.NotificationEventArgs e)
        {
            var ogName = e.Sender.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
            var ogId = e.Sender.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId).As<string>() ?? string.Empty;
            var ogLct = e.Sender.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;
            var ogCt = e.Sender.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId).As<UIA_CONTROLTYPE_ID>();
            var ogRid = AutomationElementViewModel.GetCachedRuntimeId(e.Sender);
            await DispatcherQueue.EnqueueAsync(() =>
            {
                LogOutput($"Notification received: {e.NotificationKind} / {e.NotificationProcessing} on element: {ogName} ({ogLct}, {ogId}, {ogCt}, {ogRid})\n\t{e.DisplayString}");
            });
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
                _ = expandingItem.LoadChildrenAsync();

            }

            foreach (var child in expandingItem.Children ?? [])
            {
                _ = child.LoadChildrenAsync();
            }
        }

        private void ElementsTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.AddedItems.FirstOrDefault() as AutomationElementViewModel;
            ElementDetail.Navigate(typeof(ElementDetailPage), selectedItem, new SuppressNavigationTransitionInfo());
        }

        private async void FocusChanged(object? sender, FocusChangedEventHandler.FocusChangedEventArgs e)
        {
            var focusedElement = e.Element!;

            var ogName = focusedElement.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId).As<string>() ?? string.Empty;
            var ogLct = focusedElement.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_LocalizedControlTypePropertyId).As<string>() ?? string.Empty;
            var ogRid = AutomationElementViewModel.GetCachedRuntimeId(focusedElement);

            // Temp:
            await DispatcherQueue.EnqueueAsync(() =>
            {
                LogOutput($"Focus changed event received for element: {ogName} ({ogLct}, {ogRid})");
            });

            if (!followKeyboardFocus)
            {
                // Ignore focus changes.
                return;
            }

            if (ogRid == null)
            {
                throw new InvalidOperationException($"RID cannot be null ({ogName} {ogRid})");
            }

            // Find the corresponding view model.
            var tempVm = await avmFactory.GetOrCreateNormalized(focusedElement);

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

                LogOutput($"Focus changed to element: {tempVm.Name} ({tempVm.LocalizedControlType}, {tempVm.RuntimeIdString})");
                LogOutput($"\t{ogName} ({ogLct}, {ogRid})");

                await JumpToNodeAsync(tempVm);
            });
        }

        private async void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            // Get current mouse cursor coordinates.
            var point = CursorHelpers.GetCursorPosition();

            var rawElement = _uia.ElementFromPoint(point);

            var element = await avmFactory.GetOrCreateNormalized(rawElement);

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

        private void LogOutput(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                throw new InvalidOperationException("LogOutput must be called on the UI thread.");
            }

            OutputExpander.Header = message;
            Logs.Add(message);

            OutputScrollViewer.ScrollToVerticalOffset(OutputScrollViewer.ExtentHeight);
        }
    }
}
