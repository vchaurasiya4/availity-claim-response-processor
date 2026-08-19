using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Core.Models;
using AvailityClaimResponseProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimResponseProcessor.Infrastructure.Data;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimDbContext _db;

    public ClaimRepository(ClaimDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimResponse?> GetByClaimIdAsync(string claimId) =>
        await _db.ClaimResponses
            .Include(x => x.Errors)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.ClaimId == claimId);

    public async Task<IEnumerable<ClaimResponse>> GetAllAsync() =>
        await _db.ClaimResponses
            .Include(x => x.Payment)
            .OrderByDescending(x => x.ProcessedAt)
            .ToListAsync();

    public async Task<ClaimPayment?> GetPaymentAsync(string claimId) =>
        await _db.ClaimPayments.FirstOrDefaultAsync(x => x.ClaimId == claimId);

    public async Task<IEnumerable<ClaimError>> GetErrorsAsync(string claimId)
    {
        var response = await _db.ClaimResponses
            .Include(x => x.Errors)
            .FirstOrDefaultAsync(x => x.ClaimId == claimId);
        return response?.Errors ?? Enumerable.Empty<ClaimError>();
    }

    public async Task UpsertAsync(ClaimResponse response)
    {
        var existing = await _db.ClaimResponses
            .Include(x => x.Errors)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.ClaimId == response.ClaimId);

        if (existing is null)
        {
            _db.ClaimResponses.Add(response);
        }
        else
        {
            existing.Status = response.Status;
            existing.FileType = response.FileType;
            existing.SourceFileName = response.SourceFileName;
            existing.StatusCode = response.StatusCode;
            existing.StatusMessage = response.StatusMessage;
            existing.RejectionReasonCode = response.RejectionReasonCode;
            existing.RejectionReasonMessage = response.RejectionReasonMessage;
            existing.SubmittedAmount = response.SubmittedAmount;
            existing.ApprovedAmount = response.ApprovedAmount;
            existing.PaidAmount = response.PaidAmount;
            existing.PatientResponsibilityAmount = response.PatientResponsibilityAmount;
            existing.ProcessedAt = response.ProcessedAt;
            existing.ServiceDate = response.ServiceDate;

            // Add new errors
            foreach (var error in response.Errors)
            {
                error.ClaimResponseId = existing.Id;
                _db.ClaimErrors.Add(error);
            }

            // Upsert payment
            if (response.Payment is not null)
            {
                if (existing.Payment is null)
                {
                    response.Payment.ClaimResponseId = existing.Id;
                    _db.ClaimPayments.Add(response.Payment);
                }
                else
                {
                    existing.Payment.PaymentStatusCode = response.Payment.PaymentStatusCode;
                    existing.Payment.SubmittedAmount = response.Payment.SubmittedAmount;
                    existing.Payment.ApprovedAmount = response.Payment.ApprovedAmount;
                    existing.Payment.PaidAmount = response.Payment.PaidAmount;
                    existing.Payment.PatientResponsibilityAmount = response.Payment.PatientResponsibilityAmount;
                    existing.Payment.CheckNumber = response.Payment.CheckNumber;
                    existing.Payment.PaymentDate = response.Payment.PaymentDate;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveEdiFileRecordAsync(EdiFileRecord record)
    {
        var existing = await _db.EdiFileRecords.FirstOrDefaultAsync(x => x.FileName == record.FileName);
        if (existing is null)
        {
            _db.EdiFileRecords.Add(record);
        }
        else
        {
            existing.IsProcessed = record.IsProcessed;
            existing.HasErrors = record.HasErrors;
            existing.ErrorMessage = record.ErrorMessage;
            existing.ProcessedAt = record.ProcessedAt;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsFileProcessedAsync(string fileName) =>
        await _db.EdiFileRecords.AnyAsync(x => x.FileName == fileName && x.IsProcessed);
}
