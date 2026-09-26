using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class EstadosHandlersTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ILogger<EstadosHandlers>> _mockLogger;
    private readonly EstadosHandlers _handler;

    public EstadosHandlersTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<EstadosHandlers>>();
        _handler = new EstadosHandlers(_mockWhatsAppService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenUploadSucceeds_ReturnsMediaId()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var fileName = "promocion_fin_semana.jpg";
        var contentType = "image/jpeg";
        var caption = "¡Ofertas de fin de semana!";

        _mockWhatsAppService.Setup(x => x.UploadMediaAsync(stream, fileName, contentType))
            .ReturnsAsync("media_id_abc123");

        var command = new UploadEstadoCommand(stream, fileName, contentType, caption);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be("media_id_abc123");
        _mockWhatsAppService.Verify(x => x.UploadMediaAsync(stream, fileName, contentType), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUploadFails_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream();
        var fileName = "fallido.png";
        var contentType = "image/png";

        _mockWhatsAppService.Setup(x => x.UploadMediaAsync(stream, fileName, contentType))
            .ReturnsAsync((string?)null);

        var command = new UploadEstadoCommand(stream, fileName, contentType, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockWhatsAppService.Verify(x => x.UploadMediaAsync(stream, fileName, contentType), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWhatsAppServiceThrows_PropagatesException()
    {
        // Arrange
        using var stream = new MemoryStream();
        var command = new UploadEstadoCommand(stream, "test.jpg", "image/jpeg", null);

        _mockWhatsAppService.Setup(x => x.UploadMediaAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("API error"));

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("API error");
    }
}
