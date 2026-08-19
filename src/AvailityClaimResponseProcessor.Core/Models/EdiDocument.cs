namespace AvailityClaimResponseProcessor.Core.Models;

public class EdiSegment
{
    public string Id { get; set; } = string.Empty;
    public string[] Elements { get; set; } = Array.Empty<string>();

    public string GetElement(int index, string defaultValue = "") =>
        index < Elements.Length ? Elements[index] : defaultValue;
}

public class EdiDocument
{
    public List<EdiSegment> Segments { get; set; } = new();
    public string Raw { get; set; } = string.Empty;
    public char SegmentTerminator { get; set; } = '~';
    public char ElementSeparator { get; set; } = '*';
    public char ComponentSeparator { get; set; } = ':';
}
