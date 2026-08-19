using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class Parser835Tests
{
    private readonly Parser835 _parser = new(new StatusMappingService());

    [Fact]
    public void CanParse_ReturnsTrueFor835EdiFile()
    {
        Assert.True(_parser.CanParse(".edi", "835_remit.edi"));
        Assert.False(_parser.CanParse(".edi", "277ca_response.edi"));
    }

    [Fact]
    public void Parse_835WithClpSegment_ExtractsPaymentInfo()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000835*0*P*:~";
        var content = isa +
            "GS*HP*AVAILITY*SUBMITTERID*20210615*1230*835*X*005010X221A1~" +
            "ST*835*0001*005010X221A1~" +
            "BPR*I*900*C*ACH*CCP*01*999999999*DA*123456789*1234567890**01*999999998*DA*987654321*20210615~" +
            "TRN*1*CHK12345*1234567890~" +
            "N1*PR*AVAILITY PAYER~" +
            "N1*PE*PROVIDER CLINIC~" +
            "CLP*CLAIMID001*1*1000*900*100*MA*12345~" +
            "SE*8*0001~" +
            "GE*1*835~" +
            "IEA*1*000000835~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.ClaimResponses);
        var claim = result.ClaimResponses[0];
        Assert.Equal("CLAIMID001", claim.ClaimId);
        Assert.Equal(ClaimStatus.PROCESSED, claim.Status);
        Assert.Equal(1000m, claim.SubmittedAmount);
        Assert.Equal(900m, claim.PaidAmount);
        Assert.Equal(100m, claim.PatientResponsibility);
        Assert.Single(claim.RemittanceDetails);
        Assert.Equal("CHK12345", claim.RemittanceDetails.First().CheckNumber);
        Assert.Equal(900m, claim.RemittanceDetails.First().CheckAmount);
    }
}
