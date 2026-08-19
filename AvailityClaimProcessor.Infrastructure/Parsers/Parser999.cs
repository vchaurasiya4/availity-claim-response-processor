using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 999 (Implementation Acknowledgment) EDI files.
/// Uses AK5/AK9 segments to determine acceptance/rejection.
/// </summary>
public class Parser999 : IEdiParser
{
    private readonly IStatusMappingService _statusMapping;

    public Parser999(IStatusMappingService statusMapping)
    {
        _statusMapping = statusMapping;
    }

    public string FileType => "999";

    public bool CanParse(string fileExtension, string fileName)
    {
        return fileExtension.Equals(".edi", StringComparison.OrdinalIgnoreCase)
            && (fileName.ToLower().Contains("999") || fileName.ToLower().Contains("ack"));
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            char segTerm = GetSegmentTerminator(content);
            var segments = content.Split(segTerm, StringSplitOptions.RemoveEmptyEntries);

            string interchangeControlNumber = string.Empty;
            string currentTransactionControlNumber = string.Empty;
            var errors = new List<ProcessingError>();

            foreach (var seg in segments)
            {
                var trimmed = seg.Trim();
                var elements = trimmed.Split('*');
                var segId = elements[0];

                switch (segId)
                {
                    case "ISA":
                        interchangeControlNumber = elements.Length > 13 ? elements[13] : string.Empty;
                        break;

                    case "ST":
                        currentTransactionControlNumber = elements.Length > 2 ? elements[2] : elements.Length > 1 ? elements[1] : string.Empty;
                        break;

                    case "AK2":
                        // AK2*TransactionSetId*GroupControlNumber - starts functional group acknowledgment
                        break;

                    case "AK3":
                        // AK3*SegmentId*SegmentPosition*LoopId*ErrorCode
                        if (elements.Length > 4)
                        {
                            errors.Add(new ProcessingError
                            {
                                ErrorCode = elements[4].TrimEnd('~'),
                                ErrorMessage = $"Segment error in {elements[1]} at position {elements[2]}",
                                Segment = elements[1],
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        break;

                    case "AK4":
                        // AK4*ElementPosition*ComponentPosition*DataElementReferenceNumber*ErrorCode
                        if (elements.Length > 4)
                        {
                            var errCode = elements[4].TrimEnd('~');
                            errors.Add(new ProcessingError
                            {
                                ErrorCode = errCode,
                                ErrorMessage = $"Element error at position {elements[1]}",
                                Segment = "AK4",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        break;

                    case "AK5":
                        // AK5*TransactionSetAcknowledgmentCode*ErrorCode1*...
                        // Per transaction set acknowledgment
                        var ak5Code = elements.Length > 1 ? elements[1] : string.Empty;
                        var ak5Status = _statusMapping.Map999StatusCode(ak5Code);
                        var ak5Claim = new ClaimResponse
                        {
                            ClaimId = $"999-{interchangeControlNumber}-{currentTransactionControlNumber}",
                            Status = ak5Status,
                            RawStatusCode = ak5Code,
                            FileType = FileType,
                            EdiFileId = ediFileId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        foreach (var err in errors)
                            ak5Claim.ProcessingErrors.Add(err);
                        if (ak5Status == ClaimStatus.REJECTED && elements.Length > 2)
                        {
                            var errCode = elements[2].TrimEnd('~');
                            ak5Claim.RejectReasonCode = errCode;
                            ak5Claim.RejectReasonMessage = _statusMapping.GetRejectionMessage(errCode);
                        }
                        result.ClaimResponses.Add(ak5Claim);
                        errors = new List<ProcessingError>();
                        break;

                    case "AK9":
                        // AK9*GroupAcknowledgmentCode*NumberOfTransactionSetsIncluded*NumberReceived*NumberAccepted*ErrorCode
                        var ak9Code = elements.Length > 1 ? elements[1] : string.Empty;
                        var ak9Status = _statusMapping.Map999StatusCode(ak9Code);
                        if (result.ClaimResponses.Count == 0)
                        {
                            // No AK5 entries — create a group-level entry
                            var groupClaim = new ClaimResponse
                            {
                                ClaimId = $"999-GROUP-{interchangeControlNumber}",
                                Status = ak9Status,
                                RawStatusCode = ak9Code,
                                FileType = FileType,
                                EdiFileId = ediFileId,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            if (ak9Status == ClaimStatus.REJECTED && elements.Length > 5)
                            {
                                var errCode = elements[5].TrimEnd('~');
                                groupClaim.RejectReasonCode = errCode;
                                groupClaim.RejectReasonMessage = _statusMapping.GetRejectionMessage(errCode);
                            }
                            result.ClaimResponses.Add(groupClaim);
                        }
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
