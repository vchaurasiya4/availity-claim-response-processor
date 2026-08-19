namespace AvailityClaimResponseProcessor.Infrastructure.Ftp;

public class FtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemoteFolder { get; set; } = "/EDI/OUT";
    public string LocalDownloadPath { get; set; } = "downloads";
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int PollingIntervalSeconds { get; set; } = 60;
}
