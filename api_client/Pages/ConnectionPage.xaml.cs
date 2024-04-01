using apiclient.ViewModels;

namespace apiclient.Pages
{
    public partial class ConnectionPage : ContentPage
    {
        public ConnectionPage(ConnectionPageViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}