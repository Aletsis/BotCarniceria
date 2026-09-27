using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Services;

public class DateTimeProviderTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IConfiguracionRepository> _mockConfigRepo;

    public DateTimeProviderTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockConfigRepo = new Mock<IConfiguracionRepository>();
        _mockUnitOfWork.Setup(u => u.Settings).Returns(_mockConfigRepo.Object);
    }

    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var provider = new DateTimeProvider(_mockUnitOfWork.Object);

        // Act
        var utcNow = provider.UtcNow;

        // Assert
        utcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Now_AndLocalProperties_WithUtcTimezone_ShouldMatchUtcNow()
    {
        // Arrange
        _mockConfigRepo.Setup(c => c.GetValorAsync(ConfigurationKeys.System.TimeZoneId))
            .ReturnsAsync("UTC");

        var provider = new DateTimeProvider(_mockUnitOfWork.Object);

        // Act
        var now = provider.Now;
        var localTimeOfDay = provider.LocalTimeOfDay;
        var localToday = provider.LocalToday;

        // Assert
        now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        localToday.Should().Be(now.Date);
        localTimeOfDay.Hours.Should().Be(now.Hour);
    }

    [Fact]
    public void ToLocalTime_AndToUtcTime_ShouldConvertAccurately()
    {
        // Arrange
        _mockConfigRepo.Setup(c => c.GetValorAsync(ConfigurationKeys.System.TimeZoneId))
            .ReturnsAsync("UTC");

        var provider = new DateTimeProvider(_mockUnitOfWork.Object);
        var testUtc = new DateTime(2026, 9, 27, 15, 30, 0, DateTimeKind.Utc);

        // Act
        var local = provider.ToLocalTime(testUtc);
        var backToUtc = provider.ToUtcTime(local);

        // Assert
        local.Should().Be(testUtc);
        backToUtc.Should().Be(testUtc);
    }

    [Fact]
    public void GetTimeZone_InvalidTimeZoneId_ShouldFallbackToUtc()
    {
        // Arrange
        _mockConfigRepo.Setup(c => c.GetValorAsync(ConfigurationKeys.System.TimeZoneId))
            .ReturnsAsync("Invalid_TimeZone_Id_12345");

        var provider = new DateTimeProvider(_mockUnitOfWork.Object);
        var testUtc = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var local = provider.ToLocalTime(testUtc);

        // Assert (fallback to UTC)
        local.Should().Be(testUtc);
    }

    [Fact]
    public void GetTimeZone_ShouldCacheTimeZoneForSubsequentCalls()
    {
        // Arrange
        _mockConfigRepo.Setup(c => c.GetValorAsync(ConfigurationKeys.System.TimeZoneId))
            .ReturnsAsync("UTC");

        var provider = new DateTimeProvider(_mockUnitOfWork.Object);

        // Act - call twice
        var first = provider.Now;
        var second = provider.Now;

        // Assert - GetValorAsync should only be called once because of 5-minute caching
        _mockConfigRepo.Verify(c => c.GetValorAsync(ConfigurationKeys.System.TimeZoneId), Times.Once);
    }
}
