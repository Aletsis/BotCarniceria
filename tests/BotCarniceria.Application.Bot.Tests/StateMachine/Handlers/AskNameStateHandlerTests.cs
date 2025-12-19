using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class AskNameStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly AskNameStateHandler _handler;

    public AskNameStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _handler = new AskNameStateHandler(_mockWhatsAppService.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveNameInSession()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Juan Pérez";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.NombreTemporal.Should().Be(messageContent);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeStateToAskAddress()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "María García";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.ASK_NAME);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendConfirmationMessageWithName()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Carlos López";
        var session = Conversacion.Create(phoneNumber);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Callback<string, string>((_, msg) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedMessage.Should().Contain("Gracias");
        capturedMessage.Should().Contain(messageContent);
        capturedMessage.Should().Contain("dirección");
    }

    [Fact]
    public async Task HandleAsync_WithLongName_ShouldHandleCorrectly()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Juan Carlos Pérez García de la Torre";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.NombreTemporal.Should().Be(messageContent);
        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_WithShortName_ShouldHandleCorrectly()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Ana";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.NombreTemporal.Should().Be(messageContent);
        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }
}
