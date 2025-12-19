using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Specifications;

public class ClienteSpecificationsTests
{
    #region ClienteByPhoneNumberSpecification Tests

    [Fact]
    public void ClienteByPhoneNumberSpecification_ShouldMatchClienteWithSamePhone()
    {
        // Arrange
        var cliente1 = Cliente.Create("5551234567", "Juan Pérez");
        var cliente2 = Cliente.Create("5559876543", "María García");

        var spec = new ClienteByPhoneNumberSpecification("5551234567");

        // Act & Assert
        spec.IsSatisfiedBy(cliente1).Should().BeTrue();
        spec.IsSatisfiedBy(cliente2).Should().BeFalse();
    }

    [Fact]
    public void ClienteByPhoneNumberSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551111111", "Cliente 1"),
            Cliente.Create("5552222222", "Cliente 2"),
            Cliente.Create("5553333333", "Cliente 3")
        };

        var spec = new ClienteByPhoneNumberSpecification("5552222222");
        var expression = spec.ToExpression();

        // Act
        var result = clientes.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().NumeroTelefono.Should().Be("5552222222");
    }

    #endregion

    #region ClientesActiveSpecification Tests

    [Fact]
    public void ClientesActiveSpecification_ShouldMatchOnlyActiveClientes()
    {
        // Arrange
        var clienteActive = Cliente.Create("5551234567", "Active");
        var clienteInactive = Cliente.Create("5559876543", "Inactive");
        clienteInactive.ToggleActivo(false);

        var spec = new ClientesActiveSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(clienteActive).Should().BeTrue();
        spec.IsSatisfiedBy(clienteInactive).Should().BeFalse();
    }

    [Fact]
    public void ClientesActiveSpecification_ShouldFilterActiveClientes()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551111111", "Active 1"),
            Cliente.Create("5552222222", "Inactive 1"),
            Cliente.Create("5553333333", "Active 2"),
            Cliente.Create("5554444444", "Inactive 2")
        };

        clientes[1].ToggleActivo(false);
        clientes[3].ToggleActivo(false);

        var spec = new ClientesActiveSpecification();
        var expression = spec.ToExpression();

        // Act
        var result = clientes.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.Activo.Should().BeTrue());
    }

    #endregion

    #region ClientesBySearchTermSpecification Tests

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldMatchByNombre()
    {
        // Arrange
        var cliente1 = Cliente.Create("5551234567", "Juan Pérez");
        var cliente2 = Cliente.Create("5559876543", "María García");

        var spec = new ClientesBySearchTermSpecification("juan");

        // Act & Assert
        spec.IsSatisfiedBy(cliente1).Should().BeTrue();
        spec.IsSatisfiedBy(cliente2).Should().BeFalse();
    }

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldMatchByPhoneNumber()
    {
        // Arrange
        var cliente1 = Cliente.Create("5551234567", "Juan Pérez");
        var cliente2 = Cliente.Create("5559876543", "María García");

        var spec = new ClientesBySearchTermSpecification("555123");

        // Act & Assert
        spec.IsSatisfiedBy(cliente1).Should().BeTrue();
        spec.IsSatisfiedBy(cliente2).Should().BeFalse();
    }

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldBeCaseInsensitive()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "JUAN PÉREZ");
        var spec = new ClientesBySearchTermSpecification("juan");

        // Act & Assert
        spec.IsSatisfiedBy(cliente).Should().BeTrue();
    }

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldMatchPartialNombre()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Carlos Pérez");
        var spec = new ClientesBySearchTermSpecification("carlos");

        // Act & Assert
        spec.IsSatisfiedBy(cliente).Should().BeTrue();
    }

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldThrowOnNullSearchTerm()
    {
        // Act & Assert
        Action act = () => new ClientesBySearchTermSpecification(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClientesBySearchTermSpecification_ShouldWorkWithLinq()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551111111", "Juan Pérez"),
            Cliente.Create("5552222222", "María García"),
            Cliente.Create("5553333333", "Pedro López"),
            Cliente.Create("5554444444", "Ana Martínez")
        };

        var spec = new ClientesBySearchTermSpecification("pérez");
        var expression = spec.ToExpression();

        // Act
        var result = clientes.AsQueryable().Where(expression).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Juan Pérez");
    }

    #endregion
}
