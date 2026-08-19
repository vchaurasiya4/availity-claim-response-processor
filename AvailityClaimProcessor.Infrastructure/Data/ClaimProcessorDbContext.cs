using AvailityClaimProcessor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AvailityClaimProcessor.Infrastructure.Data;

public class ClaimProcessorDbContext : DbContext
{
    public ClaimProcessorDbContext(DbContextOptions<ClaimProcessorDbContext> options) : base(options) { }

    public DbSet<EdiFile> EdiFiles => Set<EdiFile>();
    public DbSet<ClaimResponse> ClaimResponses => Set<ClaimResponse>();
    public DbSet<RemittanceDetail> RemittanceDetails => Set<RemittanceDetail>();
    public DbSet<ProcessingError> ProcessingErrors => Set<ProcessingError>();
    public DbSet<BenefitRecord> BenefitRecords => Set<BenefitRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EdiFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(10).IsRequired();
            e.HasMany(x => x.ClaimResponses).WithOne(x => x.EdiFile).HasForeignKey(x => x.EdiFileId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.BenefitRecords).WithOne(x => x.EdiFile).HasForeignKey(x => x.EdiFileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimResponse>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimId).HasMaxLength(50).IsRequired();
            e.Property(x => x.RawStatusCode).HasMaxLength(10);
            e.Property(x => x.FileType).HasMaxLength(10);
            e.Property(x => x.SubmittedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ApprovedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PatientResponsibility).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.ProcessingErrors).WithOne(x => x.ClaimResponse).HasForeignKey(x => x.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.RemittanceDetails).WithOne(x => x.ClaimResponse).HasForeignKey(x => x.ClaimResponseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RemittanceDetail>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimId).HasMaxLength(50).IsRequired();
            e.Property(x => x.CheckAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ProcessingError>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ErrorCode).HasMaxLength(50);
            e.Property(x => x.Segment).HasMaxLength(10);
        });

        modelBuilder.Entity<BenefitRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MemberId).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(10);
            e.Property(x => x.DeductibleAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.DeductibleMet).HasColumnType("decimal(18,2)");
            e.Property(x => x.CopayAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CoinsurancePercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.OutOfPocketMax).HasColumnType("decimal(18,2)");
            e.Property(x => x.OutOfPocketMet).HasColumnType("decimal(18,2)");
        });
    }
}
