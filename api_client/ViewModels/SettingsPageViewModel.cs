using api_client.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace api_client.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly ConfigurationManager _configuration;

    [ObservableProperty]
    private double _intervalValue = 0.01d;

    public AsyncRelayCommand UpdateSettingsButtonCommand { get; }

    public SettingsPageViewModel(ConfigurationManager configuration)
    {
        Log.Debug($"Открытие страницы настроек.");

        _configuration = configuration;

        UpdateSettingsButtonCommand = new AsyncRelayCommand(UpdateSettings);

        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        Log.Debug($"Страница настроек. Загрузка конфигурации.");

        IntervalValue = _configuration.RootSettings.API.FrameSendingDelay;

        Log.Information("Данные из конфигурации успешно получены");
    }

    private async Task UpdateSettings()
    {
        Log.Debug($"Страница настроек. Обновление конфигурации.");

        double newValue = Math.Round(IntervalValue, 2);

        IntervalValue = newValue;
        _configuration.RootSettings.API.FrameSendingDelay = newValue;

        _configuration.SaveJsonConfigChanges();

        Log.Information("Настройки успешно обновлены");
        await App.Current.MainPage.DisplayAlert("Внимание", "Настройки успешно обновлены", "OK");
    }
}