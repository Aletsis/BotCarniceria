using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Events;
using BotCarniceria.Core.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Domain.Entities;

public class PedidoTests
{
    [Fact]
    public void Create_ShouldCreatePedidoWithCorrectProperties()
    {
        // Arrange
        var clienteId = 1;
        var contenido = "2 kg de carne molida";
        var notas = "Sin grasa";
        var formaPago = "Tarjeta";

        // Act
        var pedido = Pedido.Create(clienteId, contenido, notas, formaPago);

        // Assert
        pedido.Should().NotBeNull();
        pedido.ClienteID.Should().Be(clienteId);
        pedido.Contenido.Should().Be(contenido);
        pedido.Notas.Should().Be(notas);
        pedido.FormaPago.Should().Be(formaPago);
        pedido.Estado.Should().Be(EstadoPedido.EnEspera);
        pedido.EstadoImpresion.Should().BeFalse();
        pedido.FechaImpresion.Should().BeNull();
        pedido.Folio.Should().NotBeNull();
        pedido.Fecha.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldUseDefaults()
    {
        // Arrange
        var clienteId = 1;
        var contenido = "1 kg de bistec";

        // Act
        var pedido = Pedido.Create(clienteId, contenido);

        // Assert
        pedido.Should().NotBeNull();
        pedido.ClienteID.Should().Be(clienteId);
        pedido.Contenido.Should().Be(contenido);
        pedido.Notas.Should().BeNull();
        pedido.FormaPago.Should().Be("Efectivo");
        pedido.Estado.Should().Be(EstadoPedido.EnEspera);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueFolio()
    {
        // Arrange
        var clienteId = 1;
        var contenido = "Test";

        // Act
        var pedido1 = Pedido.Create(clienteId, contenido);
        var pedido2 = Pedido.Create(clienteId, contenido);

        // Assert
        pedido1.Folio.Should().NotBe(pedido2.Folio);
    }

    [Fact]
    public void Create_ShouldRaisePedidoCreatedEvent()
    {
        // Arrange
        var clienteId = 1;
        var contenido = "Test";

        // Act
        var pedido = Pedido.Create(clienteId, contenido);

        // Assert
        var domainEvents = pedido.DomainEvents;
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<PedidoCreatedEvent>();
        
        var createdEvent = (PedidoCreatedEvent)domainEvents.First();
        createdEvent.Pedido.Should().Be(pedido);
    }

    [Fact]
    public void CambiarEstado_ShouldUpdateEstado()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        var nuevoEstado = EstadoPedido.EnRuta;

        // Act
        pedido.CambiarEstado(nuevoEstado);

        // Assert
        pedido.Estado.Should().Be(nuevoEstado);
    }

    [Fact]
    public void CambiarEstado_FromEnEsperaToEnRuta_ShouldSucceed()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");

        // Act
        pedido.CambiarEstado(EstadoPedido.EnRuta);

        // Assert
        pedido.Estado.Should().Be(EstadoPedido.EnRuta);
    }

    [Fact]
    public void CambiarEstado_FromEnRutaToEntregado_ShouldSucceed()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        pedido.CambiarEstado(EstadoPedido.EnRuta);

        // Act
        pedido.CambiarEstado(EstadoPedido.Entregado);

        // Assert
        pedido.Estado.Should().Be(EstadoPedido.Entregado);
    }

    [Fact]
    public void CambiarEstado_FromEnEsperaToEntregado_ShouldSucceed()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");

        // Act
        pedido.CambiarEstado(EstadoPedido.Entregado);

        // Assert
        pedido.Estado.Should().Be(EstadoPedido.Entregado);
    }

    [Fact]
    public void CambiarEstado_FromEntregadoToAnyOtherState_ShouldThrowException()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        pedido.CambiarEstado(EstadoPedido.Entregado);

        // Act
        Action act = () => pedido.CambiarEstado(EstadoPedido.EnEspera);

        // Assert
        act.Should().Throw<InvalidDomainOperationException>()
            .WithMessage("No se puede modificar un pedido que ya ha sido entregado.");
    }

    [Fact]
    public void CambiarEstado_FromEntregadoToCancelado_ShouldThrowException()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        pedido.CambiarEstado(EstadoPedido.Entregado);

        // Act
        Action act = () => pedido.CambiarEstado(EstadoPedido.Cancelado);

        // Assert
        act.Should().Throw<InvalidDomainOperationException>()
            .WithMessage("No se puede modificar un pedido que ya ha sido entregado.");
    }


    [Fact]
    public void MarcarImpreso_ShouldSetEstadoImpresionToTrue()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");

        // Act
        pedido.MarcarImpreso();

        // Assert
        pedido.EstadoImpresion.Should().BeTrue();
        pedido.FechaImpresion.Should().NotBeNull();
        pedido.FechaImpresion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarcarImpreso_CalledMultipleTimes_ShouldUpdateFechaImpresion()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");
        pedido.MarcarImpreso();
        var primeraFecha = pedido.FechaImpresion;

        // Act
        System.Threading.Thread.Sleep(100); // Pequeña pausa para asegurar diferencia de tiempo
        pedido.MarcarImpreso();

        // Assert
        pedido.EstadoImpresion.Should().BeTrue();
        pedido.FechaImpresion.Should().NotBe(primeraFecha);
        pedido.FechaImpresion.Should().BeAfter(primeraFecha!.Value);
    }

    [Theory]
    [InlineData(EstadoPedido.EnEspera)]
    [InlineData(EstadoPedido.EnRuta)]
    [InlineData(EstadoPedido.Entregado)]
    [InlineData(EstadoPedido.Cancelado)]
    public void CambiarEstado_ToValidStates_ShouldSucceed(EstadoPedido nuevoEstado)
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test");

        // Act
        pedido.CambiarEstado(nuevoEstado);

        // Assert
        pedido.Estado.Should().Be(nuevoEstado);
    }

    [Fact]
    public void Pedido_ShouldHaveCorrectInitialState()
    {
        // Arrange & Act
        var pedido = Pedido.Create(1, "Test contenido");

        // Assert
        pedido.Estado.Should().Be(EstadoPedido.EnEspera);
        pedido.EstadoImpresion.Should().BeFalse();
        pedido.FechaImpresion.Should().BeNull();
        pedido.Fecha.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
