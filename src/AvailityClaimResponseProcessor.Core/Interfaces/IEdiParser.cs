using AvailityClaimResponseProcessor.Core.Models;

namespace AvailityClaimResponseProcessor.Core.Interfaces;

public interface IEdiParser
{
    bool CanParse(string content);
    Task<IEnumerable<ClaimResponse>> ParseAsync(string content, string fileName);
}
