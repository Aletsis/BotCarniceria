using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Presentation.API.Services;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Services;

public class SessionTimeoutBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<SessionTimeoutBackgroundService>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public SessionTimeoutBackgroundServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<SessionTimeoutBackgroundService>>();
        _mockMediator = new Mock<IMediator>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IMediator)))
            .Returns(_mockMediator.Object);

        _mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    private class TestableSessionTimeoutBackgroundService : SessionTimeoutBackgroundService
    {
        public TestableSessionTimeoutBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SessionTimeoutBackgroundService> logger)
            : base(serviceProvider, logger) { }

        public Task CallExecuteAsync(CancellationToken token) => ExecuteAsync(token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyCancelled_ShouldExitGracefullyWithoutCheckingSessions()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var service = new TestableSessionTimeoutBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Act
        await service.CallExecuteAsync(cts.Token);

        // Assert
        _mockMediator.Verify(m => m.Send(It.IsAny<CheckSessionTimeoutsCommand>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopping")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRunning_ShouldSendCheckSessionTimeoutsCommand()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<CheckSessionTimeoutsCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel()) // Cancel immediately after first check
            .ReturnsAsync(Unit.Value);

        var service = new TestableSessionTimeoutBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await service.CallExecuteAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _mockMediator.Verify(m => m.Send(It.IsAny<CheckSessionTimeoutsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCheckSessionsThrows_ShouldLogError()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<CheckSessionTimeoutsCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var service = new TestableSessionTimeoutBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await service.CallExecuteAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while checking")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
