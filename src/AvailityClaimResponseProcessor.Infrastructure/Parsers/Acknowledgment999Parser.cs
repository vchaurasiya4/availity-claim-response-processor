using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace AvailityClaimResponseProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 999 (Implementation Acknowledgment) files.
/// Uses AK5/AK9 to determine functional group acceptance.
/// </summary>
public class Acknowledgment999Parser : IEdiParser
{
    private readonly IStatusMappingService _statusMapper;
    private readonly ILogger<Acknowledgment999Parser> _logger;

    public Acknowledgment999Parser(IStatusMappingService statusMapper, ILogger<Acknowledgment999Parser> logger)
    {
        _statusMapper = statusMapper;
        _logger = logger;
    }

    public bool CanParse(string content) =>
        content.Contains("ST*999") || content.Contains("AK9*") || content.Contains("AK5*");

    public async Task<IEnumerable<ClaimResponse>> ParseAsync(string content, string fileName)
    {
        var results = new List<ClaimResponse>();
        try
        {
            var doc = EdiTokenizer.Tokenize(content);
            var isaSegment = doc.Segments.FirstOrDefault(s => s.Id == "ISA");
            var controlNumber = isaSegment?.GetElement(12) ?? "UNKNOWN";

            // AK9 = Functional Group Response Trailer
            // AK9 elements: [0]=AckCode [1]=NumberOfTxSets [2]=NumberReceived [3]=NumberAccepted
            var ak9 = doc.Segments.FirstOrDefault(s => s.Id == "AK9");
            if (ak9 is null) return results;

            var groupAckCode = ak9.GetElement(0); // A=Accepted, R=Rejected, P=Partially Accepted, E=Error
            var (groupStatus, groupMessage) = _statusMapper.MapStatus(groupAckCode, "999");

            // AK5 segments = Transaction Set Acknowledgment
            var ak5Segments = doc.Segments.Where(s => s.Id == "AK5").ToList();
            var errors = new List<ClaimError>();

            // Collect AK3/AK4 error details
            var ak3Segments = doc.Segments.Where(s => s.Id == "AK3").ToList();
            foreach (var ak3 in ak3Segments)
            {
                errors.Add(new ClaimError
                {
                    ErrorCode = ak3.GetElement(3),
                    ErrorMessage = _statusMapper.GetRejectionMessage(ak3.GetElement(3)),
                    Segment = ak3.GetElement(0),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var ak4Segments = doc.Segments.Where(s => s.Id == "AK4").ToList();
            foreach (var ak4 in ak4Segments)
            {
                errors.Add(new ClaimError
                {
                    ErrorCode = ak4.GetElement(3),
                    ErrorMessage = _statusMapper.GetRejectionMessage(ak4.GetElement(3)),
                    Segment = "Element",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var response = new ClaimResponse
            {
                ClaimId = $"999-{controlNumber}",
                Status = groupStatus,
                FileType = EdiFileType.Acknowledgment999,
                SourceFileName = fileName,
                RawContent = content,
                StatusCode = groupAckCode,
                StatusMessage = groupMessage,
                ProcessedAt = DateTime.UtcNow,
                Errors = errors
            };

            results.Add(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing 999 file: {FileName}", fileName);
        }

        return await Task.FromResult(results);
    }
}
