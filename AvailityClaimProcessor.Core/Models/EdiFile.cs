namespace AvailityClaimProcessor.Core.Models;

public class EdiFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // TA1, 999, 277CA, 835, EBR, EBT
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime DownloadedAt { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<ClaimResponse> ClaimResponses { get; set; } = new List<ClaimResponse>();
    public ICollection<BenefitRecord> BenefitRecords { get; set; } = new List<BenefitRecord>();
}
