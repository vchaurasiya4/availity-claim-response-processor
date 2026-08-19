using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Infrastructure.Parsers;
using AvailityClaimResponseProcessor.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AvailityClaimResponseProcessor.Tests;

public class EdiParserTests
{
    private readonly StatusMappingService _mapper = new();

    // Minimal valid ISA header (106 chars up to and including '~')
    // Sender/Receiver IDs must each be exactly 15 chars; component separator at pos 104, terminator at pos 105
    private static string BuildIsa(string body) =>
        "ISA*00*          *00*          *ZZ*SUBMITTERID    *ZZ*AVAILITY       *210615*1230*^*00501*000000905*0*P*:~\r\n" + body;

    // ──────────────────────────────────────────── EdiTokenizer ───────────────

    [Fact]
    public void EdiTokenizer_ParsesSegments_Correctly()
    {
        var content = BuildIsa("GS*FA*SENDER*RECEIVER*20210615*1230*1*X*005010X231A1~\r\nST*999*0001~\r\nAK1*FA*1~\r\nAK9*A*1*1*1~\r\nSE*3*0001~\r\nGE*1*1~\r\nIEA*1*000000905~");
        var doc = EdiTokenizer.Tokenize(content);
        doc.Segments.Should().Contain(s => s.Id == "ISA");
        doc.Segments.Should().Contain(s => s.Id == "AK9");
        doc.ElementSeparator.Should().Be('*');
        doc.SegmentTerminator.Should().Be('~');
        doc.ComponentSeparator.Should().Be(':');
    }

    [Fact]
    public void EdiTokenizer_ThrowsOnShortContent()
    {
        var act = () => EdiTokenizer.Tokenize("ISA*short");
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────── 999 Parser ─────────────────

    [Fact]
    public async Task Acknowledgment999Parser_AcceptedFile_ReturnsAcceptedStatus()
    {
        var content = BuildIsa(
            "GS*FA*SENDER*RECEIVER*20210615*1230*1*X*005010X231A1~\r\n" +
            "ST*999*0001~\r\n" +
            "AK1*FA*1~\r\n" +
            "AK9*A*1*1*1~\r\n" +
            "SE*3*0001~\r\n" +
            "GE*1*1~\r\n" +
            "IEA*1*000000905~");

        var parser = new Acknowledgment999Parser(_mapper, NullLogger<Acknowledgment999Parser>.Instance);
        parser.CanParse(content).Should().BeTrue();

        var results = (await parser.ParseAsync(content, "test_999.edi")).ToList();
        results.Should().HaveCount(1);
        results[0].Status.Should().Be(ClaimStatus.ACCEPTED);
        results[0].StatusCode.Should().Be("A");
        results[0].FileType.Should().Be(EdiFileType.Acknowledgment999);
    }

    [Fact]
    public async Task Acknowledgment999Parser_RejectedFile_ReturnsRejectedStatus()
    {
        var content = BuildIsa(
            "GS*FA*SENDER*RECEIVER*20210615*1230*1*X*005010X231A1~\r\n" +
            "ST*999*0001~\r\n" +
            "AK1*FA*1~\r\n" +
            "AK9*R*5~\r\n" +
            "SE*3*0001~\r\n" +
            "GE*1*1~\r\n" +
            "IEA*1*000000905~");

        var parser = new Acknowledgment999Parser(_mapper, NullLogger<Acknowledgment999Parser>.Instance);
        var results = (await parser.ParseAsync(content, "test_999.edi")).ToList();
        results.Should().HaveCount(1);
        results[0].Status.Should().Be(ClaimStatus.REJECTED);
    }

    // ──────────────────────────────────────────── 277CA Parser ───────────────

    [Fact]
    public async Task ClaimAcknowledgment277CaParser_ParsesClaims()
    {
        var content = BuildIsa(
            "GS*HN*SENDER*RECEIVER*20210615*1230*1*X*005010X214~\r\n" +
            "ST*277*0001*005010X214~\r\n" +
            "CLM*CLAIMID001*1000~\r\n" +
            "STC*A1:19*20210615*WQ~\r\n" +
            "CLM*CLAIMID002*500~\r\n" +
            "STC*R1:19*20210615*WQ~\r\n" +
            "SE*5*0001~\r\n" +
            "GE*1*1~\r\n" +
            "IEA*1*000000905~");

        var parser = new ClaimAcknowledgment277CaParser(_mapper, NullLogger<ClaimAcknowledgment277CaParser>.Instance);
        parser.CanParse(content).Should().BeTrue();

        var results = (await parser.ParseAsync(content, "test_277CA.edi")).ToList();
        results.Should().HaveCount(2);

        var accepted = results.First(r => r.ClaimId == "CLAIMID001");
        accepted.Status.Should().Be(ClaimStatus.ACCEPTED);
        accepted.SubmittedAmount.Should().Be(1000m);
        accepted.StatusCode.Should().Be("A1");
        accepted.ServiceDate.Should().Be(new DateTime(2021, 6, 15));

        var rejected = results.First(r => r.ClaimId == "CLAIMID002");
        rejected.Status.Should().Be(ClaimStatus.REJECTED);
    }

    // ──────────────────────────────────────────── 835 Parser ─────────────────

    [Fact]
    public async Task RemittanceAdvice835Parser_ParsesPayments()
    {
        var content = BuildIsa(
            "GS*HP*SENDER*RECEIVER*20210615*1230*1*X*005010X221A1~\r\n" +
            "ST*835*0001~\r\n" +
            "BPR*I*100*C*ACH*CCP*01*111111111*DA*222222222*1234567890**01*333333333*DA*444444444*20210615~\r\n" +
            "TRN*1*CHECK123*1234567890~\r\n" +
            "CLP*CLAIMID001*1*1000*900*100~\r\n" +
            "SE*5*0001~\r\n" +
            "GE*1*1~\r\n" +
            "IEA*1*000000905~");

        var parser = new RemittanceAdvice835Parser(_mapper, NullLogger<RemittanceAdvice835Parser>.Instance);
        parser.CanParse(content).Should().BeTrue();

        var results = (await parser.ParseAsync(content, "test_835.edi")).ToList();
        results.Should().HaveCount(1);

        var claim = results[0];
        claim.ClaimId.Should().Be("CLAIMID001");
        claim.Status.Should().Be(ClaimStatus.PROCESSED);
        claim.SubmittedAmount.Should().Be(1000m);
        claim.PaidAmount.Should().Be(900m);
        claim.PatientResponsibilityAmount.Should().Be(100m);
        claim.Payment.Should().NotBeNull();
        claim.Payment!.CheckNumber.Should().Be("CHECK123");
        claim.FileType.Should().Be(EdiFileType.RemittanceAdvice835);
    }

    // ──────────────────────────────────────────── TA1 Parser ─────────────────

    [Fact]
    public async Task Ta1Parser_AcceptedInterchange_ReturnsAccepted()
    {
        var content = BuildIsa("TA1*000000905*210615*1230*A*000~\r\nIEA*0*000000905~");
        var parser = new Ta1Parser(_mapper, NullLogger<Ta1Parser>.Instance);
        parser.CanParse(content).Should().BeTrue();

        var results = (await parser.ParseAsync(content, "test_ta1.edi")).ToList();
        results.Should().HaveCount(1);
        results[0].Status.Should().Be(ClaimStatus.ACCEPTED);
        results[0].FileType.Should().Be(EdiFileType.TA1);
    }
}
