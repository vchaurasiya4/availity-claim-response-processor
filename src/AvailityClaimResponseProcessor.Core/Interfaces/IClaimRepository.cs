using AvailityClaimResponseProcessor.Core.Models;

namespace AvailityClaimResponseProcessor.Core.Interfaces;

public interface IClaimRepository
{
    Task<ClaimResponse?> GetByClaimIdAsync(string claimId);
    Task<IEnumerable<ClaimResponse>> GetAllAsync();
    Task<ClaimPayment?> GetPaymentAsync(string claimId);
    Task<IEnumerable<ClaimError>> GetErrorsAsync(string claimId);
    Task UpsertAsync(ClaimResponse response);
    Task SaveEdiFileRecordAsync(EdiFileRecord record);
    Task<bool> IsFileProcessedAsync(string fileName);
}
