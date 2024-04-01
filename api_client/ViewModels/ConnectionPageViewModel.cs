using apiclient.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace apiclient.ViewModels;

public partial class ConnectionPageViewModel : ObservableObject
{
    private readonly NetUtils _netUtils;

    public ConnectionPageViewModel(NetUtils netUtils)
    {
        _netUtils = netUtils;
        ConnectCommand = new RelayCommand(async () => await ConnectAsync());
    }

    [ObservableProperty]
    private string _ip;

    [ObservableProperty]
    private string _port;

    [ObservableProperty]
    private string _errorLabel;

    [ObservableProperty]
    private string _welcomeLabelText = "Введите Ip-адрес и порт";

    [ObservableProperty]
    private bool _isConnectButtonEnabled = true;

    [ObservableProperty]
    private bool _isErrorLabelEnabled = false;

    public RelayCommand ConnectCommand { get; }

    private async Task ConnectAsync()
    {
        IsErrorLabelEnabled = false;
        IsConnectButtonEnabled = false;

        if (!_netUtils.SetIpAndPort(Ip, Port))
        {
            IsErrorLabelEnabled = true;
            ErrorLabel = "Некорректный ip-адрес или порт";
        }

        bool result = await _netUtils.CheckServerConnection();
        if (result) // Подключено успешно
        {
            await Shell.Current.GoToAsync("MainPage");
        }
        else
        {
            IsErrorLabelEnabled = true;
            ErrorLabel = "Ошибка подключения к серверу";
        }
        IsConnectButtonEnabled = true;
    }
}