using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class EbtParserTests
{
    private readonly EbtParser _parser = new(new StatusMappingService());

    [Fact]
    public void CanParse_ReturnsTrueForEbtExtension()
    {
        Assert.True(_parser.CanParse(".ebt", "transaction.ebt"));
        Assert.False(_parser.CanParse(".edi", "transaction.edi"));
        Assert.False(_parser.CanParse(".ebr", "transaction.ebr"));
    }

    [Fact]
    public void Parse_ProprietaryFormat_ExtractsBenefitData()
    {
        var content = @"MEMBERID=T67890
ELIGIBILITYSTATUS=Active
PLANID=PLAN002
GROUPNUMBER=GRP200
SUBSCRIBERID=SUB001
COVERAGETYPE=Dental
DEDUCTIBLE=500.00
DEDUCTIBLEMET=200.00
COPAY=15.00
COVERAGESTARTDATE=2021-01-01
CLAIMID=CLM-EBT-001";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.BenefitRecords);
        var benefit = result.BenefitRecords[0];
        Assert.Equal("T67890", benefit.MemberId);
        Assert.Equal("Active", benefit.EligibilityStatus);
        Assert.Equal(500m, benefit.DeductibleAmount);
        Assert.Equal(15m, benefit.CopayAmount);

        // Linked claim reference
        Assert.Single(result.ClaimResponses);
        Assert.Equal("CLM-EBT-001", result.ClaimResponses[0].ClaimId);
    }

    [Fact]
    public void Parse_EdiStyleFormat_ExtractsMemberId()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000272*0*P*:~";
        var content = isa +
            "ST*271*0001*005010X279A1~" +
            "NM1*IL*1*SMITH*JANE****MI*EBT123~" +
            "EB*1*IND*30~" +
            "AMT*C1*1000~" +
            "AMT*C2*400~" +
            "AMT*C3*3000~" +
            "REF*1W*SUB999~" +
            "SE*7*0001~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.BenefitRecords);
        Assert.Equal("EBT123", result.BenefitRecords[0].MemberId);
        Assert.Equal(1000m, result.BenefitRecords[0].DeductibleAmount);
        Assert.Equal(400m, result.BenefitRecords[0].DeductibleMet);
        Assert.Equal(3000m, result.BenefitRecords[0].OutOfPocketMax);
    }
}
