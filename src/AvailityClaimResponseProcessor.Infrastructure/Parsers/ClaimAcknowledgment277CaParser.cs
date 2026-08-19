using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace AvailityClaimResponseProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 277CA (Claim Acknowledgment) files.
/// Extracts per-claim status from STC and CLM segments.
/// </summary>
public class ClaimAcknowledgment277CaParser : IEdiParser
{
    private readonly IStatusMappingService _statusMapper;
    private readonly ILogger<ClaimAcknowledgment277CaParser> _logger;

    public ClaimAcknowledgment277CaParser(IStatusMappingService statusMapper, ILogger<ClaimAcknowledgment277CaParser> logger)
    {
        _statusMapper = statusMapper;
        _logger = logger;
    }

    public bool CanParse(string content) => content.Contains("ST*277") || content.Contains("277CA");

    public async Task<IEnumerable<ClaimResponse>> ParseAsync(string content, string fileName)
    {
        var results = new List<ClaimResponse>();
        try
        {
            var doc = EdiTokenizer.Tokenize(content);

            // Process claim-level segments: CLM followed by STC
            ClaimResponse? currentClaim = null;
            foreach (var segment in doc.Segments)
            {
                if (segment.Id == "CLM")
                {
                    // CLM*ClaimId*Amount~
                    var claimId = segment.GetElement(0);
                    var amountStr = segment.GetElement(1);

                    currentClaim = new ClaimResponse
                    {
                        ClaimId = claimId,
                        FileType = EdiFileType.ClaimAcknowledgment277CA,
                        SourceFileName = fileName,
                        ProcessedAt = DateTime.UtcNow,
                        Status = ClaimStatus.PENDING
                    };

                    if (decimal.TryParse(amountStr, out var amount))
                        currentClaim.SubmittedAmount = amount;

                    results.Add(currentClaim);
                }
                else if (segment.Id == "STC" && currentClaim is not null)
                {
                    // STC*StatusCode:StatusInfo*Date*ActionCode~
                    // Element[0] is composite: StatusCode:qualifier (e.g. A1:19)
                    var statusComposite = segment.GetElement(0);
                    var statusParts = statusComposite.Split(doc.ComponentSeparator);
                    var statusCode = statusParts[0];
                    var dateStr = segment.GetElement(1);

                    var (status, message) = _statusMapper.MapStatus(statusCode, "277CA");
                    currentClaim.Status = status;
                    currentClaim.StatusCode = statusCode;
                    currentClaim.StatusMessage = message;

                    if (statusParts.Length > 1)
                        currentClaim.RejectionReasonCode = statusParts[1];

                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var svcDate))
                        currentClaim.ServiceDate = svcDate;
                }
                else if (segment.Id == "REF" && currentClaim is not null)
                {
                    // REF*1K*ClaimNumber — additional claim reference
                    if (segment.GetElement(0) == "1K" && string.IsNullOrEmpty(currentClaim.ClaimId))
                        currentClaim.ClaimId = segment.GetElement(1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing 277CA file: {FileName}", fileName);
        }

        return await Task.FromResult(results);
    }
}
