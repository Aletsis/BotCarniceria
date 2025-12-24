using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs; // Added for EnqueuePrintJob
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class SelectPaymentStateHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<SelectPaymentStateHandler>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IConfiguracionRepository> _mockSettings;
    private readonly SelectPaymentStateHandler _handler;

    public SelectPaymentStateHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockNotificationService = new Mock<IRealTimeNotificationService>();
        _mockLogger = new Mock<ILogger<SelectPaymentStateHandler>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockSettings = new Mock<IConfiguracionRepository>();

        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettings.Object);

        _handler = new SelectPaymentStateHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object,
            _mockBackgroundJobService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object);
    }

    [Fact]
    public async Task HandleAsync_PaymentCash_ShouldAddPedidoToRepository()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("2 kg de carne molida");
        var cliente = Cliente.Create(phoneNumber, "Juan Pérez", "Calle Principal 123");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        _mockBackgroundJobService.Setup(x => x.EnqueueAsync(It.IsAny<EnqueuePrintJob>(), It.IsAny<CancellationToken>())) // Fixed signature (CancellationToken optional? Usually generic EnqueueAsync<T>)
            .ReturnsAsync("job-id");

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockOrderRepository.Verify(x => x.AddAsync(It.IsAny<Pedido>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldResetSessionAfterOrder()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("Pedido de prueba");
        var cliente = Cliente.Create(phoneNumber, "Test", "Test Address");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        session.Buffer.Should().BeNullOrEmpty();
        session.Estado.Should().Be(ConversationState.START);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendConfirmationMessage()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("Test order");
        var cliente = Cliente.Create(phoneNumber, "Test", "Test Address");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Callback<string, string>((_, msg) => capturedMessage = msg)
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        capturedMessage.Should().Contain("Pedido confirmado");
        capturedMessage.Should().Contain("Folio");
    }

    [Fact]
    public async Task HandleAsync_ShouldEnqueuePrintJob()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("Test order");
        var cliente = Cliente.Create(phoneNumber, "Test Cliente", "Test Address");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockBackgroundJobService.Verify(x => x.EnqueueAsync(
            It.IsAny<EnqueuePrintJob>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotifyOrdersUpdated()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("Test order");
        var cliente = Cliente.Create(phoneNumber, "Test", "Test Address");

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockNotificationService.Verify(x => x.NotifyOrdersUpdatedAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNoCliente_ShouldRollbackAndReturn()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var messageContent = "payment_cash";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepository.Setup(x => x.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, messageContent, TipoContenidoMensaje.Texto, session);

        // Assert
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _mockOrderRepository.Verify(x => x.AddAsync(It.IsAny<Pedido>()), Times.Never);
    }
}
