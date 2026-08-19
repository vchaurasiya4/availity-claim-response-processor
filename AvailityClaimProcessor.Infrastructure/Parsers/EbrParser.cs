using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Parsers;

/// <summary>
/// Parses .EBR (Electronic Benefit Response) files from Availity.
/// These files contain eligibility and benefit information using EDI 271-like structure
/// or a proprietary Availity key=value / delimited format.
/// </summary>
public class EbrParser : IEdiParser
{
    public string FileType => "EBR";

    public bool CanParse(string fileExtension, string fileName)
    {
        return fileExtension.Equals(".ebr", StringComparison.OrdinalIgnoreCase);
    }

    public ParseResult Parse(string content, int ediFileId)
    {
        var result = new ParseResult { FileType = FileType };
        try
        {
            // EBR files may be EDI 271 style or Availity proprietary format
            // Detect format: if it starts with ISA treat as EDI, otherwise try delimited
            if (content.TrimStart().StartsWith("ISA"))
                ParseEdi271Style(content, ediFileId, result);
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

    private static void ParseEdi271Style(string content, int ediFileId, ParseResult result)
    {
        char segTerm = content.Length > 105 ? content[105] : '~';
        var segments = content.Split(segTerm, StringSplitOptions.RemoveEmptyEntries);

        var benefit = new BenefitRecord
        {
            FileType = "EBR",
            EdiFileId = ediFileId,
            CreatedAt = DateTime.UtcNow,
            RawContent = content.Length > 2000 ? content[..2000] : content
        };

        foreach (var seg in segments)
        {
            var trimmed = seg.Trim();
            var elements = trimmed.Split('*');
            var segId = elements[0];

            switch (segId)
            {
                case "NM1":
                    // NM1*IL*1*LastName*FirstName - Insured/Subscriber
                    if (elements.Length > 1 && (elements[1] == "IL" || elements[1] == "1P"))
                    {
                        var lastName = elements.Length > 3 ? elements[3] : string.Empty;
                        var firstName = elements.Length > 4 ? elements[4] : string.Empty;
                        var id = elements.Length > 9 ? elements[9].TrimEnd('~') : string.Empty;
                        if (!string.IsNullOrEmpty(id))
                            benefit.MemberId = id;
                        else if (!string.IsNullOrEmpty(lastName))
                            benefit.MemberId = $"{lastName},{firstName}";
                    }
                    break;

                case "INS":
                    // INS*Y*18*001*...*A - Subscriber info; element 5 = eligibility status
                    if (elements.Length > 5)
                        benefit.EligibilityStatus = elements[5].TrimEnd('~') == "A" ? "Active" : "Inactive";
                    break;

                case "DTP":
                    // DTP*346*D8*CCYYMMDD - Coverage start; DTP*347*D8*CCYYMMDD - Coverage end
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

                case "EB":
                    // EB*1*FAM*30*...*...*...*...*CopayAmount - Benefit information
                    if (elements.Length > 1)
                    {
                        var eligCode = elements[1];
                        benefit.EligibilityStatus ??= eligCode == "1" ? "Active" : "Inactive";
                    }
                    if (elements.Length > 3)
                        benefit.CoverageType = elements[3];
                    if (elements.Length > 9 && decimal.TryParse(elements[9].TrimEnd('~'), out var copay))
                        benefit.CopayAmount = copay;
                    break;

                case "MSG":
                    break;

                case "REF":
                    // REF*18*GroupNumber or REF*1L*GroupNumber
                    if (elements.Length > 2 && (elements[1] == "18" || elements[1] == "1L"))
                        benefit.GroupNumber = elements[2].TrimEnd('~');
                    if (elements.Length > 2 && elements[1] == "6P")
                        benefit.PlanId = elements[2].TrimEnd('~');
                    break;

                case "III":
                    // Additional benefit info
                    break;
            }
        }

        if (string.IsNullOrEmpty(benefit.MemberId))
            benefit.MemberId = $"EBR-{ediFileId}-{DateTime.UtcNow.Ticks}";

        result.BenefitRecords.Add(benefit);
    }

    private static void ParseProprietaryStyle(string content, int ediFileId, ParseResult result)
    {
        // Availity proprietary EBR: key=value pairs, one per line, or pipe-delimited
        var benefit = new BenefitRecord
        {
            FileType = "EBR",
            EdiFileId = ediFileId,
            CreatedAt = DateTime.UtcNow,
            RawContent = content.Length > 2000 ? content[..2000] : content
        };

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains('='))
            {
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
                        if (decimal.TryParse(value, out var dedMet)) benefit.DeductibleMet = dedMet; break;
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
                }
            }
        }

        if (string.IsNullOrEmpty(benefit.MemberId))
            benefit.MemberId = $"EBR-{ediFileId}-{DateTime.UtcNow.Ticks}";

        result.BenefitRecords.Add(benefit);
    }
}
