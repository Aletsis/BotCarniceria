using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class DashboardQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly DashboardQueryHandler _handler;

    public DashboardQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockSessionRepository = new Mock<ISessionRepository>();

        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(x => x.Sessions).Returns(_mockSessionRepository.Object);

        _handler = new DashboardQueryHandler(_mockUnitOfWork.Object);
    }

    private static Pedido CreatePedidoWithFecha(DateTime fecha, EstadoPedido estado = EstadoPedido.EnEspera)
    {
        var pedido = Pedido.Create(1, "1 kg arrachera");
        if (estado != EstadoPedido.EnEspera)
        {
            pedido.CambiarEstado(estado);
        }
        typeof(Pedido).GetProperty(nameof(Pedido.Fecha))?.SetValue(pedido, fecha);
        return pedido;
    }

    [Fact]
    public async Task Handle_WithOrdersAndActiveSessions_ReturnsCorrectCounts()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var orders = new List<Pedido>
        {
            CreatePedidoWithFecha(now),
            CreatePedidoWithFecha(now.AddMinutes(-30)),
            CreatePedidoWithFecha(now.AddMinutes(-60))
        };

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosTodaySpecification>()))
            .ReturnsAsync(orders);
        _mockSessionRepository.Setup(x => x.CountAsync(It.IsAny<ActiveSessionsSpecification>()))
            .ReturnsAsync(7);
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosPendingSpecification>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.PedidosHoy.Should().Be(3);
        result.ChatsActivos.Should().Be(7);
        result.IngresosHoy.Should().Be(0);
        result.IngresosTotales.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GeneratesHourlyActivity_GroupedAndOrderedByHour()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var order1 = CreatePedidoWithFecha(today.AddHours(10).AddMinutes(15));
        var order2 = CreatePedidoWithFecha(today.AddHours(10).AddMinutes(45));
        var order3 = CreatePedidoWithFecha(today.AddHours(14).AddMinutes(20));

        var orders = new List<Pedido> { order1, order2, order3 };

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosTodaySpecification>()))
            .ReturnsAsync(orders);
        _mockSessionRepository.Setup(x => x.CountAsync(It.IsAny<ActiveSessionsSpecification>()))
            .ReturnsAsync(0);
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosPendingSpecification>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.ActividadPorHora.Should().NotBeEmpty();
        
        var hour10Local = $"{order1.Fecha.ToLocalTime().Hour:00}:00";
        var hour14Local = $"{order3.Fecha.ToLocalTime().Hour:00}:00";

        var activity10 = result.ActividadPorHora.FirstOrDefault(a => a.Hora == hour10Local);
        activity10.Should().NotBeNull();
        activity10!.CantidadPedidos.Should().Be(2);

        var activity14 = result.ActividadPorHora.FirstOrDefault(a => a.Hora == hour14Local);
        activity14.Should().NotBeNull();
        activity14!.CantidadPedidos.Should().Be(1);

        // Activity list should be sorted by hour
        result.ActividadPorHora.Should().BeInAscendingOrder(a => a.Hora);
    }

    [Fact]
    public async Task Handle_WhenPendingOrdersOlderThan15MinutesExist_AddsCriticalAlert()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var delayedOrder1 = CreatePedidoWithFecha(now.AddMinutes(-20)); // > 15m
        var delayedOrder2 = CreatePedidoWithFecha(now.AddMinutes(-40)); // > 15m
        var recentOrder = CreatePedidoWithFecha(now.AddMinutes(-5));    // <= 15m

        var pendingOrders = new List<Pedido> { delayedOrder1, delayedOrder2, recentOrder };

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosTodaySpecification>()))
            .ReturnsAsync(new List<Pedido>());
        _mockSessionRepository.Setup(x => x.CountAsync(It.IsAny<ActiveSessionsSpecification>()))
            .ReturnsAsync(0);
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosPendingSpecification>()))
            .ReturnsAsync(pendingOrders);

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.AlertasCriticas.Should().HaveCount(1);
        result.AlertasCriticas[0].Should().Be("2 pedidos en espera por más de 15 min.");
    }

    [Fact]
    public async Task Handle_WhenPendingOrdersAreRecent_DoesNotAddCriticalAlert()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var recentOrder1 = CreatePedidoWithFecha(now.AddMinutes(-5));
        var recentOrder2 = CreatePedidoWithFecha(now.AddMinutes(-10));

        var pendingOrders = new List<Pedido> { recentOrder1, recentOrder2 };

        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosTodaySpecification>()))
            .ReturnsAsync(new List<Pedido>());
        _mockSessionRepository.Setup(x => x.CountAsync(It.IsAny<ActiveSessionsSpecification>()))
            .ReturnsAsync(0);
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosPendingSpecification>()))
            .ReturnsAsync(pendingOrders);

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.AlertasCriticas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDatabaseIsEmpty_ReturnsZeroStatsAndEmptyCollections()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosTodaySpecification>()))
            .ReturnsAsync(new List<Pedido>());
        _mockSessionRepository.Setup(x => x.CountAsync(It.IsAny<ActiveSessionsSpecification>()))
            .ReturnsAsync(0);
        _mockOrderRepository.Setup(x => x.FindAsync(It.IsAny<PedidosPendingSpecification>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        result.PedidosHoy.Should().Be(0);
        result.ChatsActivos.Should().Be(0);
        result.IngresosHoy.Should().Be(0);
        result.IngresosTotales.Should().Be(0);
        result.ActividadPorHora.Should().BeEmpty();
        result.AlertasCriticas.Should().BeEmpty();
    }
}
