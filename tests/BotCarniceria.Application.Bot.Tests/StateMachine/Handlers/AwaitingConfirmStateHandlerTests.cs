using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class AwaitingConfirmStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly AwaitingConfirmStateHandler _handler;

    public AwaitingConfirmStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);

        _handler = new AwaitingConfirmStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_OrderConfirm_ShouldAskForAddressConfirmation()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_confirm";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Calle Principal 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

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
        capturedMessage.Should().Contain("Confirmación de Dirección");
        capturedMessage.Should().Contain(cliente.Direccion);
    }

    [Fact]
    public async Task HandleAsync_OrderConfirm_ShouldChangeStateToConfirmAddress()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_confirm";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Calle Principal 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Estado.Should().Be(ConversationState.CONFIRM_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_OrderConfirm_ShouldSendTwoButtons()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_confirm";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Calle Principal 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

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
        capturedButtons.Should().HaveCount(2);
        capturedButtons.Should().Contain(b => b.id == "address_correct");
        capturedButtons.Should().Contain(b => b.id == "address_wrong");
    }

    [Fact]
    public async Task HandleAsync_OrderAddMore_ShouldSendAddMoreMessage()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_add_more";
        var session = Conversacion.Create(phoneNumber);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Callback<string, string>((_, msg) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        capturedMessage.Should().Contain("agregar");
    }

    [Fact]
    public async Task HandleAsync_OrderAddMore_ShouldChangeStateToAddingMore()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_add_more";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Estado.Should().Be(ConversationState.ADDING_MORE);
    }

    [Fact]
    public async Task HandleAsync_OrderConfirm_WithNoAddress_ShouldShowNoRegistrada()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "order_confirm";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

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
        capturedMessage.Should().Contain("No registrada");
    }
}
