using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class EbrParserTests
{
    private readonly EbrParser _parser = new();

    [Fact]
    public void CanParse_ReturnsTrueForEbrExtension()
    {
        Assert.True(_parser.CanParse(".ebr", "response.ebr"));
        Assert.False(_parser.CanParse(".edi", "response.edi"));
        Assert.False(_parser.CanParse(".ebt", "response.ebt"));
    }

    [Fact]
    public void Parse_ProprietaryFormat_ExtractsBenefitData()
    {
        var content = @"MEMBERID=M12345
ELIGIBILITYSTATUS=Active
PLANNAME=Blue Shield PPO
PLANID=BS001
GROUPNUMBER=GRP100
COVERAGETYPE=Medical
DEDUCTIBLE=1500.00
DEDUCTIBLEMET=750.00
COPAY=25.00
COINSURANCE=20.00
OUTOFPOCKETMAX=5000.00
OUTOFPOCKETMET=1200.00
COVERAGESTARTDATE=2021-01-01
COVERAGEENDDATE=2021-12-31";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.BenefitRecords);
        var benefit = result.BenefitRecords[0];
        Assert.Equal("M12345", benefit.MemberId);
        Assert.Equal("Active", benefit.EligibilityStatus);
        Assert.Equal("Blue Shield PPO", benefit.PlanName);
        Assert.Equal(1500m, benefit.DeductibleAmount);
        Assert.Equal(750m, benefit.DeductibleMet);
        Assert.Equal(25m, benefit.CopayAmount);
        Assert.Equal(20m, benefit.CoinsurancePercent);
        Assert.Equal(5000m, benefit.OutOfPocketMax);
        Assert.Equal(new DateTime(2021, 1, 1), benefit.CoverageStartDate);
    }

    [Fact]
    public void Parse_EdiStyleFormat_ExtractsMemberId()
    {
        var isa = "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000271*0*P*:~";
        var content = isa +
            "GS*HB*SUBMITTERID*AVAILITY*20210615*1230*271*X*005010X279A1~" +
            "ST*271*0001*005010X279A1~" +
            "BHT*0022*11*10001234*20210615*1230~" +
            "NM1*IL*1*DOE*JOHN****MI*M98765~" +
            "INS*Y*18*001*AI*A~" +
            "DTP*346*D8*20210101~" +
            "DTP*347*D8*20211231~" +
            "EB*1*FAM*30*HM~" +
            "SE*8*0001~";

        var result = _parser.Parse(content, 1);

        Assert.True(result.Success);
        Assert.Single(result.BenefitRecords);
        Assert.Equal("M98765", result.BenefitRecords[0].MemberId);
        Assert.Equal("Active", result.BenefitRecords[0].EligibilityStatus);
    }
}
