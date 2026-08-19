namespace AvailityClaimProcessor.Core.Models;

public class BenefitRecord
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // EBR or EBT
    public string? EligibilityStatus { get; set; }
    public string? PlanName { get; set; }
    public string? PlanId { get; set; }
    public string? GroupNumber { get; set; }
    public string? SubscriberId { get; set; }
    public string? CoverageType { get; set; }
    public decimal? DeductibleAmount { get; set; }
    public decimal? DeductibleMet { get; set; }
    public decimal? CopayAmount { get; set; }
    public decimal? CoinsurancePercent { get; set; }
    public decimal? OutOfPocketMax { get; set; }
    public decimal? OutOfPocketMet { get; set; }
    public DateTime? CoverageStartDate { get; set; }
    public DateTime? CoverageEndDate { get; set; }
    public string? RawContent { get; set; }
    public DateTime CreatedAt { get; set; }

    public int EdiFileId { get; set; }
    public EdiFile? EdiFile { get; set; }
}
