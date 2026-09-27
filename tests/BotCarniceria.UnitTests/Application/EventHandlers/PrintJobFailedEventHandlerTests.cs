using BotCarniceria.Core.Application.EventHandlers;
using BotCarniceria.Core.Application.Events;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.EventHandlers;

public class PrintJobFailedEventHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IUsuarioRepository> _mockUserRepo;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ILogger<PrintJobFailedEventHandler>> _mockLogger;
    private readonly PrintJobFailedEventHandler _handler;

    public PrintJobFailedEventHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockUserRepo = new Mock<IUsuarioRepository>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<PrintJobFailedEventHandler>>();

        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepo.Object);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepo.Object);

        _handler = new PrintJobFailedEventHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object,
            _mockLogger.Object);
    }

    private static Pedido CreatePedidoWithCliente(long pedidoId = 1L)
    {
        var cliente = Cliente.Create("5551234567", "María López", "Av Libertad 10");
        var pedido = Pedido.Create(1, "2 kg bistec");
        typeof(Pedido).GetProperty(nameof(Pedido.PedidoID))?.SetValue(pedido, pedidoId);
        typeof(Pedido).GetProperty(nameof(Pedido.Cliente))?.SetValue(pedido, cliente);
        return pedido;
    }

    [Fact]
    public async Task Handle_WhenFirstAttemptAndNotFinal_SendsRetryMessageToAdminsAndSupervisors()
    {
        // Arrange
        var pedido = CreatePedidoWithCliente(10L);
        _mockOrderRepo.Setup(x => x.GetByIdAsync(10L))
            .ReturnsAsync(pedido);

        var admin = Usuario.Create("admin1", "hash", "Admin", RolUsuario.Admin, "5551112222");
        _mockUserRepo.Setup(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()))
            .ReturnsAsync(new List<Usuario> { admin });

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync("5551112222", It.IsAny<string>()))
            .Callback<string, string>((_, msg) => capturedMessage = msg)
            .ReturnsAsync(true);

        var notification = new PrintJobFailedEvent(10L, 1, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage.Should().Contain("en el primer intento");
        capturedMessage.Should().Contain("El sistema reintentará automáticamente.");
        capturedMessage.Should().Contain(pedido.Folio.Value);
        capturedMessage.Should().Contain("María López");
        capturedMessage.Should().Contain("5551234567");

        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync("5551112222", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubsequentAttemptAndFinal_SendsManualInterventionMessage()
    {
        // Arrange
        var pedido = CreatePedidoWithCliente(20L);
        _mockOrderRepo.Setup(x => x.GetByIdAsync(20L))
            .ReturnsAsync(pedido);

        var supervisor = Usuario.Create("super1", "hash", "Super", RolUsuario.Supervisor, "5553334444");
        _mockUserRepo.Setup(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()))
            .ReturnsAsync(new List<Usuario> { supervisor });

        string? capturedMessage = null;
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync("5553334444", It.IsAny<string>()))
            .Callback<string, string>((_, msg) => capturedMessage = msg)
            .ReturnsAsync(true);

        var notification = new PrintJobFailedEvent(20L, 3, true);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage.Should().Contain("en el intento #3");
        capturedMessage.Should().Contain("Se han agotado los reintentos. Revise la impresora MANUALMENTE.");
    }

    [Fact]
    public async Task Handle_WhenPedidoNotFound_LogsWarningAndReturns()
    {
        // Arrange
        _mockOrderRepo.Setup(x => x.GetByIdAsync(999L))
            .ReturnsAsync((Pedido?)null);

        var notification = new PrintJobFailedEvent(999L, 1, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockUserRepo.Verify(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()), Times.Never);
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoAdminsOrSupervisorsFound_LogsWarningAndReturns()
    {
        // Arrange
        var pedido = CreatePedidoWithCliente(1L);
        _mockOrderRepo.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(pedido);

        _mockUserRepo.Setup(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()))
            .ReturnsAsync(new List<Usuario>());

        var notification = new PrintJobFailedEvent(1L, 1, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminHasNoPhone_SkipsSending()
    {
        // Arrange
        var pedido = CreatePedidoWithCliente(1L);
        _mockOrderRepo.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(pedido);

        var adminSinTelefono = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin, null);
        _mockUserRepo.Setup(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()))
            .ReturnsAsync(new List<Usuario> { adminSinTelefono });

        var notification = new PrintJobFailedEvent(1L, 1, false);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSendTextMessageFails_CompletesCleanly()
    {
        // Arrange
        var pedido = CreatePedidoWithCliente(1L);
        _mockOrderRepo.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(pedido);

        var admin = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin, "5551112222");
        _mockUserRepo.Setup(x => x.FindAsync(It.IsAny<AdminAndSupervisorUsersSpecification>()))
            .ReturnsAsync(new List<Usuario> { admin });

        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // Delivery failed

        var notification = new PrintJobFailedEvent(1L, 1, false);

        // Act
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync("5551112222", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_CatchesAndLogsError()
    {
        // Arrange
        _mockOrderRepo.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var notification = new PrintJobFailedEvent(1L, 1, false);

        // Act
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
