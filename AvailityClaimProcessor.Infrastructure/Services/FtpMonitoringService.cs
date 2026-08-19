using FluentFTP;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvailityClaimProcessor.Infrastructure.Services;

public class FtpMonitoringService : BackgroundService
{
    private readonly FtpSettings _settings;
    private readonly EdiProcessingService _processingService;
    private readonly ILogger<FtpMonitoringService> _logger;

    private static readonly string[] SupportedExtensions =
        { ".edi", ".ebr", ".ebt" };

    public FtpMonitoringService(
        IOptions<FtpSettings> settings,
        EdiProcessingService processingService,
        ILogger<FtpMonitoringService> logger)
    {
        _settings = settings.Value;
        _processingService = processingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FTP monitoring service started. Polling every {Seconds}s",
            _settings.PollingIntervalSeconds);

        Directory.CreateDirectory(_settings.LocalDownloadPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollFtpAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in FTP polling loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task PollFtpAsync(CancellationToken ct)
    {
        using var ftp = new AsyncFtpClient(_settings.Host, _settings.Username, _settings.Password, _settings.Port);

        for (int attempt = 1; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                await ftp.Connect(ct);
                break;
            }
            catch (Exception ex) when (attempt < _settings.RetryCount)
            {
                _logger.LogWarning("FTP connect attempt {Attempt}/{Max} failed: {Error}", attempt, _settings.RetryCount, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds), ct);
            }
        }

        if (!ftp.IsConnected)
        {
            _logger.LogError("Could not connect to FTP after {Retries} attempts", _settings.RetryCount);
            return;
        }

        try
        {
            var items = await ftp.GetListing(_settings.RemotePath, ct);
            foreach (var item in items)
            {
                if (item.Type != FtpObjectType.File) continue;

                var ext = Path.GetExtension(item.Name).ToLower();
                if (!SupportedExtensions.Contains(ext)) continue;

                var localPath = Path.Combine(_settings.LocalDownloadPath, item.Name);
                if (File.Exists(localPath))
                {
                    _logger.LogDebug("Skipping already-downloaded file: {File}", item.Name);
                    continue;
                }

                try
                {
                    _logger.LogInformation("Downloading {File}", item.FullName);
                    await ftp.DownloadFile(localPath, item.FullName, FtpLocalExists.Skip, FtpVerify.None, null, ct);

                    var content = await File.ReadAllTextAsync(localPath, ct);
                    await _processingService.ProcessFileAsync(localPath, content, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading/processing file {File}", item.Name);
                }
            }
        }
        finally
        {
            await ftp.Disconnect(ct);
        }
    }
}
