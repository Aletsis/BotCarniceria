using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Specifications;

public class CompositeSpecificationsTests
{
    #region AndSpecification Tests

    [Fact]
    public void AndSpecification_ShouldMatchWhenBothSpecificationsMatch()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        
        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);
        var andSpec = new AndSpecification<Pedido>(specCliente, specEstado);

        // Act & Assert
        andSpec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    [Fact]
    public void AndSpecification_ShouldNotMatchWhenFirstSpecificationFails()
    {
        // Arrange
        var pedido = Pedido.Create(2, "Carne molida"); // Different cliente
        
        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);
        var andSpec = new AndSpecification<Pedido>(specCliente, specEstado);

        // Act & Assert
        andSpec.IsSatisfiedBy(pedido).Should().BeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldNotMatchWhenSecondSpecificationFails()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        pedido.CambiarEstado(EstadoPedido.EnRuta); // Different estado
        
        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);
        var andSpec = new AndSpecification<Pedido>(specCliente, specEstado);

        // Act & Assert
        andSpec.IsSatisfiedBy(pedido).Should().BeFalse();
    }

    [Fact]
    public void AndSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"), // Cliente 1, EnEspera
            Pedido.Create(2, "Pedido 2"), // Cliente 2, EnEspera
            Pedido.Create(1, "Pedido 3")  // Cliente 1, EnEspera
        };
        pedidos[1].CambiarEstado(EstadoPedido.EnRuta);

        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);
        var andSpec = new AndSpecification<Pedido>(specCliente, specEstado);
        var expression = andSpec.ToExpression();

        // Act
        var result = pedidos.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p =>
        {
            p.ClienteID.Should().Be(1);
            p.Estado.Should().Be(EstadoPedido.EnEspera);
        });
    }

    [Fact]
    public void AndSpecification_ShouldThrowWhenLeftIsNull()
    {
        // Arrange
        var spec = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);

        // Act & Assert
        Action act = () => new AndSpecification<Pedido>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AndSpecification_ShouldThrowWhenRightIsNull()
    {
        // Arrange
        var spec = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);

        // Act & Assert
        Action act = () => new AndSpecification<Pedido>(spec, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region OrSpecification Tests

    [Fact]
    public void OrSpecification_ShouldMatchWhenEitherSpecificationMatches()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");
        pedido2.CambiarEstado(EstadoPedido.EnRuta);

        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnRuta);
        var orSpec = new OrSpecification<Pedido>(specCliente, specEstado);

        // Act & Assert
        orSpec.IsSatisfiedBy(pedido1).Should().BeTrue(); // Matches cliente
        orSpec.IsSatisfiedBy(pedido2).Should().BeTrue(); // Matches estado
    }

    [Fact]
    public void OrSpecification_ShouldNotMatchWhenBothSpecificationsFail()
    {
        // Arrange
        var pedido = Pedido.Create(3, "Pedido");
        
        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnRuta);
        var orSpec = new OrSpecification<Pedido>(specCliente, specEstado);

        // Act & Assert
        orSpec.IsSatisfiedBy(pedido).Should().BeFalse();
    }

    [Fact]
    public void OrSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"), // Cliente 1, EnEspera
            Pedido.Create(2, "Pedido 2"), // Cliente 2, EnEspera
            Pedido.Create(3, "Pedido 3")  // Cliente 3, EnEspera
        };
        pedidos[1].CambiarEstado(EstadoPedido.EnRuta);

        var specCliente = new PedidosByClienteSpecification(1);
        var specEstado = new PedidosByEstadoSpecification(EstadoPedido.EnRuta);
        var orSpec = new OrSpecification<Pedido>(specCliente, specEstado);
        var expression = orSpec.ToExpression();

        // Act
        var result = pedidos.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2); // Pedido 1 (cliente 1) and Pedido 2 (EnRuta)
    }

    [Fact]
    public void OrSpecification_ShouldThrowWhenLeftIsNull()
    {
        // Arrange
        var spec = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);

        // Act & Assert
        Action act = () => new OrSpecification<Pedido>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrSpecification_ShouldThrowWhenRightIsNull()
    {
        // Arrange
        var spec = new PedidosByEstadoSpecification(EstadoPedido.EnEspera);

        // Act & Assert
        Action act = () => new OrSpecification<Pedido>(spec, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region NotSpecification Tests

    [Fact]
    public void NotSpecification_ShouldInvertSpecificationResult()
    {
        // Arrange
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");

        var spec = new PedidosByClienteSpecification(1);
        var notSpec = new NotSpecification<Pedido>(spec);

        // Act & Assert
        notSpec.IsSatisfiedBy(pedido1).Should().BeFalse(); // Was true, now false
        notSpec.IsSatisfiedBy(pedido2).Should().BeTrue();  // Was false, now true
    }

    [Fact]
    public void NotSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(1, "Pedido 2"),
            Pedido.Create(2, "Pedido 3"),
            Pedido.Create(3, "Pedido 4")
        };

        var spec = new PedidosByClienteSpecification(1);
        var notSpec = new NotSpecification<Pedido>(spec);
        var expression = notSpec.ToExpression();

        // Act
        var result = pedidos.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2); // All except cliente 1
        result.Should().AllSatisfy(p => p.ClienteID.Should().NotBe(1));
    }

    [Fact]
    public void NotSpecification_ShouldThrowWhenSpecificationIsNull()
    {
        // Act & Assert
        Action act = () => new NotSpecification<Pedido>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void Specification_FluentAnd_ShouldWorkCorrectly()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        
        var spec = new PedidosByClienteSpecification(1)
            .And(new PedidosByEstadoSpecification(EstadoPedido.EnEspera));

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    [Fact]
    public void Specification_FluentOr_ShouldWorkCorrectly()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        
        var spec = new PedidosByClienteSpecification(2) // Doesn't match
            .Or(new PedidosByEstadoSpecification(EstadoPedido.EnEspera)); // Matches

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    [Fact]
    public void Specification_FluentNot_ShouldWorkCorrectly()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        
        var spec = new PedidosByClienteSpecification(2).Not();

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue(); // Not cliente 2
    }

    [Fact]
    public void Specification_ComplexFluentCombination_ShouldWorkCorrectly()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Carne molida");
        
        // (Cliente == 1 AND Estado == EnEspera) OR (Estado == EnRuta)
        var spec = new PedidosByClienteSpecification(1)
            .And(new PedidosByEstadoSpecification(EstadoPedido.EnEspera))
            .Or(new PedidosByEstadoSpecification(EstadoPedido.EnRuta));

        // Act & Assert
        spec.IsSatisfiedBy(pedido).Should().BeTrue();
    }

    [Fact]
    public void Specification_ImplicitConversionToExpression_ShouldWork()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2")
        };

        Specification<Pedido> spec = new PedidosByClienteSpecification(1);

        // Act - Implicit conversion to Expression<Func<Pedido, bool>>
        var result = pedidos.AsQueryable().Where(spec).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().ClienteID.Should().Be(1);
    }

    #endregion
}
