using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class ConfirmLateOrderStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly ConfirmLateOrderStateHandler _handler;

    public ConfirmLateOrderStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);

        _handler = new ConfirmLateOrderStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_LateOrderContinue_WhenClienteIsNull_ShouldCreateClienteAndAskName()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, "late_order_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockClienteRepository.Verify(x => x.AddAsync(It.Is<Cliente>(c =>
            c.NumeroTelefono == phoneNumber &&
            c.Nombre == "Nuevo Cliente" &&
            c.Direccion == "Sin Dirección")), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("nombre completo"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_NAME);
    }

    [Fact]
    public async Task HandleAsync_LateOrderContinue_WhenClienteNameIsDefault_ShouldAskName()
    {
        var name = "Nuevo Cliente";
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);
        var cliente = Cliente.Create(phoneNumber, name, "Calle 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, "late_order_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("nombre completo"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_NAME);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Sin Dirección")]
    public async Task HandleAsync_LateOrderContinue_WhenClienteAddressIsEmptyOrDefault_ShouldAskAddress(string address)
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", address);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, "late_order_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("dirección de entrega"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_LateOrderContinue_WhenClienteHasCompleteData_ShouldSetTakingOrder()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Av. Principal 456");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, "late_order_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Juan Pérez") && msg.Contains("escribe tu pedido"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.TAKING_ORDER);
    }

    [Fact]
    public async Task HandleAsync_LateOrderCancel_ShouldSendCancellationAndMenuButtonsAndSetStateMenu()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);

        // Act
        await _handler.HandleAsync(phoneNumber, "late_order_cancel", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("operación cancelada"))),
            Times.Once);

        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("menú")),
            It.Is<List<(string id, string title)>>(buttons =>
                buttons.Count == 3 &&
                buttons.Any(b => b.id == "menu_hacer_pedido") &&
                buttons.Any(b => b.id == "menu_estado_pedido") &&
                buttons.Any(b => b.id == "menu_informacion")),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.MENU);
    }

    [Theory]
    [InlineData("otro_texto")]
    [InlineData("ayuda")]
    [InlineData("123")]
    public async Task HandleAsync_InvalidInput_ShouldReiterateWarningButtonsWithoutChangingState(string input)
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);

        // Act
        await _handler.HandleAsync(phoneNumber, input, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Aviso de Horario") && msg.Contains("4:00 P.M.")),
            It.Is<List<(string id, string title)>>(buttons =>
                buttons.Count == 2 &&
                buttons.Any(b => b.id == "late_order_continue") &&
                buttons.Any(b => b.id == "late_order_cancel")),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.CONFIRM_LATE_ORDER);
    }
}
