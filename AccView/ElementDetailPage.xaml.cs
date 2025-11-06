using AccView.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AccView
{
    public sealed partial class ElementDetailPage : Page
    {
        public AutomationElementViewModel? ViewModel { get; set; }

        public ElementDetailPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var elementViewModel = e.Parameter as ViewModels.AutomationElementViewModel;
            elementViewModel?.LoadDetailedProperties();

            // TODO: notify property changed?
            ViewModel = elementViewModel;
        }

        private void InvokeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.Invoke();
        }
    }
}
