namespace AvailityClaimResponseProcessor.Core.Enums;

public enum ClaimStatus
{
    PENDING,
    ACCEPTED,
    REJECTED,
    PROCESSED,
    ERROR
}

public enum EdiFileType
{
    TA1,
    Acknowledgment999,
    ClaimAcknowledgment277CA,
    RemittanceAdvice835,
    Unknown
}
