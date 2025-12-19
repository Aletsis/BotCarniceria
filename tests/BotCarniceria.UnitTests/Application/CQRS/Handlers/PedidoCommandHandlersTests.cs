using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.Interfaces;
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

    public PedidoCommandHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPedidoRepository = new Mock<IOrderRepository>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();

        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockPedidoRepository.Object);
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

/*
    #region ImprimirPedidoCommandHandler Tests

    [Fact]
    public async Task ImprimirPedidoCommandHandler_ShouldMarkPedidoAsImpreso()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle 123");
        
        var clienteProperty = typeof(Pedido).GetProperty("Cliente");
        clienteProperty?.SetValue(pedido, cliente);

        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new ImprimirPedidoCommand
        {
            PedidoID = 1
        };

        var handler = new ImprimirPedidoCommandHandler(
            _mockUnitOfWork.Object,
            _mockPrintingService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.EstadoImpresion.Should().BeTrue();
        pedido.FechaImpresion.Should().NotBeNull();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WithPrintingService_ShouldCallPrintTicket()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle 123");
        
        var clienteProperty = typeof(Pedido).GetProperty("Cliente");
        clienteProperty?.SetValue(pedido, cliente);

        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new ImprimirPedidoCommand
        {
            PedidoID = 1
        };

        var handler = new ImprimirPedidoCommandHandler(
            _mockUnitOfWork.Object,
            _mockPrintingService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockPrintingService.Verify(
            x => x.PrintTicketAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WithoutPrintingService_ShouldStillMarkAsImpreso()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var command = new ImprimirPedidoCommand
        {
            PedidoID = 1
        };

        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.EstadoImpresion.Should().BeTrue();
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenPrintingFails_ShouldStillReturnTrue()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle 123");
        
        var clienteProperty = typeof(Pedido).GetProperty("Cliente");
        clienteProperty?.SetValue(pedido, cliente);

        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        _mockPrintingService.Setup(x => x.PrintTicketAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Printer error"));

        var command = new ImprimirPedidoCommand
        {
            PedidoID = 1
        };

        var handler = new ImprimirPedidoCommandHandler(
            _mockUnitOfWork.Object,
            _mockPrintingService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        pedido.EstadoImpresion.Should().BeTrue();
    }

    [Fact]
    public async Task ImprimirPedidoCommandHandler_WhenPedidoNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Pedido?)null);

        var command = new ImprimirPedidoCommand
        {
            PedidoID = 999
        };

        var handler = new ImprimirPedidoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPedidoRepository.Verify(x => x.UpdateAsync(It.IsAny<Pedido>()), Times.Never);
    }

    #endregion
*/
}
