using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Core.Models;
using AvailityClaimResponseProcessor.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimResponseProcessor.Tests;

public class ClaimRepositoryTests
{
    private static ClaimDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ClaimDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ClaimDbContext(opts);
    }

    [Fact]
    public async Task UpsertAsync_NewClaim_SavesSuccessfully()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        var claim = new ClaimResponse { ClaimId = "CLM001", Status = ClaimStatus.ACCEPTED, SourceFileName = "test.edi" };

        await repo.UpsertAsync(claim);

        var saved = await repo.GetByClaimIdAsync("CLM001");
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(ClaimStatus.ACCEPTED);
    }

    [Fact]
    public async Task UpsertAsync_ExistingClaim_UpdatesStatus()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        await repo.UpsertAsync(new ClaimResponse { ClaimId = "CLM002", Status = ClaimStatus.PENDING, SourceFileName = "a.edi" });

        await repo.UpsertAsync(new ClaimResponse { ClaimId = "CLM002", Status = ClaimStatus.PROCESSED, SourceFileName = "b.edi" });

        var updated = await repo.GetByClaimIdAsync("CLM002");
        updated!.Status.Should().Be(ClaimStatus.PROCESSED);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSavedClaims()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        await repo.UpsertAsync(new ClaimResponse { ClaimId = "C1", SourceFileName = "f.edi" });
        await repo.UpsertAsync(new ClaimResponse { ClaimId = "C2", SourceFileName = "g.edi" });

        var all = (await repo.GetAllAsync()).ToList();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task IsFileProcessedAsync_ReturnsFalseForNewFile()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        (await repo.IsFileProcessedAsync("newfile.edi")).Should().BeFalse();
    }

    [Fact]
    public async Task IsFileProcessedAsync_ReturnsTrueAfterSaving()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        await repo.SaveEdiFileRecordAsync(new EdiFileRecord { FileName = "done.edi", IsProcessed = true });

        (await repo.IsFileProcessedAsync("done.edi")).Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentAsync_ReturnsPaymentForClaim()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        var claim = new ClaimResponse
        {
            ClaimId = "CLM003",
            SourceFileName = "835.edi",
            Payment = new ClaimPayment
            {
                ClaimId = "CLM003",
                PaymentStatusCode = "1",
                SubmittedAmount = 1000,
                ApprovedAmount = 900,
                PaidAmount = 900,
                PatientResponsibilityAmount = 100
            }
        };
        await repo.UpsertAsync(claim);

        var payment = await repo.GetPaymentAsync("CLM003");
        payment.Should().NotBeNull();
        payment!.PaidAmount.Should().Be(900);
    }

    [Fact]
    public async Task GetErrorsAsync_ReturnsErrorsForClaim()
    {
        await using var db = CreateDb();
        var repo = new ClaimRepository(db);
        var claim = new ClaimResponse
        {
            ClaimId = "CLM004",
            SourceFileName = "999.edi",
            Errors = new List<ClaimError>
            {
                new() { ErrorCode = "005", ErrorMessage = "One or More Segments in Error" }
            }
        };
        await repo.UpsertAsync(claim);

        var errors = (await repo.GetErrorsAsync("CLM004")).ToList();
        errors.Should().HaveCount(1);
        errors[0].ErrorCode.Should().Be("005");
    }
}
