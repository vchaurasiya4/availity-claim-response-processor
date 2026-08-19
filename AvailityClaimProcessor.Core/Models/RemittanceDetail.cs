namespace AvailityClaimProcessor.Core.Models;

public class RemittanceDetail
{
    public int Id { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public string? CheckNumber { get; set; }
    public decimal? CheckAmount { get; set; }
    public DateTime? CheckDate { get; set; }
    public string? PayerName { get; set; }
    public string? PayeeName { get; set; }
    public string? ServiceLineInfo { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ClaimResponseId { get; set; }
    public ClaimResponse? ClaimResponse { get; set; }
}
