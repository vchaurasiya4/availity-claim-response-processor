using AvailityClaimResponseProcessor.Core.Enums;

namespace AvailityClaimResponseProcessor.Core.Models;

public class ClaimResponse
{
    public int Id { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.PENDING;
    public EdiFileType FileType { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public string? RejectionReasonCode { get; set; }
    public string? RejectionReasonMessage { get; set; }
    public decimal? SubmittedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? PatientResponsibilityAmount { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ServiceDate { get; set; }
    public ICollection<ClaimError> Errors { get; set; } = new List<ClaimError>();
    public ClaimPayment? Payment { get; set; }
}

public class ClaimError
{
    public int Id { get; set; }
    public int ClaimResponseId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Segment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ClaimResponse? ClaimResponse { get; set; }
}

public class ClaimPayment
{
    public int Id { get; set; }
    public int ClaimResponseId { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public string PaymentStatusCode { get; set; } = string.Empty;
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PatientResponsibilityAmount { get; set; }
    public string? CheckNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ClaimResponse? ClaimResponse { get; set; }
}

public class EdiFileRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public EdiFileType FileType { get; set; }
    public string? InterchangeControlNumber { get; set; }
    public bool IsProcessed { get; set; }
    public bool HasErrors { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
