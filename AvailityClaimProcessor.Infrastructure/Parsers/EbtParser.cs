using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses .EBT (Electronic Benefit Transaction) files from Availity.
/// These files contain benefit transaction details, usage information, and claim references.
/// </summary>
public class EbtParser : IEdiParser
{
    private readonly IStatusMappingService _statusMapping;

    public EbtParser(IStatusMappingService statusMapping)
    {
        _statusMapping = statusMapping;
    }

    public string FileType => "EBT";

    public bool CanParse(string fileExtension, string fileName)
    {
        return fileExtension.Equals(".ebt", StringComparison.OrdinalIgnoreCase);
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            if (content.TrimStart().StartsWith("ISA"))
                ParseEdiStyle(content, ediFileId, result);
            else
                ParseProprietaryStyle(content, ediFileId, result);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    private void ParseEdiStyle(string content, int ediFileId, ParseResult result)
    {
        char segTerm = content.Length > 105 ? content[105] : '~';
        var segments = content.Split(segTerm, StringSplitOptions.RemoveEmptyEntries);

        var benefit = new BenefitRecord
        {
            FileType = "EBT",
            EdiFileId = ediFileId,
            CreatedAt = DateTime.UtcNow,
            RawContent = content.Length > 2000 ? content[..2000] : content
        };

        // Also track any linked claim responses (for claim references)
        ClaimResponse? linkedClaim = null;

        foreach (var seg in segments)
        {
            var trimmed = seg.Trim();
            var elements = trimmed.Split('*');
            var segId = elements[0];

            switch (segId)
            {
                case "NM1":
                    if (elements.Length > 1 && elements[1] == "IL" && elements.Length > 9)
                    {
                        var id = elements[9].TrimEnd('~');
                        if (!string.IsNullOrEmpty(id))
                            benefit.MemberId = id;
                    }
                    break;

                case "EB":
                    // EB*InfoCode*CoverageLevel*ServiceType
                    if (elements.Length > 3)
                        benefit.CoverageType = elements[3];
                    if (elements.Length > 1)
                        benefit.EligibilityStatus = elements[1] == "1" ? "Active" : "Inactive";
                    if (elements.Length > 9 && decimal.TryParse(elements[9].TrimEnd('~'), out var copay))
                        benefit.CopayAmount = copay;
                    break;

                case "DTP":
                    if (elements.Length > 3)
                    {
                        var qualifier = elements[1];
                        var dateStr = elements[3].TrimEnd('~');
                        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                            System.Globalization.DateTimeStyles.None, out var dt))
                        {
                            if (qualifier == "346") benefit.CoverageStartDate = dt;
                            else if (qualifier == "347") benefit.CoverageEndDate = dt;
                        }
                    }
                    break;

                case "REF":
                    if (elements.Length > 2)
                    {
                        var qualifier = elements[1];
                        var refValue = elements[2].TrimEnd('~');
                        switch (qualifier)
                        {
                            case "18": case "1L": benefit.GroupNumber = refValue; break;
                            case "6P": benefit.PlanId = refValue; break;
                            case "1W": benefit.SubscriberId = refValue; break;
                        }

                        // If a claim reference number — create linked claim
                        if (qualifier == "D9" || qualifier == "EA")
                        {
                            linkedClaim ??= new ClaimResponse
                            {
                                ClaimId = refValue,
                                Status = ClaimStatus.PENDING,
                                RawStatusCode = "EBT",
                                FileType = "EBT",
                                EdiFileId = ediFileId,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                        }
                    }
                    break;

                case "AMT":
                    // AMT*C1*Amount - benefit amount
                    if (elements.Length > 2 && decimal.TryParse(elements[2].TrimEnd('~'), out var txnAmt))
                    {
                        var qualifier = elements[1];
                        switch (qualifier)
                        {
                            case "C1": benefit.DeductibleAmount = txnAmt; break;
                            case "C2": benefit.DeductibleMet = txnAmt; break;
                            case "C3": benefit.OutOfPocketMax = txnAmt; break;
                            case "C4": benefit.OutOfPocketMet = txnAmt; break;
                            case "C5": benefit.CopayAmount = txnAmt; break;
                            case "C6": benefit.CoinsurancePercent = txnAmt; break;
                        }
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(benefit.MemberId))
            benefit.MemberId = $"EBT-{ediFileId}-{DateTime.UtcNow.Ticks}";

        result.BenefitRecords.Add(benefit);
        if (linkedClaim != null)
            result.ClaimResponses.Add(linkedClaim);
    }

    private static void ParseProprietaryStyle(string content, int ediFileId, ParseResult result)
    {
        var benefit = new BenefitRecord
        {
            FileType = "EBT",
            EdiFileId = ediFileId,
            CreatedAt = DateTime.UtcNow,
            RawContent = content.Length > 2000 ? content[..2000] : content
        };

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string? linkedClaimId = null;

        foreach (var line in lines)
        {
            if (!line.Contains('=')) continue;
            var idx = line.IndexOf('=');
            var key = line[..idx].Trim().ToUpperInvariant();
            var value = line[(idx + 1)..].Trim();

            switch (key)
            {
                case "MEMBERID": case "MEMBER_ID": benefit.MemberId = value; break;
                case "ELIGIBILITYSTATUS": case "ELIGIBILITY_STATUS": benefit.EligibilityStatus = value; break;
                case "PLANNAME": case "PLAN_NAME": benefit.PlanName = value; break;
                case "PLANID": case "PLAN_ID": benefit.PlanId = value; break;
                case "GROUPNUMBER": case "GROUP_NUMBER": benefit.GroupNumber = value; break;
                case "SUBSCRIBERID": case "SUBSCRIBER_ID": benefit.SubscriberId = value; break;
                case "COVERAGETYPE": case "COVERAGE_TYPE": benefit.CoverageType = value; break;
                case "DEDUCTIBLE":
                    if (decimal.TryParse(value, out var ded)) benefit.DeductibleAmount = ded; break;
                case "DEDUCTIBLEMET": case "DEDUCTIBLE_MET":
                    if (decimal.TryParse(value, out var dm)) benefit.DeductibleMet = dm; break;
                case "COPAY":
                    if (decimal.TryParse(value, out var cop)) benefit.CopayAmount = cop; break;
                case "COINSURANCE":
                    if (decimal.TryParse(value, out var coins)) benefit.CoinsurancePercent = coins; break;
                case "OUTOFPOCKETMAX": case "OUT_OF_POCKET_MAX":
                    if (decimal.TryParse(value, out var oop)) benefit.OutOfPocketMax = oop; break;
                case "OUTOFPOCKETMET": case "OUT_OF_POCKET_MET":
                    if (decimal.TryParse(value, out var oopMet)) benefit.OutOfPocketMet = oopMet; break;
                case "COVERAGESTARTDATE": case "COVERAGE_START_DATE":
                    if (DateTime.TryParse(value, out var cs)) benefit.CoverageStartDate = cs; break;
                case "COVERAGEENDDATE": case "COVERAGE_END_DATE":
                    if (DateTime.TryParse(value, out var ce)) benefit.CoverageEndDate = ce; break;
                case "CLAIMID": case "CLAIM_ID": linkedClaimId = value; break;
            }
        }

        if (string.IsNullOrEmpty(benefit.MemberId))
            benefit.MemberId = $"EBT-{ediFileId}-{DateTime.UtcNow.Ticks}";

        result.BenefitRecords.Add(benefit);

        if (!string.IsNullOrEmpty(linkedClaimId))
        {
            result.ClaimResponses.Add(new ClaimResponse
            {
                ClaimId = linkedClaimId,
                Status = ClaimStatus.PENDING,
                RawStatusCode = "EBT",
                FileType = "EBT",
                EdiFileId = ediFileId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
