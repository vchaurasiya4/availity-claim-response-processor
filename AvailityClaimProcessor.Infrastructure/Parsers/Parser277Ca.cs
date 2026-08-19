using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 277CA (Claim Acknowledgment) EDI files.
/// Extracts claim-level status using CLM and STC segments.
/// </summary>
public class Parser277Ca : IEdiParser
{
    private readonly IStatusMappingService _statusMapping;

    public Parser277Ca(IStatusMappingService statusMapping)
    {
        _statusMapping = statusMapping;
    }

    public string FileType => "277CA";

    public bool CanParse(string fileExtension, string fileName)
    {
        return fileExtension.Equals(".edi", StringComparison.OrdinalIgnoreCase)
            && (fileName.ToLower().Contains("277") || fileName.ToLower().Contains("277ca"));
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            char segTerm = GetSegmentTerminator(content);
            var segments = content.Split(segTerm, StringSplitOptions.RemoveEmptyEntries);

            string currentClaimId = string.Empty;
            DateTime? currentServiceDate = null;

            foreach (var seg in segments)
            {
                var trimmed = seg.Trim();
                var elements = trimmed.Split('*');
                var segId = elements[0];

                switch (segId)
                {
                    case "CLM":
                        // CLM*ClaimId*Amount*~
                        currentClaimId = elements.Length > 1 ? elements[1] : string.Empty;
                        break;

                    case "DTP":
                        // DTP*472*D8*CCYYMMDD - Service date
                        if (elements.Length > 3 && elements[1] == "472")
                        {
                            var dateStr = elements[3].TrimEnd('~');
                            if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                                System.Globalization.DateTimeStyles.None, out var dt))
                                currentServiceDate = dt;
                        }
                        break;

                    case "STC":
                        // STC*StatusCategoryCode:StatusCode*Date*ActionCode*TotalClaimChargeAmount
                        if (string.IsNullOrEmpty(currentClaimId)) break;

                        var stcStatusComposite = elements.Length > 1 ? elements[1] : string.Empty;
                        var stcParts = stcStatusComposite.Split(':');
                        var stcStatusCode = stcParts.Length > 0 ? stcParts[0] : string.Empty;
                        var stcCategoryCode = stcParts.Length > 1 ? stcParts[1] : string.Empty;
                        var stcDateStr = elements.Length > 2 ? elements[2] : string.Empty;
                        decimal? totalAmount = null;
                        if (elements.Length > 4 && decimal.TryParse(elements[4], out var amt))
                            totalAmount = amt;

                        DateTime? stcDate = null;
                        if (DateTime.TryParseExact(stcDateStr, "yyyyMMdd", null,
                            System.Globalization.DateTimeStyles.None, out var sd))
                            stcDate = sd;

                        var status = _statusMapping.MapEdiStatusToClaimStatus(stcStatusCode);
                        var claimResponse = new ClaimResponse
                        {
                            ClaimId = currentClaimId,
                            Status = status,
                            RawStatusCode = stcStatusCode,
                            FileType = FileType,
                            ServiceDate = currentServiceDate ?? stcDate,
                            SubmittedAmount = totalAmount,
                            EdiFileId = ediFileId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        if (status == ClaimStatus.REJECTED)
                        {
                            // Second STC composite may carry error code
                            var errCode = elements.Length > 8 ? elements[8].TrimEnd('~') : stcCategoryCode;
                            claimResponse.RejectReasonCode = errCode;
                            claimResponse.RejectReasonMessage = _statusMapping.GetRejectionMessage(errCode);
                            claimResponse.ProcessingErrors.Add(new ProcessingError
                            {
                                ErrorCode = errCode,
                                ErrorMessage = claimResponse.RejectReasonMessage ?? errCode,
                                Segment = "STC",
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        result.ClaimResponses.Add(claimResponse);
                        break;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    private static char GetSegmentTerminator(string content)
    {
        if (content.Length > 105)
            return content[105];
        return '~';
    }
}
