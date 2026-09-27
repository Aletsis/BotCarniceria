using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class MenuStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IConfiguracionRepository> _mockConfigRepository;
    private readonly MenuStateHandler _handler;

    public MenuStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockConfigRepository = new Mock<IConfiguracionRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockConfigRepository.Object);

        _handler = new MenuStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object,
            _mockDateTimeProvider.Object);
    }

    #region Hacer Pedido Tests

    [Fact]
    public async Task HandleAsync_HacerPedido_WithNewCliente_ShouldAskForName()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_hacer_pedido";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("nombre completo"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_NAME);
    }

    [Fact]
    public async Task HandleAsync_HacerPedido_WithClienteWithoutAddress_ShouldAskForAddress()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_hacer_pedido";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("dirección"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_ADDRESS);
    }

    [Fact]
    public async Task HandleAsync_HacerPedido_WithCompleteCliente_ShouldStartTakingOrder()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_hacer_pedido";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Calle Principal 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("escribe tu pedido") && msg.Contains("Juan Pérez"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.TAKING_ORDER);
    }

    [Fact]
    public async Task HandleAsync_HacerPedido_WhenAfterWarningTime_DefaultSetting_ShouldPromptLateOrderConfirmation()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);

        _mockConfigRepository.Setup(x => x.GetValorAsync(ConfigurationKeys.Orders.LateOrderWarningStartHour))
            .ReturnsAsync((string?)null); // Default 16:00
        _mockDateTimeProvider.Setup(x => x.LocalTimeOfDay)
            .Returns(new TimeSpan(16, 30, 0)); // 4:30 PM

        // Act
        await _handler.HandleAsync(phoneNumber, "menu_hacer_pedido", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Aviso de Horario") && msg.Contains("entregarlo al día siguiente")),
            It.Is<List<(string id, string title)>>(buttons =>
                buttons.Any(b => b.id == "late_order_continue") &&
                buttons.Any(b => b.id == "late_order_cancel")),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.CONFIRM_LATE_ORDER);
    }

    [Fact]
    public async Task HandleAsync_HacerPedido_WhenAfterWarningTime_CustomHHmmSetting_ShouldPromptLateOrderConfirmation()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);

        _mockConfigRepository.Setup(x => x.GetValorAsync(ConfigurationKeys.Orders.LateOrderWarningStartHour))
            .ReturnsAsync("17:45");
        _mockDateTimeProvider.Setup(x => x.LocalTimeOfDay)
            .Returns(new TimeSpan(18, 0, 0)); // 6:00 PM >= 5:45 PM

        // Act
        await _handler.HandleAsync(phoneNumber, "menu_hacer_pedido", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Aviso de Horario")),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.CONFIRM_LATE_ORDER);
    }

    [Fact]
    public async Task HandleAsync_HacerPedido_WhenAfterWarningTime_SingleHourSetting_ShouldPromptLateOrderConfirmation()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);

        _mockConfigRepository.Setup(x => x.GetValorAsync(ConfigurationKeys.Orders.LateOrderWarningStartHour))
            .ReturnsAsync("15");
        _mockDateTimeProvider.Setup(x => x.LocalTimeOfDay)
            .Returns(new TimeSpan(15, 1, 0));

        // Act
        await _handler.HandleAsync(phoneNumber, "menu_hacer_pedido", TipoContenidoMensaje.Texto, session);

        // Assert
        session.Estado.Should().Be(ConversationState.CONFIRM_LATE_ORDER);
    }

    #endregion

    #region Estado Pedido Tests

    [Fact]
    public async Task HandleAsync_EstadoPedido_WithNoCliente_ShouldInformNoOrders()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_estado_pedido";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("No tienes pedidos"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EstadoPedido_WithNoPedidos_ShouldInformNoOrders()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_estado_pedido";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<OrdersByClienteIdSpecification>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("No tienes pedidos"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EstadoPedido_WithPedidos_ShouldShowRecentOrders()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_estado_pedido";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        var pedidos = new List<Pedido>
        {
            Pedido.Create(cliente.ClienteID, "Pedido 1"),
            Pedido.Create(cliente.ClienteID, "Pedido 2")
        };

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<OrdersByClienteIdSpecification>()))
            .ReturnsAsync(pedidos);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("últimos pedidos") && msg.Contains("Folio"))),
            Times.Once);
    }

    #endregion

    #region Facturación Tests

    [Fact]
    public async Task HandleAsync_SolicitarFactura_WhenClienteIsNull_ShouldAskForNameAndSetAskName()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, "menu_solicitar_factura", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("datos básicos") && msg.Contains("nombre completo"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.ASK_NAME);
    }

    [Fact]
    public async Task HandleAsync_SolicitarFactura_WhenClienteExists_ShouldShowWarningButtonsAndSetBillingWarning()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, "menu_solicitar_factura", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Aviso Importante") && msg.Contains("facturación es diaria")),
            It.Is<List<(string id, string title)>>(buttons =>
                buttons.Any(b => b.id == "billing_warning_continue") &&
                buttons.Any(b => b.id == "billing_warning_cancel")),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_WARNING);
    }

    #endregion

    #region Información Tests

    [Fact]
    public async Task HandleAsync_Informacion_ShouldShowBusinessInfo()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_informacion";
        var session = Conversacion.Create(phoneNumber);

        _mockConfigRepository.Setup(x => x.GetValorAsync("Negocio_Horarios"))
            .ReturnsAsync("Lun-Sáb 8:00 AM - 8:00 PM");
        _mockConfigRepository.Setup(x => x.GetValorAsync("Negocio_Direccion"))
            .ReturnsAsync("Calle Principal 123");
        _mockConfigRepository.Setup(x => x.GetValorAsync("Negocio_Telefono"))
            .ReturnsAsync("555-1234");
        _mockConfigRepository.Setup(x => x.GetValorAsync("Negocio_TiempoEntrega"))
            .ReturnsAsync("60-90 minutos");

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveListAsync(
            phoneNumber,
            It.Is<string>(msg => 
                msg.Contains("Información") && 
                msg.Contains("Dirección") && 
                msg.Contains("Horarios")),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title, string? description)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Informacion_WithNullConfig_ShouldUseDefaults()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "menu_informacion";
        var session = Conversacion.Create(phoneNumber);

        _mockConfigRepository.Setup(x => x.GetValorAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveListAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Información")),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title, string? description)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);
    }

    #endregion

    #region Invalid Option Tests

    [Fact]
    public async Task HandleAsync_InvalidOption_ShouldShowMenuButtons()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "opcion_invalida";
        var session = Conversacion.Create(phoneNumber);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveListAsync(
            phoneNumber,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<List<(string id, string title, string? description)>>(b => b.Count == 4),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);
    }

    #endregion
}
