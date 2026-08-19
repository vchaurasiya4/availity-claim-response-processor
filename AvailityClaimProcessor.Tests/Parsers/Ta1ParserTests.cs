using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class Ta1ParserTests
{
    private readonly Ta1Parser _parser = new(new StatusMappingService());

    [Fact]
    public void CanParse_ReturnsTrueForTa1EdiFile()
    {
        Assert.True(_parser.CanParse(".edi", "ta1_interchange.edi"));
        Assert.False(_parser.CanParse(".edi", "835_remit.edi"));
        Assert.False(_parser.CanParse(".ebr", "ta1_file.ebr"));
    }

    [Fact]
    public void Parse_AcceptedTa1_ReturnsAccepted()
    {
        // ISA is exactly 106 chars, ending with segment terminator at index 105
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000001*0*P*:~";
        var ta1 = "TA1*000000001*210615*1230*A*000~";
        var content = isa + ta1;

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.ClaimResponses);
        Assert.Equal(ClaimStatus.ACCEPTED, result.ClaimResponses[0].Status);
        Assert.Equal("A", result.ClaimResponses[0].RawStatusCode);
    }

    [Fact]
    public void Parse_RejectedTa1_ReturnsRejected()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000002*0*P*:~";
        var ta1 = "TA1*000000002*210615*1230*R*023~";
        var content = isa + ta1;

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.ClaimResponses);
        Assert.Equal(ClaimStatus.REJECTED, result.ClaimResponses[0].Status);
        Assert.Equal("023", result.ClaimResponses[0].RejectReasonCode);
        Assert.NotEmpty(result.ClaimResponses[0].ProcessingErrors);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsSuccessWithNoResults()
    {
        var result = _parser.Parse(string.Empty, 1);
        Assert.True(result.Success);
        Assert.Empty(result.ClaimResponses);
    }
}
