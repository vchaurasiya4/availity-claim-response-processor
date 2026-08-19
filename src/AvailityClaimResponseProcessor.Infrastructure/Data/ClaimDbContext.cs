using AvailityClaimResponseProcessor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimResponseProcessor.Infrastructure.Data;

public class ClaimDbContext : DbContext
{
    public ClaimDbContext(DbContextOptions<ClaimDbContext> options) : base(options) { }

    public DbSet<ClaimResponse> ClaimResponses => Set<ClaimResponse>();
    public DbSet<ClaimError> ClaimErrors => Set<ClaimError>();
    public DbSet<ClaimPayment> ClaimPayments => Set<ClaimPayment>();
    public DbSet<EdiFileRecord> EdiFileRecords => Set<EdiFileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClaimResponse>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClaimId);
            e.Property(x => x.ClaimId).IsRequired().HasMaxLength(50);
            e.Property(x => x.SourceFileName).HasMaxLength(255);
            e.Property(x => x.StatusCode).HasMaxLength(20);
            e.Property(x => x.RejectionReasonCode).HasMaxLength(20);
            e.Property(x => x.SubmittedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ApprovedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PatientResponsibilityAmount).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Errors).WithOne(x => x.ClaimResponse).HasForeignKey(x => x.ClaimResponseId);
            e.HasOne(x => x.Payment).WithOne(x => x.ClaimResponse).HasForeignKey<ClaimPayment>(x => x.ClaimResponseId);
        });

        modelBuilder.Entity<ClaimError>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ErrorCode).HasMaxLength(20);
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.Property(x => x.Segment).HasMaxLength(10);
        });

        modelBuilder.Entity<ClaimPayment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimId).IsRequired().HasMaxLength(50);
            e.Property(x => x.PaymentStatusCode).HasMaxLength(10);
            e.Property(x => x.SubmittedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ApprovedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PatientResponsibilityAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CheckNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<EdiFileRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FileName).IsUnique();
            e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.InterchangeControlNumber).HasMaxLength(20);
        });
    }
}
