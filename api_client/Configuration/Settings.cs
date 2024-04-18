namespace api_client.Configuration;

public class RootSettings
{
    public ApiSettings API { get; set; } = new ApiSettings();
}

public class ApiSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "8000";
    public double FrameSendingDelay { get; set; } = 0.5d;
}