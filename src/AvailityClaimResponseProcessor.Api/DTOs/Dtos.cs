using AvailityClaimResponseProcessor.Core.Enums;

namespace AvailityClaimResponseProcessor.Api.DTOs;

public record ClaimStatusDto(
    string ClaimId,
    ClaimStatus Status,
    string? StatusCode,
    string? StatusMessage,
    string? RejectionReasonCode,
    string? RejectionReasonMessage,
    EdiFileType FileType,
    string SourceFileName,
    decimal? SubmittedAmount,
    decimal? ApprovedAmount,
    decimal? PaidAmount,
    decimal? PatientResponsibilityAmount,
    DateTime ProcessedAt,
    DateTime? ServiceDate
);

public record RemittanceDto(
    string ClaimId,
    string PaymentStatusCode,
    decimal SubmittedAmount,
    decimal ApprovedAmount,
    decimal PaidAmount,
    decimal PatientResponsibilityAmount,
    string? CheckNumber,
    DateTime? PaymentDate
);

public record ClaimErrorDto(
    int Id,
    string ErrorCode,
    string ErrorMessage,
    string? Segment,
    DateTime CreatedAt
);

public record HealthDto(string Status, DateTime Timestamp, string Version);
