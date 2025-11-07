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

namespace AccView
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public ObservableCollection<AutomationElementViewModel> AccessibilityTree = new();

        private IUIAutomation _uia;
        private readonly IUIAutomationCondition _trueCondition;

        public delegate void MoveOutlineDelegate(Rect rect);
        private MoveOutlineDelegate? _moveOutlineFunc;

        public MainWindow(MoveOutlineDelegate? func)
        {
            InitializeComponent();
            _uia = UIAHelpers.CreateUIAutomationInstance();

            // TODO: support a different view.
            _trueCondition = _uia.CreateTrueCondition();

            _moveOutlineFunc = func;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var root = _uia.GetRootElement();
            var children = root.FindAll(Windows.Win32.UI.Accessibility.TreeScope.TreeScope_Children, _trueCondition);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var vm = new AutomationElementViewModel(_uia, element);
                vm.LoadChildren();

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

        private async void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            // Get current mouse cursor coordinates.
            var point = CursorHelpers.GetCursorPosition();

            var element = _uia.ElementFromPoint(point);

            // Find element's ancestors.
            // TODO: need to normalize this to match the tree view I'm using.
            var treeWalker = _uia.CreateTreeWalker(_trueCondition);
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

        private void TreeViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            var vm = ElementsTreeView.ItemFromContainer(item) as AutomationElementViewModel;
            if (_moveOutlineFunc != null)
            {
                var rect = RectHelper.FromCoordinatesAndDimensions(
                    (float)ToDp(vm.BoundingRect.X),
                    (float)ToDp(vm.BoundingRect.Y),
                    (float)ToDp(vm.BoundingRect.Width),
                    (float)ToDp(vm.BoundingRect.Height));
                _moveOutlineFunc(rect);
            }
        }

        private double ToDp(double pixels)
        {
            var dpi = this.GetDpiForWindow();
            return (pixels * 96 / dpi);
        }
    }
}
