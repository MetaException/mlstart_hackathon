using apiclient.Pages;

namespace apiclient
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("AuthPage", typeof(AuthPage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
        }
    }
}