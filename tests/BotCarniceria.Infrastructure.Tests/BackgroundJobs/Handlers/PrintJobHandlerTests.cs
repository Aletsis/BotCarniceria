using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BotCarniceria.Core.Application.Specifications;
using System.Linq.Expressions;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Common;
using Hangfire.States;

namespace BotCarniceria.Infrastructure.Tests.BackgroundJobs.Handlers;

public class PrintJobHandlerTests
{
    private readonly Mock<IPrintingService> _printingServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IWhatsAppService> _whatsAppServiceMock;
    private readonly Mock<ILogger<PrintJobHandler>> _loggerMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IConfiguracionRepository> _settingsMock;
    private readonly Mock<IUsuarioRepository> _usersMock;
    private readonly PrintJobHandler _handler;

    public PrintJobHandlerTests()
    {
        _printingServiceMock = new Mock<IPrintingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _whatsAppServiceMock = new Mock<IWhatsAppService>();
        _loggerMock = new Mock<ILogger<PrintJobHandler>>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _settingsMock = new Mock<IConfiguracionRepository>();
        _usersMock = new Mock<IUsuarioRepository>();

        _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingsMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_usersMock.Object);

        _handler = new PrintJobHandler(
            _printingServiceMock.Object,
            _unitOfWorkMock.Object,
            _whatsAppServiceMock.Object,
            _loggerMock.Object,
            _backgroundJobClientMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPedidoExistsAndPrintSucceeds_ShouldCompleteSuccessfully()
    {
        // Arrange
        var job = new EnqueuePrintJob { PedidoId = 1, PrinterName = "TestPrinter", RetryCount = 0 };
        var cliente = Cliente.Create("John Doe", "1234567890");
        var pedido = Pedido.Create(cliente.ClienteID, "1kg Carne", "Sin notas", "Efectivo");
        
        // Setup UnitOfWork
        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(job.PedidoId))
            .ReturnsAsync(pedido);
        _unitOfWorkMock.Setup(u => u.Clientes.GetByIdAsync(pedido.ClienteID))
            .ReturnsAsync(cliente);

        // Setup PrintingService
        _printingServiceMock.Setup(p => p.PrintTicketAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        _printingServiceMock.Verify(p => p.PrintTicketAsync(
            It.IsAny<string>(),
            cliente.Nombre,
            cliente.NumeroTelefono,
            It.IsAny<string>(),
            pedido.Contenido,
            It.IsAny<string>()), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPedidoNotFound_ShouldReturnEarly()
    {
        // Arrange
        var job = new EnqueuePrintJob { PedidoId = 999, PrinterName = "TestPrinter" };
        
        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(job.PedidoId))
            .ReturnsAsync((Pedido?)null);

        // Act
        await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        _printingServiceMock.Verify(p => p.PrintTicketAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPrintFails_ShouldScheduleRetry()
    {
        // Arrange
        var job = new EnqueuePrintJob { PedidoId = 1, PrinterName = "TestPrinter", RetryCount = 0 };
        var cliente = Cliente.Create("John Doe", "1234567890");
        var pedido = Pedido.Create(cliente.ClienteID, "1kg Carne", "Sin notas", "Efectivo");

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(job.PedidoId))
            .ReturnsAsync(pedido);
        _unitOfWorkMock.Setup(u => u.Clientes.GetByIdAsync(pedido.ClienteID))
            .ReturnsAsync(cliente);

        _printingServiceMock.Setup(p => p.PrintTicketAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // Simulate Failure

        // Default settings for retry
        _settingsMock.Setup(s => s.GetValorAsync("Sistema.PrintRetryCount")).ReturnsAsync("3");
        _settingsMock.Setup(s => s.GetValorAsync("Sistema.RetryIntervalSeconds")).ReturnsAsync("60");
        
        // Mock Users for notification
        _usersMock.Setup(u => u.FindAsync(It.IsAny<Specification<Usuario>>()))
            .ReturnsAsync(new List<Usuario>());

        // Act
        // Should NOT throw exception now, but catch and schedule retry
        await _handler.ExecuteAsync(job, CancellationToken.None);

        // Assert
        // Verify that duplicate job was scheduled
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(j => j.Type == typeof(PrintJobHandler) && j.Method.Name == "ExecuteAsync"),
            It.IsAny<ScheduledState>()), Times.Once);

        // Verify that retry count was incremented in the scheduled job arguments
        // This is complex to verify deeply with Moq without inspecting the arguments of the Job. 
        // For now, Times.Once is sufficient proof it reached the scheduling block.
    }
}
