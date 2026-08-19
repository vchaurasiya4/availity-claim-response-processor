using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace AvailityClaimResponseProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses TA1 (Interchange Acknowledgment) segments.
/// TA1 acknowledges the ISA/IEA envelope.
/// </summary>
public class Ta1Parser : IEdiParser
{
    private readonly IStatusMappingService _statusMapper;
    private readonly ILogger<Ta1Parser> _logger;

    public Ta1Parser(IStatusMappingService statusMapper, ILogger<Ta1Parser> logger)
    {
        _statusMapper = statusMapper;
        _logger = logger;
    }

    public bool CanParse(string content) => content.Contains("\nTA1*") || content.Contains("~TA1*");

    public async Task<IEnumerable<ClaimResponse>> ParseAsync(string content, string fileName)
    {
        var results = new List<ClaimResponse>();
        try
        {
            var doc = EdiTokenizer.Tokenize(content);
            var isaSegment = doc.Segments.FirstOrDefault(s => s.Id == "ISA");
            var ta1Segment = doc.Segments.FirstOrDefault(s => s.Id == "TA1");

            if (ta1Segment is null) return results;

            // TA1 elements: [0]=InterchangeControlNumber [1]=Date [2]=Time [3]=AcknowledgmentCode [4]=NoteCode
            var controlNumber = ta1Segment.GetElement(0);
            var ackCode = ta1Segment.GetElement(3); // A=Accepted, R=Rejected, E=Error
            var noteCode = ta1Segment.GetElement(4);

            var (status, message) = _statusMapper.MapStatus(ackCode, "TA1");
            var claimId = $"TA1-{controlNumber}";

            results.Add(new ClaimResponse
            {
                ClaimId = claimId,
                Status = status,
                FileType = EdiFileType.TA1,
                SourceFileName = fileName,
                RawContent = content,
                StatusCode = ackCode,
                StatusMessage = message,
                RejectionReasonCode = noteCode,
                RejectionReasonMessage = noteCode != "000" ? _statusMapper.GetRejectionMessage(noteCode) : null,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing TA1 file: {FileName}", fileName);
        }

        return await Task.FromResult(results);
    }
}
