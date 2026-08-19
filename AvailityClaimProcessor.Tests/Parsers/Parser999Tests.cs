using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class Parser999Tests
{
    private readonly Parser999 _parser = new(new StatusMappingService());

    [Fact]
    public void CanParse_ReturnsTrueFor999EdiFile()
    {
        Assert.True(_parser.CanParse(".edi", "999_ack.edi"));
        Assert.False(_parser.CanParse(".edi", "835_remit.edi"));
    }

    [Fact]
    public void Parse_Accepted999_ReturnsAccepted()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000905*0*P*:~";
        var content = isa +
            "GS*FA*SUBMITTERID*AVAILITY*20210615*1230*905*X*005010X231A1~" +
            "ST*999*0001*005010X231A1~" +
            "AK1*HC*905*005010X222A2~" +
            "AK2*837*0001*005010X222A2~" +
            "AK5*A~" +
            "AK9*A*1*1*1~" +
            "SE*6*0001~" +
            "GE*1*905~" +
            "IEA*1*000000905~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.NotEmpty(result.ClaimResponses);
        Assert.All(result.ClaimResponses, c => Assert.Equal(ClaimStatus.ACCEPTED, c.Status));
    }

    [Fact]
    public void Parse_Rejected999_ReturnsRejected()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000906*0*P*:~";
        var content = isa +
            "GS*FA*SUBMITTERID*AVAILITY*20210615*1230*906*X*005010X231A1~" +
            "ST*999*0001*005010X231A1~" +
            "AK1*HC*906*005010X222A2~" +
            "AK2*837*0001*005010X222A2~" +
            "AK5*R*005~" +
            "AK9*R*1*1*0*005~" +
            "SE*6*0001~" +
            "GE*1*906~" +
            "IEA*1*000000906~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.NotEmpty(result.ClaimResponses);
        var rejected = result.ClaimResponses.First();
        Assert.Equal(ClaimStatus.REJECTED, rejected.Status);
    }
}
