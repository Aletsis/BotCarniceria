using BotCarniceria.Application.Bot.EventHandlers;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.EventHandlers;

public class PedidoCreatedEventHandlerTests
{
    private readonly Mock<ILogger<PedidoCreatedEventHandler>> _mockLogger;
    private readonly PedidoCreatedEventHandler _handler;

    public PedidoCreatedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PedidoCreatedEventHandler>>();
        _handler = new PedidoCreatedEventHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldLogInformation()
    {
        // Arrange
        // We need to create a valid Pedido. Assuming Pedido.Create exists and is accessible.
        // We also need a valid Cliente or mock it if Pedido needs it. 
        // Pedido usually needs a ClienteID.
        // Let's create a Client and then a Order for it.
        // We pass an arbitrary int ID because Pedido.Create expects int
        var pedido = Pedido.Create(1, "1kg Carne");
        
        var domainEvent = new PedidoCreatedEvent(pedido);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pedido creado")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}
