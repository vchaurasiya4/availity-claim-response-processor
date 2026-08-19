using FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvailityClaimResponseProcessor.Infrastructure.Ftp;

public interface IFtpService
{
    Task<IEnumerable<string>> ListFilesAsync(CancellationToken ct = default);
    Task<string?> DownloadFileAsync(string remoteFileName, CancellationToken ct = default);
}

public class FtpService : IFtpService
{
    private readonly FtpSettings _settings;
    private readonly ILogger<FtpService> _logger;

    public FtpService(IOptions<FtpSettings> settings, ILogger<FtpService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                using var client = CreateClient();
                await client.Connect(ct);
                var items = await client.GetListing(_settings.RemoteFolder, ct);
                var files = items
                    .Where(i => i.Type == FtpObjectType.File)
                    .Select(i => i.Name)
                    .ToList();
                await client.Disconnect(ct);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FTP list attempt {Attempt}/{MaxAttempts} failed", attempt, _settings.RetryCount);
                if (attempt < _settings.RetryCount)
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds), ct);
            }
        }

        _logger.LogError("All FTP list attempts failed for {Host}", _settings.Host);
        return Enumerable.Empty<string>();
    }

    public async Task<string?> DownloadFileAsync(string remoteFileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_settings.LocalDownloadPath);
        var localPath = Path.Combine(_settings.LocalDownloadPath, remoteFileName);
        var remotePath = $"{_settings.RemoteFolder.TrimEnd('/')}/{remoteFileName}";

        for (var attempt = 1; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                using var client = CreateClient();
                await client.Connect(ct);
                var result = await client.DownloadFile(localPath, remotePath, FtpLocalExists.Skip, token: ct);
                await client.Disconnect(ct);

                if (result == FtpStatus.Success || result == FtpStatus.Skipped)
                {
                    _logger.LogInformation("Downloaded {File} to {LocalPath}", remoteFileName, localPath);
                    return await File.ReadAllTextAsync(localPath, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FTP download attempt {Attempt}/{MaxAttempts} failed for {File}", attempt, _settings.RetryCount, remoteFileName);
                if (attempt < _settings.RetryCount)
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds), ct);
            }
        }

        _logger.LogError("All FTP download attempts failed for {File}", remoteFileName);
        return null;
    }

    private AsyncFtpClient CreateClient() =>
        new(_settings.Host, _settings.Username, _settings.Password, _settings.Port);
}
