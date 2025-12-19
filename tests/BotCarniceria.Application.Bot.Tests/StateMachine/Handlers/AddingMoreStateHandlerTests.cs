using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class AddingMoreStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly AddingMoreStateHandler _handler;

    public AddingMoreStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _handler = new AddingMoreStateHandler(_mockWhatsAppService.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldAppendToExistingOrder()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var existingOrder = "2 kg de carne molida";
        var newItems = "1 kg de bistec";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer(existingOrder);

        // Act
        await _handler.HandleAsync(phoneNumber, newItems, session);

        // Assert
        session.Buffer.Should().Contain(existingOrder);
        session.Buffer.Should().Contain(newItems);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeStateToAwaitingConfirm()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "500g de chorizo";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("Pedido existente");

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.Estado.Should().Be(ConversationState.AWAITING_CONFIRM);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendUpdatedSummary()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var existingOrder = "2 kg de carne";
        var newItems = "1 kg de pollo";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer(existingOrder);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendInteractiveButtonsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, List<(string id, string title)>, string?, string?>((_, msg, __, ___, ____) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, newItems, session);

        // Assert
        capturedMessage.Should().Contain("actualizado");
        capturedMessage.Should().Contain(existingOrder);
        capturedMessage.Should().Contain(newItems);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyBuffer_ShouldHandleGracefully()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "1 kg de carne";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.Buffer.Should().Contain(messageContent);
    }
}
