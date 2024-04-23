using api_client.Configuration;
using api_client.Model;
using OpenCvSharp;
using Serilog;
using System.Text.Json;

namespace api_client.Utils;

public class NetUtils
{
    private readonly HttpClientHandler _handler;
    private readonly ConfigurationManager _settings;

    private HttpClient _client;

    public NetUtils(ConfigurationManager settings)
    {
        _handler = new HttpClientHandler();

        _settings = settings;

        SetIpAndPort(_settings.RootSettings.API.Host, _settings.RootSettings.API.Port); // TODO: учесть что в конфиге могут стоять некорректные значения
    }

    public async Task<bool> ResetTrackingParameters()
    {
        try
        {
            var response = await _client.GetAsync(ApiLinks.ResetLink);
            response.EnsureSuccessStatusCode();

            Log.Information("Успешно сброшены параметры трекинга");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        return false;
    }

    public async Task<bool> CheckServerConnection()
    {
        try
        {
            var response = await _client.GetAsync(ApiLinks.HealthLink);
            response.EnsureSuccessStatusCode();

            Log.Information($"Успешно подключено к {response.RequestMessage.RequestUri.AbsoluteUri}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        return false;
    }

    public async Task<List<FrameInfo>> SendVideoFrameAsync(Mat frame, string fileName)
    {
        try
        {
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(frame.ToBytes()), "image", fileName}
            };

            var response = await _client.PostAsync(ApiLinks.DataLink, content);

            List<FrameInfo> frameInfos;
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                frameInfos = await JsonSerializer.DeserializeAsync<List<FrameInfo>>(stream);
            }

            if (frameInfos is null)
                throw new ArgumentNullException(nameof(frameInfos), "Сайт вернул null");

            return frameInfos;
        }
        catch (Exception ex)
        {
            Log.Error($"Произошла ошибка отправки кадра видео по пути: {fileName}: {ex.Message}");
            throw;
        }
    }

    public bool SetIpAndPort(string ip, string port)
    {
        try
        {
            // Задавать параметры можно только до отправки первого запроса
            _client = new HttpClient(_handler) { BaseAddress = new Uri($"http://{ip}:{port}") };
            Log.Information($"Базовый ip адресс изменён на {_client.BaseAddress}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{ex.Message} ip = {ip}, port = {port}");
        }
        return false;
    }
}