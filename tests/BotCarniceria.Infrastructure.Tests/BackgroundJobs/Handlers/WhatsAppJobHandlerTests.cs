using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.BackgroundJobs.Handlers;

public class WhatsAppJobHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ILogger<WhatsAppJobHandler>> _mockLogger;
    private readonly WhatsAppJobHandler _handler;

    public WhatsAppJobHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<WhatsAppJobHandler>>();
        _handler = new WhatsAppJobHandler(_mockWhatsAppService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageSentSuccessfully_ShouldCompleteAndLog()
    {
        // Arrange
        var job = new EnqueueWhatsAppMessageJob
        {
            JobId = "job-100",
            PhoneNumber = "5551234567",
            Message = "Tu pedido está listo"
        };

        _mockWhatsAppService.Setup(w => w.SendTextMessageAsync("5551234567", "Tu pedido está listo"))
            .ReturnsAsync(true);

        // Act
        await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync("5551234567", "Tu pedido está listo"), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSendReturnsFalse_ShouldThrowInvalidOperationExceptionAndLog()
    {
        // Arrange
        var job = new EnqueueWhatsAppMessageJob
        {
            JobId = "job-fail",
            PhoneNumber = "5551234567",
            Message = "Hola"
        };

        _mockWhatsAppService.Setup(w => w.SendTextMessageAsync("5551234567", "Hola"))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to send WhatsApp message to 5551234567");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed for phone 5551234567")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenServiceThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var job = new EnqueueWhatsAppMessageJob
        {
            JobId = "job-ex",
            PhoneNumber = "5551234567",
            Message = "Hola"
        };

        _mockWhatsAppService.Setup(w => w.SendTextMessageAsync("5551234567", "Hola"))
            .ThrowsAsync(new HttpRequestException("Network failure"));

        // Act
        var act = async () => await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed for phone 5551234567")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
