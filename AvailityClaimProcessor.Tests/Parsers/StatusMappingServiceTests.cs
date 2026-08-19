using AvailityClaimProcessor.Core.Models;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Xunit;

namespace AvailityClaimProcessor.Tests.Parsers;

public class StatusMappingServiceTests
{
    private readonly StatusMappingService _svc = new();

    [Theory]
    [InlineData("A1", ClaimStatus.ACCEPTED)]
    [InlineData("A2", ClaimStatus.PROCESSED)]
    [InlineData("R1", ClaimStatus.REJECTED)]
    [InlineData("R3", ClaimStatus.REJECTED)]
    [InlineData("P1", ClaimStatus.PENDING)]
    [InlineData("WQ", ClaimStatus.PENDING)]
    [InlineData("UNKNOWN", ClaimStatus.PENDING)]
    [InlineData("", ClaimStatus.PENDING)]
    public void MapEdiStatus_ReturnsExpected(string code, ClaimStatus expected)
    {
        Assert.Equal(expected, _svc.MapEdiStatusToClaimStatus(code));
    }

    [Theory]
    [InlineData("A", ClaimStatus.ACCEPTED)]
    [InlineData("E", ClaimStatus.REJECTED)]
    [InlineData("R", ClaimStatus.REJECTED)]
    [InlineData("X", ClaimStatus.PENDING)]
    public void MapTa1Status_ReturnsExpected(string code, ClaimStatus expected)
    {
        Assert.Equal(expected, _svc.MapTa1StatusCode(code));
    }

    [Theory]
    [InlineData("A", ClaimStatus.ACCEPTED)]
    [InlineData("R", ClaimStatus.REJECTED)]
    [InlineData("M", ClaimStatus.ACCEPTED)]
    [InlineData("X", ClaimStatus.REJECTED)]
    public void Map999Status_ReturnsExpected(string code, ClaimStatus expected)
    {
        Assert.Equal(expected, _svc.Map999StatusCode(code));
    }

    [Fact]
    public void GetRejectionMessage_ReturnsKnownMessage()
    {
        var msg = _svc.GetRejectionMessage("R1");
        Assert.Equal("Not Medically Necessary", msg);
    }

    [Fact]
    public void GetRejectionMessage_UnknownCodeReturnsDefault()
    {
        var msg = _svc.GetRejectionMessage("ZZZZ");
        Assert.Contains("ZZZZ", msg);
    }
}
