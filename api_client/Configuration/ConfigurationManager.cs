using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace api_client.Configuration;

public class ConfigurationManager
{
    public string FullPath { get; set; }
    public RootSettings RootSettings { get; set; }

    public ConfigurationManager(string FullPath, RootSettings RootSettings)
    {
        this.FullPath = FullPath;
        this.RootSettings = RootSettings;
    }

    public void SaveJsonConfigChanges()
    {
        var serializedConfig = JsonConvert.SerializeObject(RootSettings, Formatting.Indented);
        File.WriteAllText(FullPath, serializedConfig);
    }

    private void CreateConfigurationFile()
    {
        var directoryName = Path.GetDirectoryName(FullPath);

        if (directoryName == null)
        {
            throw new ArgumentNullException(nameof(directoryName), $"{nameof(directoryName)} was null");
        }

        // Проверяем, существует ли папка
        if (!Directory.Exists(directoryName))
        {
            Log.Information("Папка с конфигурацией не найдена.");
            try
            {
                Log.Information("Создание папки с конфигурацией.");
                Directory.CreateDirectory(directoryName);
                Log.Information("Папка с конфигурацией создана.");
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка при создании папки с конфигурацией. {ex.ToString}");
                Environment.Exit(1);
            }
        }

        // Проверяем, существует ли файл
        if (!File.Exists(FullPath))
        {
            Log.Information("Файл с конфигурацией не найден.");
            try
            {
                Log.Information("Создание файла с конфигурацией.");
                SaveJsonConfigChanges();
                Log.Information("Файл с конфигурацией создан.");
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка при создании файла с конфигурацией. {ex.ToString}");
                Environment.Exit(1);
            }
        }
    }

    public static ConfigurationManager SetupConfiguration(string fullPath = "Configuartion/appsettings.json")
    {
        Log.Information("Загрузка конфигурации.");

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(fullPath, optional: true, reloadOnChange: true);

        var configuration = configurationBuilder.Build();

        var rootSettings = configuration.Get<RootSettings>() ?? new RootSettings();

        var newConfig = new ConfigurationManager(fullPath, rootSettings);
        newConfig.CreateConfigurationFile();

        return newConfig;
    }
}