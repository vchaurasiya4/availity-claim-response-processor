namespace AvailityClaimProcessor.Core.Models;

public class ClaimResponse
{
    public int Id { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; }
    public string RawStatusCode { get; set; } = string.Empty; // A1, A2, R1, R2, R3, etc.
    public string FileType { get; set; } = string.Empty; // TA1, 999, 277CA, 835
    public string? RejectReasonCode { get; set; }
    public string? RejectReasonMessage { get; set; }
    public decimal? SubmittedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? PatientResponsibility { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int EdiFileId { get; set; }
    public EdiFile? EdiFile { get; set; }

    public ICollection<ProcessingError> ProcessingErrors { get; set; } = new List<ProcessingError>();
    public ICollection<RemittanceDetail> RemittanceDetails { get; set; } = new List<RemittanceDetail>();
}
