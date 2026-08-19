namespace AvailityClaimProcessor.Core.Models;

public class ProcessingError
{
    public int Id { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public int ClaimResponseId { get; set; }
    public ClaimResponse? ClaimResponse { get; set; }
}
