using api_client.ViewModels;

namespace api_client.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }

        // При использовании обычных команд
        // Если перейти на страницу с настройками, а затем вернуться назад, используя backbutton, то кнопки menu перестанут отвечать на нажатия
        // Поэтому используем этот костыль

        private void OpenFileClicked(object sender, EventArgs e)
        {
            ((MainPageViewModel)BindingContext).OpenFileCommand.Execute(null);
        }

        private void SaveFileClicked(object sender, EventArgs e)
        {
            ((MainPageViewModel)BindingContext).SaveFileCommand.Execute(null);
        }

        private void OpenSettingsClicked(object sender, EventArgs e)
        {
            ((MainPageViewModel)BindingContext).OpenSettingsCommand.Execute(null);
        }
    }
}