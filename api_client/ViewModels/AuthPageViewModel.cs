using apiclient.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static apiclient.Model.ResponseEnum;

namespace apiclient.ViewModels;

public partial class AuthPageViewModel : ObservableObject
{
    private readonly NetUtils _netUtils;

    public AuthPageViewModel(NetUtils netUtils)
    {
        _netUtils = netUtils;

        AuthCommand = new RelayCommand<string>(async (authType) => await AuthAsync(authType));
    }

    [ObservableProperty]
    private string _login;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private string _errorLabel;

    [ObservableProperty]
    private string _welcomeLabelText = "Введите логин и пароль";

    [ObservableProperty]
    private bool _isLoginButtonEnabled = true;

    [ObservableProperty]
    private bool _isRegisterButtonEnabled = true;

    [ObservableProperty]
    private bool _isErrorLabelEnabled = false;

    public RelayCommand<string> AuthCommand { get; }

    private async Task AuthAsync(string authType)
    {
        IsErrorLabelEnabled = false;

        NetUtilsResponseCodes result;
        if (authType == "login")
        {
            IsLoginButtonEnabled = false;

            result = await _netUtils.LoginAsync(Login, Password);
        }
        else
        {
            IsRegisterButtonEnabled = false;

            result = await _netUtils.RegisterAsync(Login, Password);
        }

        if (result == NetUtilsResponseCodes.OK)
        {
            await Shell.Current.GoToAsync("MainPage");
        }
        else
        {
            IsErrorLabelEnabled = true;

            if (result == NetUtilsResponseCodes.CANTCONNECTTOTHESERVER) //Переписать
            {
                ErrorLabel = "Не удалось соединиться с сервером";
            }
            else if (result == NetUtilsResponseCodes.BADREQUEST)
            {
                ErrorLabel = "Некорректно введён логин или пароль";
            }
            else
            {
                if (authType == "login")
                {
                    HandleLoginError(result);
                }
                else
                {
                    HandleRegisterError(result);
                }
            }
        }

        IsRegisterButtonEnabled = true;
        IsLoginButtonEnabled = true;
    }

    private void HandleLoginError(NetUtilsResponseCodes responseCode)
    {
        if (responseCode == NetUtilsResponseCodes.UNATHROIZED)
        {
            ErrorLabel = "Неверный логин или пароль";
        }
        else if (responseCode == NetUtilsResponseCodes.ERROR)
        {
            ErrorLabel = "Произошла ошибка при выполнении входа";
        }
    }

    private void HandleRegisterError(NetUtilsResponseCodes responseCode)
    {
        if (responseCode == NetUtilsResponseCodes.USERISALREDYEXISTS)
        {
            ErrorLabel = "Пользователь уже зарегистрирован";
        }
        else if (responseCode == NetUtilsResponseCodes.ERROR)
        {
            ErrorLabel = "Произошла ошибка при регистрации пользователя";
        }
    }
}