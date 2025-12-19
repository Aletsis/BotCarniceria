using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Specifications;

public class PedidoSpecificationsTests
{
    #region PedidosByClienteSpecification Tests

    [Fact]
    public void PedidosByClienteSpecification_ShouldMatchPedidosWithSameClienteId()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");
        var pedido3 = Pedido.Create(1, "Pedido 3");

        var spec = new PedidosByClienteSpecification(1);

        // Act & Assert
        spec.IsSatisfiedBy(pedido1).Should().BeTrue();
        spec.IsSatisfiedBy(pedido2).Should().BeFalse();
        spec.IsSatisfiedBy(pedido3).Should().BeTrue();
    }

    [Fact]
    public void PedidosByClienteSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2"),
            Pedido.Create(1, "Pedido 3"),
            Pedido.Create(3, "Pedido 4")
        };

        var spec = new PedidosByClienteSpecification(1);
        var expression = spec.ToExpression();

        // Act
        var result = pedidos.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.ClienteID.Should().Be(1));
    }

    #endregion

    #region PedidosByEstadoSpecification Tests

    [Fact]
    public void PedidosByEstadoSpecification_ShouldMatchPedidosWithSameEstado()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");
        pedido2.CambiarEstado(EstadoPedido.EnRuta);

        var spec = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);

        // Act & Assert
        spec.IsSatisfiedBy(pedido1).Should().BeTrue();
        spec.IsSatisfiedBy(pedido2).Should().BeFalse();
    }

    [Theory]
    [InlineData(EstadoPedido.EnEspera)]
    [InlineData(EstadoPedido.EnRuta)]
    [InlineData(EstadoPedido.Entregado)]
    [InlineData(EstadoPedido.Cancelado)]
    public void PedidosByEstadoSpecification_ShouldWorkForAllEstados(EstadoPedido estado)
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        pedido.CambiarEstado(estado);

        var spec = new PedidosByEstadoSpecification(estado);

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    #endregion

    #region PedidosByDateRangeSpecification Tests

    [Fact]
    public void PedidosByDateRangeSpecification_ShouldMatchPedidosInRange()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido Enero"),
            Pedido.Create(2, "Pedido Febrero"),
            Pedido.Create(3, "Pedido Enero 2")
        };

        // Simular fechas diferentes (usando reflection para testing)
        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidos[0], new DateTime(2025, 1, 15));
        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidos[1], new DateTime(2025, 2, 15));
        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidos[2], new DateTime(2025, 1, 30));

        var spec = new PedidosByDateRangeSpecification(startDate, endDate);

        // Act & Assert
        spec.IsSatisfiedBy(pedidos[0]).Should().BeTrue();
        spec.IsSatisfiedBy(pedidos[1]).Should().BeFalse();
        spec.IsSatisfiedBy(pedidos[2]).Should().BeTrue();
    }

    [Fact]
    public void PedidosByDateRangeSpecification_ShouldIncludeBoundaryDates()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);

        var pedidoStart = Pedido.Create(1, "Start");
        var pedidoEnd = Pedido.Create(2, "End");

        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidoStart, startDate);
        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidoEnd, endDate);

        var spec = new PedidosByDateRangeSpecification(startDate, endDate);

        // Act & Assert
        spec.IsSatisfiedBy(pedidoStart).Should().BeTrue();
        spec.IsSatisfiedBy(pedidoEnd).Should().BeTrue();
    }

    #endregion

    #region PedidosByFolioSpecification Tests

    [Fact]
    public void PedidosByFolioSpecification_ShouldMatchPedidoWithSameFolio()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");

        var folio1 = pedido1.Folio.Value;
        var spec = new PedidosByFolioSpecification(folio1);

        // Act & Assert
        spec.IsSatisfiedBy(pedido1).Should().BeTrue();
        spec.IsSatisfiedBy(pedido2).Should().BeFalse();
    }

    #endregion

    #region PedidosTodaySpecification Tests

    [Fact]
    public void PedidosTodaySpecification_ShouldMatchTodaysPedidos()
    {
        // Arrange
        var pedidoToday = Pedido.Create(1, "Today");
        var pedidoYesterday = Pedido.Create(2, "Yesterday");

        typeof(Pedido).GetProperty("Fecha")?.SetValue(pedidoYesterday, DateTime.UtcNow.AddDays(-1));

        var spec = new PedidosTodaySpecification();

        // Act & Assert
        spec.IsSatisfiedBy(pedidoToday).Should().BeTrue();
        spec.IsSatisfiedBy(pedidoYesterday).Should().BeFalse();
    }

    #endregion

    #region PedidosPendingSpecification Tests

    [Fact]
    public void PedidosPendingSpecification_ShouldMatchOnlyPendingPedidos()
    {
        // Arrange
        var pedidoPending = Pedido.Create(1, "Pending");
        var pedidoEnRuta = Pedido.Create(2, "En Ruta");
        pedidoEnRuta.CambiarEstado(EstadoPedido.EnRuta);

        var spec = new PedidosPendingSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(pedidoPending).Should().BeTrue();
        spec.IsSatisfiedBy(pedidoEnRuta).Should().BeFalse();
    }

    #endregion

    #region PedidosActiveSpecification Tests

    [Fact]
    public void PedidosActiveSpecification_ShouldExcludeCanceladosAndEntregados()
    {
        // Arrange
        var pedidoEnEspera = Pedido.Create(1, "En Espera");
        var pedidoEnRuta = Pedido.Create(2, "En Ruta");
        var pedidoEntregado = Pedido.Create(3, "Entregado");
        var pedidoCancelado = Pedido.Create(4, "Cancelado");

        pedidoEnRuta.CambiarEstado(EstadoPedido.EnRuta);
        pedidoEntregado.CambiarEstado(EstadoPedido.Entregado);
        pedidoCancelado.CambiarEstado(EstadoPedido.Cancelado);

        var spec = new PedidosActiveSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(pedidoEnEspera).Should().BeTrue();
        spec.IsSatisfiedBy(pedidoEnRuta).Should().BeTrue();
        spec.IsSatisfiedBy(pedidoEntregado).Should().BeFalse();
        spec.IsSatisfiedBy(pedidoCancelado).Should().BeFalse();
    }

    #endregion

    #region PedidosBySearchTermSpecification Tests

    [Fact]
    public void PedidosBySearchTermSpecification_ShouldMatchByContenido()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "2 kg de carne molida");
        var pedido2 = Pedido.Create(2, "1 kg de pollo");

        var spec = new PedidosBySearchTermSpecification("carne");

        // Act & Assert
        spec.IsSatisfiedBy(pedido1).Should().BeTrue();
        spec.IsSatisfiedBy(pedido2).Should().BeFalse();
    }

    [Fact]
    public void PedidosBySearchTermSpecification_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pedido = Pedido.Create(1, "CARNE MOLIDA");
        var spec = new PedidosBySearchTermSpecification("carne");

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    [Fact]
    public void PedidosBySearchTermSpecification_ShouldThrowOnNullSearchTerm()
    {
        // Act & Assert
        Action act = () => new PedidosBySearchTermSpecification(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
