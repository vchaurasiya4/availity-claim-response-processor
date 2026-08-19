using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses TA1 (Interchange Acknowledgment) segments from EDI files.
/// TA1 validates the ISA envelope of the interchange.
/// </summary>
public class Ta1Parser : IEdiParser
{
    private readonly IStatusMappingService _statusMapping;

    public Ta1Parser(IStatusMappingService statusMapping)
    {
        _statusMapping = statusMapping;
    }

    public string FileType => "TA1";

    public bool CanParse(string fileExtension, string fileName)
    {
        var lower = fileName.ToLower();
        return fileExtension.Equals(".edi", StringComparison.OrdinalIgnoreCase)
            && (lower.Contains("ta1") || lower.Contains("interchange"));
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            char segment = GetSegmentTerminator(content);
            var segments = content.Split(segment, StringSplitOptions.RemoveEmptyEntries);

            foreach (var seg in segments)
            {
                var trimmed = seg.Trim();
                if (!trimmed.StartsWith("TA1")) continue;

                var elements = trimmed.Split('*');
                // TA1*InterchangeControlNumber*Date*Time*AcknowledgmentCode*NoteCode
                if (elements.Length < 5) continue;

                var controlNumber = elements.Length > 1 ? elements[1] : string.Empty;
                var ackCode = elements.Length > 4 ? elements[4] : string.Empty;
                var noteCode = elements.Length > 5 ? elements[5].TrimEnd('~') : string.Empty;

                var status = _statusMapping.MapTa1StatusCode(ackCode);
                var claimResponse = new ClaimResponse
                {
                    ClaimId = $"TA1-{controlNumber}",
                    Status = status,
                    RawStatusCode = ackCode,
                    FileType = FileType,
                    EdiFileId = ediFileId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (status == ClaimStatus.REJECTED && !string.IsNullOrEmpty(noteCode))
                {
                    claimResponse.RejectReasonCode = noteCode;
                    claimResponse.RejectReasonMessage = _statusMapping.GetRejectionMessage(noteCode);
                    claimResponse.ProcessingErrors.Add(new ProcessingError
                    {
                        ErrorCode = noteCode,
                        ErrorMessage = claimResponse.RejectReasonMessage ?? noteCode,
                        Segment = "TA1",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                result.ClaimResponses.Add(claimResponse);
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
        // ISA segment ends with a segment terminator character at position 105
        if (content.Length > 105)
            return content[105];
        return '~';
    }
}
