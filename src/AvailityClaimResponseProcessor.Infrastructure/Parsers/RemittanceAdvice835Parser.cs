using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace AvailityClaimResponseProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses 835 (Remittance Advice) files.
/// Extracts payment details from CLP segments.
/// </summary>
public class RemittanceAdvice835Parser : IEdiParser
{
    private readonly IStatusMappingService _statusMapper;
    private readonly ILogger<RemittanceAdvice835Parser> _logger;

    public RemittanceAdvice835Parser(IStatusMappingService statusMapper, ILogger<RemittanceAdvice835Parser> logger)
    {
        _statusMapper = statusMapper;
        _logger = logger;
    }

    public bool CanParse(string content) => content.Contains("ST*835");

    public async Task<IEnumerable<ClaimResponse>> ParseAsync(string content, string fileName)
    {
        var results = new List<ClaimResponse>();
        try
        {
            var doc = EdiTokenizer.Tokenize(content);

            ClaimResponse? currentClaim = null;
            string? pendingCheckNumber = null;
            DateTime? pendingPaymentDate = null;

            foreach (var segment in doc.Segments)
            {
                if (segment.Id == "BPR")
                {
                    // BPR payment date at element 15
                    var dateStr = segment.GetElement(15);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var payDate))
                        pendingPaymentDate = payDate;
                }
                else if (segment.Id == "TRN")
                {
                    // TRN*1*CheckNumber — store for upcoming CLPs
                    if (segment.GetElement(0) == "1")
                        pendingCheckNumber = segment.GetElement(1);
                    // Also apply to current claim if already processing
                    if (currentClaim?.Payment is not null)
                        currentClaim.Payment.CheckNumber = pendingCheckNumber;
                }
                else if (segment.Id == "CLP")
                {
                    // CLP*ClaimId*StatusCode*SubmittedAmt*PaidAmt*PatientResp*ClaimFilingIndicator~
                    // CLP02 status: 1=Processed as Primary, 2=Secondary, 3=Tertiary, 4=Denied
                    var claimId = segment.GetElement(0);
                    var statusCode = segment.GetElement(1); // 1=Processed, 2=Adjusted, 3=Denied, 4=Pending
                    var submittedStr = segment.GetElement(2);
                    var paidStr = segment.GetElement(3);
                    var patientRespStr = segment.GetElement(4);

                    decimal.TryParse(submittedStr, out var submitted);
                    decimal.TryParse(paidStr, out var paid);
                    decimal.TryParse(patientRespStr, out var patientResp);

                    var (status, message) = _statusMapper.MapStatus(statusCode, "835");

                    currentClaim = new ClaimResponse
                    {
                        ClaimId = claimId,
                        Status = status,
                        FileType = EdiFileType.RemittanceAdvice835,
                        SourceFileName = fileName,
                        StatusCode = statusCode,
                        StatusMessage = message,
                        SubmittedAmount = submitted,
                        PaidAmount = paid,
                        PatientResponsibilityAmount = patientResp,
                        ProcessedAt = DateTime.UtcNow,
                        Payment = new ClaimPayment
                        {
                            ClaimId = claimId,
                            PaymentStatusCode = statusCode,
                            SubmittedAmount = submitted,
                            ApprovedAmount = submitted,
                            PaidAmount = paid,
                            PatientResponsibilityAmount = patientResp,
                            CheckNumber = pendingCheckNumber,
                            PaymentDate = pendingPaymentDate,
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    results.Add(currentClaim);
                }
                else if (segment.Id == "CAS" && currentClaim?.Payment is not null)
                {
                    // CAS = Claim Adjustment; element[1] = adjustment amount
                    if (decimal.TryParse(segment.GetElement(2), out var adjustedAmt))
                        currentClaim.Payment.ApprovedAmount = currentClaim.Payment.SubmittedAmount - adjustedAmt;
                    currentClaim.ApprovedAmount = currentClaim.Payment.ApprovedAmount;
                }
                else if (segment.Id == "MOA" && currentClaim?.Payment is not null)
                {
                    // MOA contains remark codes for claim adjustments
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing 835 file: {FileName}", fileName);
        }

        return await Task.FromResult(results);
    }
}
