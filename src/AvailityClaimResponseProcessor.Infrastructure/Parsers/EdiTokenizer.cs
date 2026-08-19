using AvailityClaimResponseProcessor.Core.Models;

namespace AvailityClaimResponseProcessor.Infrastructure.Parsers;

/// <summary>
/// Utility for tokenizing raw EDI X12 content into segments and elements.
/// </summary>
public static class EdiTokenizer
{
    public static EdiDocument Tokenize(string content)
    {
        var doc = new EdiDocument { Raw = content };

        if (content.Length < 106)
            throw new InvalidOperationException("EDI content is too short to contain a valid ISA envelope.");

        // ISA segment defines delimiters
        doc.ElementSeparator = content[3];
        doc.ComponentSeparator = content[104];
        doc.SegmentTerminator = content[105];

        var rawSegments = content.Split(doc.SegmentTerminator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in rawSegments)
        {
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var elements = trimmed.Split(doc.ElementSeparator);
            doc.Segments.Add(new EdiSegment
            {
                Id = elements[0].Trim(),
                Elements = elements.Skip(1).ToArray()
            });
        }

        return doc;
    }
}
