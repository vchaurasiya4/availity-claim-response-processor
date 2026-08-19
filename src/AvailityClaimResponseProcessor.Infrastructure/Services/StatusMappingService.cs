using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;

namespace AvailityClaimResponseProcessor.Infrastructure.Services;

/// <summary>
/// Maps EDI status/reason codes to business-friendly ClaimStatus values and messages.
/// </summary>
public class StatusMappingService : IStatusMappingService
{
    // 277CA / TA1 / 999 status codes → business status
    private static readonly Dictionary<string, (ClaimStatus Status, string Message)> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // TA1 / 999 acknowledgment codes
        { "A",  (ClaimStatus.ACCEPTED,  "Interchange Accepted") },
        { "E",  (ClaimStatus.ERROR,     "Interchange Accepted With Errors") },
        { "R",  (ClaimStatus.REJECTED,  "Interchange Rejected") },
        // 999 functional group
        { "P",  (ClaimStatus.ACCEPTED,  "Partially Accepted") },
        // 277CA status codes
        { "A1", (ClaimStatus.ACCEPTED,  "Accepted for Further Processing") },
        { "A2", (ClaimStatus.PROCESSED, "Accepted and Forwarded") },
        { "A3", (ClaimStatus.ACCEPTED,  "Accepted") },
        { "A4", (ClaimStatus.PENDING,   "Accepted – Pending") },
        { "A5", (ClaimStatus.ACCEPTED,  "Acknowledged") },
        { "A6", (ClaimStatus.ACCEPTED,  "Accepted – Pending Adjudication") },
        { "A7", (ClaimStatus.ACCEPTED,  "Accepted – Additional Information Needed") },
        { "A8", (ClaimStatus.ACCEPTED,  "Accepted – Pending Post-Payment Review") },
        { "R1", (ClaimStatus.REJECTED,  "Rejected – Claim not found") },
        { "R2", (ClaimStatus.REJECTED,  "Rejected – Duplicate Claim") },
        { "R3", (ClaimStatus.REJECTED,  "Rejected – Claim not on file") },
        { "R4", (ClaimStatus.REJECTED,  "Rejected – Invalid Claim Information") },
        { "R5", (ClaimStatus.REJECTED,  "Rejected – Service not covered") },
        { "R6", (ClaimStatus.REJECTED,  "Rejected – Claim Period Not Covered") },
        { "R7", (ClaimStatus.REJECTED,  "Rejected – Subscriber not found") },
        { "R8", (ClaimStatus.REJECTED,  "Rejected – Subscriber not eligible") },
        { "R9", (ClaimStatus.REJECTED,  "Rejected – Subscriber not eligible for date of service") },
        { "R10",(ClaimStatus.REJECTED,  "Rejected – Plan limitations apply") },
        // 835 CLP payment status codes
        { "1",  (ClaimStatus.PROCESSED, "Processed as Primary") },
        { "2",  (ClaimStatus.PROCESSED, "Processed as Secondary") },
        { "3",  (ClaimStatus.PROCESSED, "Processed as Tertiary") },
        { "4",  (ClaimStatus.REJECTED,  "Denied") },
        { "19", (ClaimStatus.PROCESSED, "Processed as Primary – Forwarded to Additional Payer") },
        { "20", (ClaimStatus.PROCESSED, "Processed as Secondary – Forwarded to Additional Payer") },
        { "21", (ClaimStatus.PROCESSED, "Processed as Tertiary – Forwarded to Additional Payer") },
        { "22", (ClaimStatus.REJECTED,  "Reversed – Payment") },
        { "23", (ClaimStatus.PROCESSED, "Not Paid – Patient Not Eligible") },
    };

    private static readonly Dictionary<string, string> RejectionReasonMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "000", "No errors" },
        { "001", "The Interchange Control Numbers in the header ISA 13 and trailer IEA02 do not match" },
        { "002", "This Standard as Noted in the Control Standards Identifier is Not Supported" },
        { "003", "This Version of the Controls is Not Supported" },
        { "004", "The Segment Terminator is Invalid" },
        { "005", "Invalid Interchange ID Qualifier for Sender" },
        { "006", "Invalid Interchange Sender ID" },
        { "007", "Invalid Interchange ID Qualifier for Receiver" },
        { "008", "Invalid Interchange Receiver ID" },
        { "009", "Unknown Interchange Receiver ID" },
        { "010", "Invalid Authorization Information Qualifier Value" },
        { "011", "Invalid Authorization Information Value" },
        { "012", "Invalid Security Information Qualifier Value" },
        { "013", "Invalid Security Information Value" },
        { "014", "Invalid Interchange Date Value" },
        { "015", "Invalid Interchange Time Value" },
        { "016", "Invalid Interchange Standards Identifier Value" },
        { "017", "Invalid Interchange Version ID Value" },
        { "018", "Invalid Interchange Control Number Value" },
        { "019", "Invalid Acknowledgment Requested Value" },
        { "020", "Invalid Test Indicator Value" },
        { "021", "Invalid Number of Included Groups Value" },
        { "022", "Invalid Control Structure" },
        { "023", "Improper (Premature) End-of-File" },
        { "024", "Invalid Interchange Content" },
        { "025", "Duplicate Interchange Control Numbers" },
        { "026", "Invalid Data Element Separator" },
        { "027", "Invalid Component Element Separator" },
        { "I6",  "Invalid Group Control Number" },
        { "I10", "Authentication Key Name Unknown" },
        { "I11", "Encryption Key Name Unknown" },
        { "I12", "Requested Service (Authentication or Encrypted) Not Available" },
        { "I13", "Unknown Security Recipient" },
        { "I14", "Unknown Security Originator" },
        { "I15", "Relations Among IK3, IK4, and IK5 Are Not Consistent" },
        { "I16", "Unknown Interchange Receiver" },
        // 999 / 277CA reason codes
        { "1",   "Transaction Set Not Supported" },
        { "2",   "Transaction Set Trailer Missing" },
        { "3",   "Transaction Set Control Number in Header and Trailer Do Not Match" },
        { "4",   "Number of Included Segments Does Not Match Actual Count" },
        { "5",   "One or More Segments in Error" },
        { "6",   "Missing or Invalid Transaction Set Identifier" },
        { "7",   "Missing or Invalid Transaction Set Control Number" },
        { "8",   "Authentication Key Name Unknown" },
        { "9",   "Encryption Key Name Unknown" },
        { "10",  "Requested Service (Authentication or Encrypted) Not Available" },
        { "11",  "Unknown Security Recipient" },
        { "12",  "Unknown Security Originator" },
        { "13",  "Transaction Set Not in Agreement with the Controlling Table" },
        { "14",  "Unique Interchange Reference Constraint Violation" },
        { "15",  "Invalid Interchange Date Value" },
        { "16",  "Invalid Interchange Time Value" },
        { "19",  "STC Status code qualifier: Accepted for Further Processing" },
        { "20",  "Rejected – Duplicate Claim" },
    };

    public (ClaimStatus Status, string Message) MapStatus(string statusCode, string? fileType = null)
    {
        if (StatusMap.TryGetValue(statusCode, out var result))
            return result;

        return (ClaimStatus.PENDING, $"Unknown status code: {statusCode}");
    }

    public string GetRejectionMessage(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode)) return string.Empty;
        return RejectionReasonMap.TryGetValue(reasonCode, out var msg) ? msg : $"Reason code: {reasonCode}";
    }
}
