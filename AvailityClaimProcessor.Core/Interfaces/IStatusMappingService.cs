using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Core.Interfaces;

public interface IStatusMappingService
{
    ClaimStatus MapEdiStatusToClaimStatus(string ediCode);
    string GetRejectionMessage(string reasonCode);
    ClaimStatus MapTa1StatusCode(string ta1Code);
    ClaimStatus Map999StatusCode(string ak9Code);
}
