namespace AvailityClaimProcessor.Infrastructure.Services;

public class FtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemotePath { get; set; } = "/EDI/OUT";
    public string LocalDownloadPath { get; set; } = "./downloads";
    public int PollingIntervalSeconds { get; set; } = 60;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}
