using AvailityClaimResponseProcessor.Core.Enums;

namespace AvailityClaimResponseProcessor.Core.Interfaces;

public interface IStatusMappingService
{
    (ClaimStatus Status, string Message) MapStatus(string statusCode, string? fileType = null);
    string GetRejectionMessage(string reasonCode);
}
