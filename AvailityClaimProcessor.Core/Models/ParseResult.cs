namespace AvailityClaimProcessor.Core.Models;

public class ParseResult
{
    public bool Success { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<ClaimResponse> ClaimResponses { get; set; } = new();
    public List<BenefitRecord> BenefitRecords { get; set; } = new();
}
