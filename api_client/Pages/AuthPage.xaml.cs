using apiclient.ViewModels;

namespace apiclient.Pages
{
    public partial class AuthPage : ContentPage
    {
        public AuthPage(AuthPageViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}