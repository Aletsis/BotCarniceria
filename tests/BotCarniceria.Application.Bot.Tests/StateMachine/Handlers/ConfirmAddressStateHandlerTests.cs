using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class ConfirmAddressStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly ConfirmAddressStateHandler _handler;

    public ConfirmAddressStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);

        _handler = new ConfirmAddressStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_AddressCorrect_ShouldAskForPaymentMethod()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "address_correct";
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedMessage.Should().Contain("Forma de Pago");
    }

    [Fact]
    public async Task HandleAsync_AddressCorrect_ShouldChangeStateToSelectPayment()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "address_correct";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.Estado.Should().Be(ConversationState.SELECT_PAYMENT);
    }

    [Fact]
    public async Task HandleAsync_AddressCorrect_ShouldSendTwoPaymentOptions()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "address_correct";
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedButtons.Should().HaveCount(2);
        capturedButtons.Should().Contain(b => b.id == "payment_cash");
        capturedButtons.Should().Contain(b => b.id == "payment_card");
    }

    [Fact]
    public async Task HandleAsync_AddressWrong_ShouldAskForNewAddress()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "address_wrong";
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
        capturedMessage.Should().Contain("nueva dirección");
    }

    [Fact]
    public async Task HandleAsync_AddressWrong_ShouldChangeStateToAskAddress()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "address_wrong";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_InvalidResponse_ShouldResendConfirmation()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "invalid_response";
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedMessage.Should().Contain("Confirmación de Dirección");
        capturedMessage.Should().Contain(cliente.Direccion);
    }
}
