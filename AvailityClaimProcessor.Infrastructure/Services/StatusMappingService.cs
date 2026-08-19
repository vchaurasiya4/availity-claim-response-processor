using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Infrastructure.Services;

public class StatusMappingService : IStatusMappingService
{
    // 277CA STC status codes
    private static readonly Dictionary<string, ClaimStatus> EdiStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A1", ClaimStatus.ACCEPTED },
        { "A2", ClaimStatus.PROCESSED },
        { "A3", ClaimStatus.ACCEPTED },
        { "A4", ClaimStatus.ACCEPTED },
        { "A6", ClaimStatus.ACCEPTED },
        { "A7", ClaimStatus.ACCEPTED },
        { "A8", ClaimStatus.PENDING },
        { "R1", ClaimStatus.REJECTED },
        { "R2", ClaimStatus.REJECTED },
        { "R3", ClaimStatus.REJECTED },
        { "R4", ClaimStatus.REJECTED },
        { "R5", ClaimStatus.REJECTED },
        { "R6", ClaimStatus.REJECTED },
        { "R7", ClaimStatus.REJECTED },
        { "R8", ClaimStatus.REJECTED },
        { "P1", ClaimStatus.PENDING },
        { "P2", ClaimStatus.PENDING },
        { "P3", ClaimStatus.PENDING },
        { "DR", ClaimStatus.REJECTED },
        { "WQ", ClaimStatus.PENDING },
    };

    // TA1 acknowledgment codes
    private static readonly Dictionary<string, ClaimStatus> Ta1StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A", ClaimStatus.ACCEPTED },
        { "E", ClaimStatus.REJECTED },
        { "R", ClaimStatus.REJECTED },
    };

    // 999 AK5/AK9 codes
    private static readonly Dictionary<string, ClaimStatus> AckStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A", ClaimStatus.ACCEPTED },
        { "E", ClaimStatus.REJECTED },
        { "R", ClaimStatus.REJECTED },
        { "M", ClaimStatus.ACCEPTED }, // Accepted with errors (warnings)
        { "W", ClaimStatus.ACCEPTED }, // Accepted with errors
        { "X", ClaimStatus.REJECTED },
    };

    private static readonly Dictionary<string, string> RejectionMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        { "1", "Deductible Amount" },
        { "2", "Coinsurance Amount" },
        { "3", "Co-payment Amount" },
        { "4", "Other Reduction" },
        { "5", "Prior Payer Paid Amount" },
        { "A1", "Claim/encounter not found" },
        { "A6", "Missing or Invalid Information" },
        { "A7", "Duplicate Claim/Service" },
        { "A8", "Claim Not Yet Processed" },
        { "R1", "Not Medically Necessary" },
        { "R2", "Not Covered" },
        { "R3", "Non-Covered Service" },
        { "R4", "No Authorization" },
        { "R5", "Limit Reached" },
        { "R6", "Invalid Information" },
        { "R7", "Deceased" },
        { "R8", "Not a Covered Benefit" },
        { "001", "Transaction Set Not Supported" },
        { "002", "Transaction Set Trailer Missing" },
        { "003", "Transaction Set Control Number in Header and Trailer Do Not Match" },
        { "004", "Number of Included Segments Does Not Match Actual Count" },
        { "005", "One or More Segments in Error" },
        { "006", "Missing or Invalid Transaction Set Identifier" },
        { "007", "Missing or Invalid Transaction Set Control Number" },
        { "008", "Authentication Key Name Unknown" },
        { "009", "Encryption Key Name Unknown" },
        { "010", "Requested Service (Encryption) Not Available" },
        { "011", "Unknown Security Recipient" },
        { "012", "Incorrect Message Length (Encryption Only)" },
        { "013", "Message Authentication Code Failed" },
        { "023", "ISA Segment Error" },
        { "024", "Invalid Interchange Content (e.g., Invalid GS Segment)" },
        { "025", "Duplicate Interchange Control Numbers" },
        { "026", "Invalid Data Element Separator" },
        { "027", "Invalid Component Element Separator" },
        { "028", "Invalid Date in ISA Segment" },
        { "029", "Invalid Time in ISA Segment" },
        { "030", "Invalid Standards Identifier in ISA Segment" },
        { "031", "Invalid Interchange Control Standards Identifier" },
        { "DR", "Duplicate Claim Received" },
        { "WQ", "Claim received and in pending status" },
        { "CO", "Contractual Obligations" },
        { "PR", "Patient Responsibility" },
        { "OA", "Other Adjustment" },
        { "PI", "Payer Initiated Reduction" },
    };

    public ClaimStatus MapEdiStatusToClaimStatus(string ediCode)
    {
        if (string.IsNullOrWhiteSpace(ediCode))
            return ClaimStatus.PENDING;

        // Try exact match first, then prefix match (e.g. "A1:19" → "A1")
        var code = ediCode.Split(':')[0].Trim();
        return EdiStatusMap.TryGetValue(code, out var status) ? status : ClaimStatus.PENDING;
    }

    public string GetRejectionMessage(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            return "Unknown rejection reason";
        return RejectionMessages.TryGetValue(reasonCode, out var msg) ? msg : $"Rejection code: {reasonCode}";
    }

    public ClaimStatus MapTa1StatusCode(string ta1Code)
    {
        if (string.IsNullOrWhiteSpace(ta1Code))
            return ClaimStatus.PENDING;
        return Ta1StatusMap.TryGetValue(ta1Code.Trim(), out var status) ? status : ClaimStatus.PENDING;
    }

    public ClaimStatus Map999StatusCode(string ak9Code)
    {
        if (string.IsNullOrWhiteSpace(ak9Code))
            return ClaimStatus.PENDING;
        return AckStatusMap.TryGetValue(ak9Code.Trim(), out var status) ? status : ClaimStatus.PENDING;
    }
}
