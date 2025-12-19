using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class MenuStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IConfiguracionRepository> _mockConfigRepository;
    private readonly MenuStateHandler _handler;

    public MenuStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockConfigRepository = new Mock<IConfiguracionRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockConfigRepository.Object);

        _handler = new MenuStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object);
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("escribe tu pedido") && msg.Contains("Juan Pérez"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.TAKING_ORDER);
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("últimos pedidos") && msg.Contains("Folio"))),
            Times.Once);
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => 
                msg.Contains("Información") && 
                msg.Contains("Dirección") && 
                msg.Contains("Horarios")),
            It.Is<List<(string id, string title)>>(b => b.Any(btn => btn.id == "menu_hacer_pedido")),
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("Información")),
            It.IsAny<List<(string id, string title)>>(),
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
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(msg => msg.Contains("selecciona una opción")),
            It.Is<List<(string id, string title)>>(b => b.Count == 3),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);
    }

    #endregion
}
