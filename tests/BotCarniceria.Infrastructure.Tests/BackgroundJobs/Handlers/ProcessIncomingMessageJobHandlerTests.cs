using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.BackgroundJobs.Handlers;

public class ProcessIncomingMessageJobHandlerTests
{
    private readonly Mock<IIncomingMessageHandler> _mockIncomingMessageHandler;
    private readonly Mock<ILogger<ProcessIncomingMessageJobHandler>> _mockLogger;
    private readonly ProcessIncomingMessageJobHandler _handler;

    public ProcessIncomingMessageJobHandlerTests()
    {
        _mockIncomingMessageHandler = new Mock<IIncomingMessageHandler>();
        _mockLogger = new Mock<ILogger<ProcessIncomingMessageJobHandler>>();
        _handler = new ProcessIncomingMessageJobHandler(_mockIncomingMessageHandler.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallIncomingMessageHandler_WhenSuccessful()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            Id = "msg_001",
            From = "5551234567",
            Type = "text",
            Text = new WhatsAppText { Body = "Hola" }
        };
        var job = new ProcessIncomingMessageJob { Message = message };

        // Act
        await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        _mockIncomingMessageHandler.Verify(h => h.HandleAsync(message), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing incoming message ID: msg_001")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            Id = "msg_err",
            From = "5551234567"
        };
        var job = new ProcessIncomingMessageJob { Message = message };

        _mockIncomingMessageHandler.Setup(h => h.HandleAsync(message))
            .ThrowsAsync(new InvalidOperationException("Failed message processing"));

        // Act
        var act = async () => await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed message processing");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing incoming message ID: msg_err")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
