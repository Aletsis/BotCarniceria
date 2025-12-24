using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class TakingOrderStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly TakingOrderStateHandler _handler;

    public TakingOrderStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _handler = new TakingOrderStateHandler(_mockWhatsAppService.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendOrderSummaryWithButtons()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "2 kg de carne molida\n1 kg de bistec";
        var session = Conversacion.Create(phoneNumber);

        List<(string id, string title)>? capturedButtons = null;
        string? capturedMessage = null;

        _mockWhatsAppService.Setup(x => x.SendInteractiveButtonsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, List<(string id, string title)>, string?, string?>((_, msg, buttons, __, ___) =>
            {
                capturedMessage = msg;
                capturedButtons = buttons;
            })
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage.Should().Contain("Resumen de tu pedido");
        capturedMessage.Should().Contain(messageContent);

        capturedButtons.Should().NotBeNull();
        capturedButtons.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeConfirmButton()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "1 kg de chorizo";
        var session = Conversacion.Create(phoneNumber);

        List<(string id, string title)>? capturedButtons = null;

        _mockWhatsAppService.Setup(x => x.SendInteractiveButtonsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, List<(string id, string title)>, string?, string?>((_, __, buttons, ___, ____) => capturedButtons = buttons)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        capturedButtons.Should().Contain(b => b.id == "order_confirm");
        capturedButtons.Should().Contain(b => b.title.Contains("Confirmar"));
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeAddMoreButton()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "500g de tocino";
        var session = Conversacion.Create(phoneNumber);

        List<(string id, string title)>? capturedButtons = null;

        _mockWhatsAppService.Setup(x => x.SendInteractiveButtonsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, List<(string id, string title)>, string?, string?>((_, __, buttons, ___, ____) => capturedButtons = buttons)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        capturedButtons.Should().Contain(b => b.id == "order_add_more");
        capturedButtons.Should().Contain(b => b.title.Contains("Agregar"));
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveOrderInBuffer()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "3 kg de carne asada";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Buffer.Should().Be(messageContent);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeStateToAwaitingConfirm()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "1 kg de arrachera";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.TAKING_ORDER);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Estado.Should().Be(ConversationState.AWAITING_CONFIRM);
    }

    [Fact]
    public async Task HandleAsync_WithLongOrder_ShouldHandleCorrectly()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = @"2 kg de carne molida
1 kg de bistec
500g de chorizo
1 kg de arrachera
3 piezas de costilla";
        var session = Conversacion.Create(phoneNumber);

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
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Buffer.Should().Be(messageContent);
        session.Estado.Should().Be(ConversationState.AWAITING_CONFIRM);
        capturedMessage.Should().Contain(messageContent);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyOrder_ShouldStillProcess()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Buffer.Should().Be(messageContent);
        session.Estado.Should().Be(ConversationState.AWAITING_CONFIRM);
    }
}
