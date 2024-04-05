﻿using apiclient.Model;
using Serilog;
using System.Net.Http.Json;

namespace apiclient.Utils;

public class NetUtils
{
    private readonly HttpClientHandler _handler;
    private HttpClient _client;

    public NetUtils()
    {
        _handler = new HttpClientHandler();
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