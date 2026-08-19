using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using AvailityClaimResponseProcessor.Infrastructure.Ftp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvailityClaimResponseProcessor.Infrastructure.Services;

public class EdiFileProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFtpService _ftpService;
    private readonly FtpSettings _ftpSettings;
    private readonly ILogger<EdiFileProcessingService> _logger;

    public EdiFileProcessingService(
        IServiceScopeFactory scopeFactory,
        IFtpService ftpService,
        IOptions<FtpSettings> ftpSettings,
        ILogger<EdiFileProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _ftpService = ftpService;
        _ftpSettings = ftpSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EDI File Processing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNewFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EDI file processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_ftpSettings.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessNewFilesAsync(CancellationToken ct)
    {
        var files = await _ftpService.ListFilesAsync(ct);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();
        var parsers = scope.ServiceProvider.GetRequiredService<IEnumerable<IEdiParser>>();

        foreach (var fileName in files)
        {
            if (await repository.IsFileProcessedAsync(fileName))
            {
                _logger.LogDebug("Skipping already-processed file: {File}", fileName);
                continue;
            }

            var fileRecord = new EdiFileRecord
            {
                FileName = fileName,
                FileType = DetectFileType(fileName),
                DownloadedAt = DateTime.UtcNow
            };

            try
            {
                var content = await _ftpService.DownloadFileAsync(fileName, ct);
                if (content is null)
                {
                    fileRecord.HasErrors = true;
                    fileRecord.ErrorMessage = "Failed to download file";
                    await repository.SaveEdiFileRecordAsync(fileRecord);
                    continue;
                }

                var parser = parsers.FirstOrDefault(p => p.CanParse(content));
                if (parser is null)
                {
                    _logger.LogWarning("No parser found for file: {File}", fileName);
                    fileRecord.HasErrors = true;
                    fileRecord.ErrorMessage = "No suitable parser found";
                    await repository.SaveEdiFileRecordAsync(fileRecord);
                    continue;
                }

                var responses = (await parser.ParseAsync(content, fileName)).ToList();
                foreach (var response in responses)
                    await repository.UpsertAsync(response);

                fileRecord.IsProcessed = true;
                fileRecord.ProcessedAt = DateTime.UtcNow;
                await repository.SaveEdiFileRecordAsync(fileRecord);

                _logger.LogInformation("Processed {File}: {Count} claim(s)", fileName, responses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file: {File}", fileName);
                fileRecord.HasErrors = true;
                fileRecord.ErrorMessage = ex.Message;
                await repository.SaveEdiFileRecordAsync(fileRecord);
            }
        }
    }

    private static EdiFileType DetectFileType(string fileName)
    {
        var upper = fileName.ToUpperInvariant();
        if (upper.Contains("TA1")) return EdiFileType.TA1;
        if (upper.Contains("999")) return EdiFileType.Acknowledgment999;
        if (upper.Contains("277")) return EdiFileType.ClaimAcknowledgment277CA;
        if (upper.Contains("835")) return EdiFileType.RemittanceAdvice835;
        return EdiFileType.Unknown;
    }
}
