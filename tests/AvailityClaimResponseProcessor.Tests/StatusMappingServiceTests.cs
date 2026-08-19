using AvailityClaimResponseProcessor.Core.Enums;
using AvailityClaimResponseProcessor.Infrastructure.Services;
using FluentAssertions;

namespace AvailityClaimResponseProcessor.Tests;

public class StatusMappingServiceTests
{
    private readonly StatusMappingService _sut = new();

    [Theory]
    [InlineData("A", ClaimStatus.ACCEPTED)]
    [InlineData("R", ClaimStatus.REJECTED)]
    [InlineData("E", ClaimStatus.ERROR)]
    [InlineData("P", ClaimStatus.ACCEPTED)]
    public void MapStatus_Ta1_ReturnsMappedStatus(string code, ClaimStatus expected)
    {
        var (status, _) = _sut.MapStatus(code, "TA1");
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData("A1", ClaimStatus.ACCEPTED)]
    [InlineData("A2", ClaimStatus.PROCESSED)]
    [InlineData("R1", ClaimStatus.REJECTED)]
    [InlineData("R2", ClaimStatus.REJECTED)]
    [InlineData("R3", ClaimStatus.REJECTED)]
    public void MapStatus_277CA_ReturnsMappedStatus(string code, ClaimStatus expected)
    {
        var (status, _) = _sut.MapStatus(code, "277CA");
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", ClaimStatus.PROCESSED)]
    [InlineData("4", ClaimStatus.REJECTED)]
    public void MapStatus_835_ReturnsMappedStatus(string code, ClaimStatus expected)
    {
        var (status, _) = _sut.MapStatus(code, "835");
        status.Should().Be(expected);
    }

    [Fact]
    public void MapStatus_UnknownCode_ReturnsPending()
    {
        var (status, message) = _sut.MapStatus("ZZ");
        status.Should().Be(ClaimStatus.PENDING);
        message.Should().Contain("ZZ");
    }

    [Fact]
    public void GetRejectionMessage_KnownCode_ReturnsMessage()
    {
        var msg = _sut.GetRejectionMessage("000");
        msg.Should().Be("No errors");
    }

    [Fact]
    public void GetRejectionMessage_UnknownCode_ReturnsFallback()
    {
        var msg = _sut.GetRejectionMessage("ZZZ");
        msg.Should().Contain("ZZZ");
    }

    [Fact]
    public void GetRejectionMessage_EmptyCode_ReturnsEmpty()
    {
        _sut.GetRejectionMessage("").Should().BeEmpty();
    }
}
