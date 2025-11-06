using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Shared;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AccView
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<TreeViewNode> TreeViewItems= new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var uia = UIAHelpers.CreateUIAutomationInstance();
            var root = uia.GetRootElement();
            var condition = uia.CreateTrueCondition();

            var children = root.FindAll(Windows.Win32.UI.Accessibility.TreeScope.TreeScope_Children, condition);
            for (int i = 0; i < children.Length; i++)
            {
                var element = children.GetElement(i);
                var name = (string)element.GetCurrentPropertyValue(Windows.Win32.UI.Accessibility.UIA_PROPERTY_ID.UIA_NamePropertyId);
                var automationId = (string)element.GetCurrentPropertyValue(Windows.Win32.UI.Accessibility.UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);
                // Check to see if this element has children
                var childCondition = uia.CreateTrueCondition();
                var childElements = element.FindFirst(Windows.Win32.UI.Accessibility.TreeScope.TreeScope_Children, childCondition);
                var hasChildren = childElements != null;
                OutputTextBlock.Text += $"{i}: Name='{name}', AutomationId='{automationId}'\r\n";

                var node = new TreeViewNode()
                {
                    Content = $"{name} ({automationId})",
                };

                if (hasChildren)
                {
                    OutputTextBlock.Text += "   + [...]\r\n";
                    node.Children.Add(new TreeViewNode()
                    {
                        Content = "..."
                    });
                }

                TreeViewItems.Add(node);
            }
        }
    }
}
