using AvailityClaimProcessor.Core.Models;

namespace AvailityClaimProcessor.Core.Interfaces;

public interface IEdiParser
{
    string FileType { get; }
    bool CanParse(string fileExtension, string fileName);
    ParseResult Parse(string content, int ediFileId);
}
