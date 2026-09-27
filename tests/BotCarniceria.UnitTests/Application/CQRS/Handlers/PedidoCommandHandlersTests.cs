using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class PedidoCommandHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockPedidoRepository;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<IConfiguracionRepository> _mockSettings;

    public PedidoCommandHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPedidoRepository = new Mock<IOrderRepository>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockSettings = new Mock<IConfiguracionRepository>();

        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockPedidoRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettings.Object);
    }

    #region CreatePedidoCommandHandler Tests

    [Fact]
    public async Task CreatePedidoCommandHandler_ShouldCreatePedidoSuccessfully()
    {
        // Arrange
        var command = new CreatePedidoCommand
        {
            ClienteID = 1,
            Contenido = "2 kg de carne molida",
            Notas = "Sin grasa",
            FormaPago = "Efectivo"
        };

        var handler = new CreatePedidoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ClienteID.Should().Be(command.ClienteID);
        result.Contenido.Should().Be(command.Contenido);
        result.Notas.Should().Be(command.Notas);
        result.FormaPago.Should().Be(command.FormaPago);

        _mockPedidoRepository.Verify(x => x.AddAsync(It.IsAny<Pedido>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePedidoCommandHandler_ShouldCreatePedidoWithDefaultFormaPago()
    {
        // Arrange
        var command = new CreatePedidoCommand
        {
            ClienteID = 1,
            Contenido = "1 kg de bistec"
        };

        var handler = new CreatePedidoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FormaPago.Should().Be("Efectivo");
        _mockPedidoRepository.Verify(x => x.AddAsync(It.IsAny<Pedido>()), Times.Once);
    }

    #endregion

    #region UpdatePedidoEstadoCommandHandler Tests

    [Fact]
    public async Task UpdatePedidoEstadoCommandHandler_ShouldUpdateEstadoSuccessfully()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle 123");
        
        // Use reflection to set the Cliente property
        var clienteProperty = typeof(Pedido).GetProperty("Cliente");
        clienteProperty?.SetValue(pedido, cliente);

        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new UpdatePedidoEstadoCommand
        {
            PedidoID = 1,
            NuevoEstado = "EnRuta"
        };

        var handler = new UpdatePedidoEstadoCommandHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.Estado.Should().Be(EstadoPedido.EnRuta);
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(
            x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePedidoEstadoCommandHandler_WhenPedidoNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Pedido?)null);

        var command = new UpdatePedidoEstadoCommand
        {
            PedidoID = 999,
            NuevoEstado = "EnRuta"
        };

        var handler = new UpdatePedidoEstadoCommandHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Never);
        _mockWhatsAppService.Verify(
            x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdatePedidoEstadoCommandHandler_WithInvalidEstado_ShouldReturnFalse()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new UpdatePedidoEstadoCommand
        {
            PedidoID = 1,
            NuevoEstado = "EstadoInvalido"
        };

        var handler = new UpdatePedidoEstadoCommandHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePedidoEstadoCommandHandler_WithoutCliente_ShouldNotSendWhatsApp()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new UpdatePedidoEstadoCommand
        {
            PedidoID = 1,
            NuevoEstado = "EnRuta"
        };

        var handler = new UpdatePedidoEstadoCommandHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockWhatsAppService.Verify(
            x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region CancelPedidoCommandHandler Tests

    [Fact]
    public async Task CancelPedidoCommandHandler_ShouldCancelPedidoSuccessfully()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new CancelPedidoCommand
        {
            PedidoID = 1,
            Motivo = "Cliente canceló"
        };

        var handler = new CancelPedidoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.Estado.Should().Be(EstadoPedido.Cancelado);
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelPedidoCommandHandler_WhenPedidoNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Pedido?)null);

        var command = new CancelPedidoCommand
        {
            PedidoID = 999
        };

        var handler = new CancelPedidoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Never);
    }

    #endregion

    #region ImprimirPedidoCommandHandler Tests

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenPedidoExists_ShouldMarkAsImpresoAndEnqueuePrintJob()
    {
        // Arrange
        var pedido = Pedido.Create(1, "2 kg arrachera");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(1L))
            .ReturnsAsync(pedido);
        _mockSettings.Setup(x => x.GetValorAsync("Printer_Name"))
            .ReturnsAsync("Termica_Cocina");
        _mockBackgroundJobService.Setup(x => x.EnqueueAsync(It.IsAny<EnqueuePrintJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-101");
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ImprimirPedidoCommand { PedidoID = 1L };
        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object, _mockBackgroundJobService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.EstadoImpresion.Should().BeTrue();
        pedido.FechaImpresion.Should().NotBeNull();

        _mockPedidoRepository.Verify(x => x.UpdateAsync(pedido), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockBackgroundJobService.Verify(x => x.EnqueueAsync(
            It.Is<EnqueuePrintJob>(j =>
                j.PedidoId == pedido.PedidoID &&
                j.PrinterName == "Termica_Cocina" &&
                !j.PrintDuplicate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenPrinterSettingNull_ShouldUseDefaultPrinterName()
    {
        // Arrange
        var pedido = Pedido.Create(1, "1 kg bistec");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(2L))
            .ReturnsAsync(pedido);
        _mockSettings.Setup(x => x.GetValorAsync("Printer_Name"))
            .ReturnsAsync((string?)null);
        _mockBackgroundJobService.Setup(x => x.EnqueueAsync(It.IsAny<EnqueuePrintJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-102");
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ImprimirPedidoCommand { PedidoID = 2L };
        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object, _mockBackgroundJobService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockBackgroundJobService.Verify(x => x.EnqueueAsync(
            It.Is<EnqueuePrintJob>(j => j.PrinterName == "default"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenPedidoNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(999L))
            .ReturnsAsync((Pedido?)null);

        var command = new ImprimirPedidoCommand { PedidoID = 999L };
        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object, _mockBackgroundJobService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockBackgroundJobService.Verify(x => x.EnqueueAsync(It.IsAny<EnqueuePrintJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenEnqueueThrows_ShouldReturnFalse()
    {
        // Arrange
        var pedido = Pedido.Create(1, "3 kg costilla");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(3L))
            .ReturnsAsync(pedido);
        _mockSettings.Setup(x => x.GetValorAsync("Printer_Name"))
            .ReturnsAsync("default");
        _mockBackgroundJobService.Setup(x => x.EnqueueAsync(It.IsAny<EnqueuePrintJob>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Hangfire unavailable"));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ImprimirPedidoCommand { PedidoID = 3L };
        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object, _mockBackgroundJobService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        pedido.EstadoImpresion.Should().BeTrue(); // Still marked as printed in DB
        _mockPedidoRepository.Verify(x => x.UpdateAsync(pedido), Times.Once);
    }

    #endregion
}
