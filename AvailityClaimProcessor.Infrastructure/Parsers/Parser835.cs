using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 835 (Remittance Advice) EDI files.
/// Extracts payment details from CLP segments.
/// CLP*ClaimId*ClaimStatusCode*TotalCharges*PaymentAmount*PatientResponsibility*ClaimFilingIndicator
/// </summary>
public class Parser835 : IEdiParser
{
    private readonly IStatusMappingService _statusMapping;

    public Parser835(IStatusMappingService statusMapping)
    {
        _statusMapping = statusMapping;
    }

    public string FileType => "835";

    public bool CanParse(string fileExtension, string fileName)
    {
        return fileExtension.Equals(".edi", StringComparison.OrdinalIgnoreCase)
            && (fileName.ToLower().Contains("835") || fileName.ToLower().Contains("remit"));
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            char segTerm = GetSegmentTerminator(content);
            var segments = content.Split(segTerm, StringSplitOptions.RemoveEmptyEntries);

            string? currentCheckNumber = null;
            decimal? currentCheckAmount = null;
            DateTime? currentCheckDate = null;
            string? payerName = null;
            string? payeeName = null;
            string? currentClaimId = null;
            ClaimResponse? currentClaim = null;

            foreach (var seg in segments)
            {
                var trimmed = seg.Trim();
                var elements = trimmed.Split('*');
                var segId = elements[0];

                switch (segId)
                {
                    case "N1":
                        // N1*PR*PayerName or N1*PE*PayeeName
                        if (elements.Length > 2)
                        {
                            if (elements[1] == "PR") payerName = elements[2];
                            else if (elements[1] == "PE") payeeName = elements[2];
                        }
                        break;

                    case "BPR":
                        // BPR*I*Amount*C*ACH*...*...*...*...*...*...*Date
                        if (elements.Length > 2 && decimal.TryParse(elements[2], out var checkAmt))
                            currentCheckAmount = checkAmt;
                        if (elements.Length > 16)
                        {
                            var dateStr = elements[16].TrimEnd('~');
                            if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                                System.Globalization.DateTimeStyles.None, out var cd))
                                currentCheckDate = cd;
                        }
                        break;

                    case "TRN":
                        // TRN*1*CheckNumber*PayerID
                        if (elements.Length > 2)
                            currentCheckNumber = elements[2];
                        break;

                    case "CLP":
                        // CLP*ClaimId*ClaimStatusCode*TotalCharges*PaymentAmount*PatientResponsibility
                        currentClaimId = elements.Length > 1 ? elements[1] : string.Empty;
                        var clpStatusCode = elements.Length > 2 ? elements[2] : string.Empty;
                        decimal? submitted = null, paid = null, patientResp = null;

                        if (elements.Length > 3 && decimal.TryParse(elements[3], out var sub))
                            submitted = sub;
                        if (elements.Length > 4 && decimal.TryParse(elements[4], out var p))
                            paid = p;
                        if (elements.Length > 5 && decimal.TryParse(elements[5], out var pr))
                            patientResp = pr;

                        var status = MapClpStatus(clpStatusCode);
                        currentClaim = new ClaimResponse
                        {
                            ClaimId = currentClaimId,
                            Status = status,
                            RawStatusCode = clpStatusCode,
                            FileType = FileType,
                            SubmittedAmount = submitted,
                            PaidAmount = paid,
                            PatientResponsibility = patientResp,
                            EdiFileId = ediFileId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        currentClaim.RemittanceDetails.Add(new RemittanceDetail
                        {
                            ClaimId = currentClaimId,
                            CheckNumber = currentCheckNumber,
                            CheckAmount = currentCheckAmount,
                            CheckDate = currentCheckDate,
                            PayerName = payerName,
                            PayeeName = payeeName,
                            CreatedAt = DateTime.UtcNow
                        });

                        result.ClaimResponses.Add(currentClaim);
                        break;

                    case "CAS":
                        // CAS*GroupCode*ReasonCode*Amount - Claim Adjustment
                        if (currentClaim != null && elements.Length > 2)
                        {
                            var reasonCode = elements[2];
                            var message = _statusMapping.GetRejectionMessage(reasonCode);
                            if (elements[1] == "CO" || elements[1] == "PR" || elements[1] == "OA")
                            {
                                decimal? adjAmt = null;
                                if (elements.Length > 3 && decimal.TryParse(elements[3], out var aa))
                                    adjAmt = aa;
                                // Compute approved = submitted - adjustment
                                if (currentClaim.SubmittedAmount.HasValue && adjAmt.HasValue)
                                    currentClaim.ApprovedAmount = currentClaim.SubmittedAmount - adjAmt;
                            }
                        }
                        break;

                    case "CLP2":
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

    private static ClaimStatus MapClpStatus(string code) => code switch
    {
        "1" => ClaimStatus.PROCESSED,
        "2" => ClaimStatus.PROCESSED,
        "3" => ClaimStatus.PROCESSED,
        "4" => ClaimStatus.REJECTED,
        "19" => ClaimStatus.PENDING,
        "20" => ClaimStatus.PENDING,
        "22" => ClaimStatus.REJECTED,
        _ => ClaimStatus.PENDING
    };

    private static char GetSegmentTerminator(string content)
    {
        if (content.Length > 105)
            return content[105];
        return '~';
    }
}
