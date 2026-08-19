using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AvailityClaimProcessor.Infrastructure.Services;

public class EdiProcessingService
{
    private readonly ClaimProcessorDbContext _db;
    private readonly IEnumerable<IEdiParser> _parsers;
    private readonly ILogger<EdiProcessingService> _logger;

    public EdiProcessingService(
        ClaimProcessorDbContext db,
        IEnumerable<IEdiParser> parsers,
        ILogger<EdiProcessingService> logger)
    {
        _db = db;
        _parsers = parsers;
        _logger = logger;
    }

    public async Task<EdiFile> ProcessFileAsync(string filePath, string content, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(fileName);

        var ediFile = new EdiFile
        {
            FileName = fileName,
            FilePath = filePath,
            FileType = DetectFileType(extension, fileName),
            FileSize = content.Length,
            DownloadedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _db.EdiFiles.Add(ediFile);
        await _db.SaveChangesAsync(ct);

        var parser = _parsers.FirstOrDefault(p => p.CanParse(extension, fileName));
        if (parser == null)
        {
            ediFile.ErrorMessage = $"No parser found for extension '{extension}' and file '{fileName}'";
            _logger.LogWarning("No parser for {File}", fileName);
            await _db.SaveChangesAsync(ct);
            return ediFile;
        }

        try
        {
            var parseResult = parser.Parse(content, ediFile.Id);
            if (!parseResult.Success)
            {
                ediFile.ErrorMessage = parseResult.ErrorMessage;
                _logger.LogError("Parse error for {File}: {Error}", fileName, parseResult.ErrorMessage);
            }
            else
            {
                foreach (var cr in parseResult.ClaimResponses)
                    _db.ClaimResponses.Add(cr);
                foreach (var br in parseResult.BenefitRecords)
                    _db.BenefitRecords.Add(br);

                ediFile.IsProcessed = true;
                _logger.LogInformation("Processed {File}: {ClaimCount} claims, {BenefitCount} benefits",
                    fileName, parseResult.ClaimResponses.Count, parseResult.BenefitRecords.Count);
            }
        }
        catch (Exception ex)
        {
            ediFile.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exception processing {File}", fileName);
        }

        await _db.SaveChangesAsync(ct);
        return ediFile;
    }

    private static string DetectFileType(string extension, string fileName)
    {
        var ext = extension.ToLower();
        var name = fileName.ToLower();
        if (ext == ".ebr") return "EBR";
        if (ext == ".ebt") return "EBT";
        if (name.Contains("ta1")) return "TA1";
        if (name.Contains("999")) return "999";
        if (name.Contains("277")) return "277CA";
        if (name.Contains("835")) return "835";
        return ext.TrimStart('.').ToUpper();
    }
}
