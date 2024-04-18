using api_client.Pages;

namespace api_client
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("MainPage/SettingsPage", typeof(SettingsPage));
        }
    }
}