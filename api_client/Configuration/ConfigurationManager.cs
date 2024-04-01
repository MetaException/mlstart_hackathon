using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace api_client.Configuration
{
    internal class ConfigurationManager
    {
        #region Fields

        private static string _ConfigurationDirPath = "Configuration";
        private static string _JsonConfigurationFilePath = "appsettings.json";
        private static string _FullPath = Path.Combine(_ConfigurationDirPath, _JsonConfigurationFilePath);
        private static ConfigurationManager? _instance = null;
        private IConfigurationRoot _Configuration;
        public RootSettings RootSettings;

        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigurationManager();
                }
                return _instance;
            }
        }

        #endregion Fields

        #region Constructors

        private ConfigurationManager()
        {
            Log.Information("Загрузка конфигурации.");

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_FullPath, optional: true, reloadOnChange: true);

            _Configuration = configurationBuilder.Build();

            RootSettings = _Configuration.Get<RootSettings>() ?? new RootSettings();

            // Проверяем, существует ли папка
            if (!Directory.Exists(_ConfigurationDirPath))
            {
                Log.Information("Папка с конфигурацией не найдена.");
                try
                {
                    Log.Information("Создание папки с конфигурацией.");
                    Directory.CreateDirectory(_ConfigurationDirPath);
                    Log.Information("Папка с конфигурацией создана.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Ошибка при создании папки с конфигурацией. {ex.ToString}");
                    Environment.Exit(1);
                }
            }
            // Проверяем, существует ли файл
            if (!File.Exists(_FullPath))
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

        #endregion Constructors

        #region Methods

        private void SaveJsonConfigChanges()
        {
            var serializedConfig = JsonConvert.SerializeObject(RootSettings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_FullPath, serializedConfig);
        }

        #endregion Methods
    }
}