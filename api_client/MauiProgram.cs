using api_client.Configuration;
using api_client.Pages;
using api_client.Utils;
using api_client.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace api_client
{
    public static class MauiProgram
    {
        private static string СlientId = Guid.NewGuid().ToString();

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitMediaElement()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            CreateLogger();

            // Позволит заменять конфигурацию прямо на ходу в коде
            builder.Services.AddSingleton<ConfigurationManager>(ConfigurationManager.SetupConfiguration());

            builder.Services.AddSingleton<NetUtils>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsPageViewModel>();

            return builder.Build();
        }

        private static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ClientHash", СlientId)

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                .WriteTo.File(
                    "logs\\debug-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                .WriteTo.File(
                    "logs\\error-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal)
                .WriteTo.File(
                    "logs\\fatal-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                .WriteTo.File(
                    "logs\\info-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                .WriteTo.File(
                    "logs\\verbose-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .WriteTo.Logger(l => l
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                .WriteTo.File(
                    "logs\\warning-.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ClientHash} {Message:lj} {NewLine}{Exception}",
                    rollingInterval: RollingInterval.Hour))

                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }
}