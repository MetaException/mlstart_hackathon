namespace api_client.Configuration;

public class RootSettings
{
    public ConnectionServiceSettings ConnectionService { get; set; } = new ConnectionServiceSettings();
    public LoggingSettings Logging { get; set; } = new LoggingSettings();
    public ApiSettings API { get; set; } = new ApiSettings();
}

public class ConnectionServiceSettings
{
    public ServerSettings Server { get; set; } = new ServerSettings();

    public int SendDelayIfNoConnection { get; set; } = 1000;
    public int RecieveDelayIfNoConnection { get; set; } = 1000;
    public int MonitoringDelay { get; set; } = 4000;
    public int ResponseIterationDelay { get; set; } = 500;

    public class ServerSettings
    {
        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 8888;
    }
}

public class LoggingSettings
{
    public int LogMaxSizeInBytes { get; set; } = 20000;
}
public class ApiSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8000;
}