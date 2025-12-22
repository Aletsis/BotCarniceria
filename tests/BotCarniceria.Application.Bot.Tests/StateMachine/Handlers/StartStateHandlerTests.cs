using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class StartStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly Mock<ILogger<StartStateHandler>> _mockLogger;
    private readonly StartStateHandler _handler;

    public StartStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockLogger = new Mock<ILogger<StartStateHandler>>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);

        _handler = new StartStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingCliente_ShouldSendPersonalizedGreeting()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Hola";
        var session = Conversacion.Create(phoneNumber);
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendInteractiveListAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title, string? description)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, string, List<(string id, string title, string? description)>, string?, string?>((_, msg, __, ___, ____, _____) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedMessage.Should().Contain("Juan Pérez");
        session.Estado.Should().Be(ConversationState.MENU);
    }

    [Fact]
    public async Task HandleAsync_WithNewCliente_ShouldSendGenericGreeting()
    {
        // Arrange
        var phoneNumber = "5559999999";
        var messageContent = "Hola";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendInteractiveListAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title, string? description)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, string, List<(string id, string title, string? description)>, string?, string?>((_, msg, __, ___, ____, _____) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedMessage.Should().Contain("Bienvenido/a");
        capturedMessage.Should().Contain("Blanqui");
        session.Estado.Should().Be(ConversationState.MENU);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendThreeMenuButtons()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Hola";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        List<(string id, string title, string? description)>? capturedRows = null;
        _mockWhatsAppService.Setup(x => x.SendInteractiveListAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<(string id, string title, string? description)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Callback<string, string, string, List<(string id, string title, string? description)>, string?, string?>((_, __, ___, rows, ____, _____) => capturedRows = rows)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        capturedRows.Should().NotBeNull();
        capturedRows.Should().HaveCount(4);
        capturedRows.Should().Contain(b => b.id == "menu_hacer_pedido");
        capturedRows.Should().Contain(b => b.id == "menu_estado_pedido");
        capturedRows.Should().Contain(b => b.id == "menu_solicitar_factura");
        capturedRows.Should().Contain(b => b.id == "menu_informacion");
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeSessionStateToMenu()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "Hola";
        var session = Conversacion.Create(phoneNumber);
        var initialState = session.Estado;

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, session);

        // Assert
        initialState.Should().Be(ConversationState.START);
        session.Estado.Should().Be(ConversationState.MENU);
    }
}
