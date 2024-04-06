using api_client.Configuration;
using api_client.Model;
using apiclient.Model;
using OpenCvSharp;
using Serilog;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace apiclient.Utils;

public class NetUtils
{
    private readonly HttpClientHandler _handler;
    private readonly Configuration _settings;

    private HttpClient _client;

    public NetUtils(Configuration settings)
    {
        _handler = new HttpClientHandler();

        _settings = settings;

        SetIpAndPort(_settings.RootSettings.API.Host, _settings.RootSettings.API.Port); // TODO: учесть что в конфиге могут стоять некорректные значения
    }

    public async Task<bool> CheckServerConnection()
    {
        try
        {
            var response = await _client.GetAsync(ApiLinks.HealthLink);
            response.EnsureSuccessStatusCode();

            Log.Information($"Successfully connected to {response.RequestMessage.RequestUri.AbsoluteUri}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        return false;
    }

    public async Task<Stream> DownloadFileToStream(string url)
    {
        var stream = new MemoryStream();
        HttpResponseMessage response = await _client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            await response.Content.CopyToAsync(stream);
        }
        return stream;
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

            var jsonContent = await response.Content.ReadAsStringAsync();
            var frameInfo = JsonSerializer.Deserialize<List<FrameInfo>>(jsonContent); // Обработать случай ошибки

            if (frameInfo is null)
                throw new ArgumentNullException(nameof(frameInfo), "Сайт вернул null");

            return frameInfo;
        }
        catch (Exception ex)
        {
            Log.Error($"Произошла ошибка отправки изображения по пути {fileName}: {ex.Message}");
            throw;
        }
    }

    public async Task<VideoResponseModel> SendVideoAsync(Stream fileStream, string filePath)
    {
        try
        {
            var content = new MultipartFormDataContent
            {
                { new StreamContent(fileStream), "video", filePath}
            };

            var response = await _client.PostAsync(ApiLinks.DataLink, content);

            var videoInfo = await response.Content.ReadFromJsonAsync<VideoResponseModel>();

            if (videoInfo is null)
                throw new ArgumentNullException(nameof(videoInfo), "Сайт вернул null");

            return videoInfo;
        }
        catch (Exception ex)
        {
            Log.Error($"Произошла ошибка отправки изображения по пути {filePath}: {ex.Message}");
            throw;
        }
    }

    /*
    public async Task<ImageInfo> SendImageAsync(Stream fileStream, string filePath)
    {
        try
        {
            var content = new MultipartFormDataContent
            {
                { new StreamContent(fileStream), "image", filePath}
            };

            var response = await _client.PostAsync(ApiLinks.DataLink, content);

            var imageInfo = await response.Content.ReadFromJsonAsync<ImageInfo>();

            if (imageInfo is null)
                throw new ArgumentNullException(nameof(imageInfo), "Сайт вернул null");

            return imageInfo;
        }
        catch (Exception ex)
        {
            Log.Error($"Произошла ошибка отправки изображения по пути {filePath}: {ex.Message}");
            throw;
        }
    }
    */

    public bool SetIpAndPort(string ip, string port)
    {
        try
        {
            // Задавать параметры можно только до отправки первого запроса
            _client = new HttpClient(_handler) { BaseAddress = new Uri($"http://{ip}:{port}") };
            Log.Information($"Successfully changed base address to {_client.BaseAddress}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{ex.Message} ip = {ip}, port = {port}");
        }
        return false;
    }
}