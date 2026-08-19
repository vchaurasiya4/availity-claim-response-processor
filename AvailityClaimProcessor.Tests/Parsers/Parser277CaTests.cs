using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class Parser277CaTests
{
    private readonly Parser277Ca _parser = new(new StatusMappingService());

    [Fact]
    public void CanParse_ReturnsTrueFor277EdiFile()
    {
        Assert.True(_parser.CanParse(".edi", "277ca_response.edi"));
        Assert.False(_parser.CanParse(".edi", "835_remit.edi"));
    }

    [Fact]
    public void Parse_AcceptedClaim_ReturnsAccepted()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000277*0*P*:~";
        var content = isa +
            "GS*HN*SUBMITTERID*AVAILITY*20210615*1230*277*X*005010X214~" +
            "ST*277*0001*005010X214~" +
            "CLM*CLAIM001*1000*~" +
            "DTP*472*D8*20210615~" +
            "STC*A1:19*20210615*WQ*1000~" +
            "SE*5*0001~" +
            "GE*1*277~" +
            "IEA*1*000000277~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.ClaimResponses);
        var claim = result.ClaimResponses[0];
        Assert.Equal("CLAIM001", claim.ClaimId);
        Assert.Equal(ClaimStatus.ACCEPTED, claim.Status);
        Assert.Equal("A1", claim.RawStatusCode);
    }

    [Fact]
    public void Parse_RejectedClaim_ReturnsRejected()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000278*0*P*:~";
        var content = isa +
            "GS*HN*SUBMITTERID*AVAILITY*20210615*1230*278*X*005010X214~" +
            "ST*277*0001*005010X214~" +
            "CLM*CLAIM002*500*~" +
            "STC*R3:45*20210615*WQ*500~" +
            "SE*4*0001~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.ClaimResponses);
        Assert.Equal(ClaimStatus.REJECTED, result.ClaimResponses[0].Status);
    }
}
