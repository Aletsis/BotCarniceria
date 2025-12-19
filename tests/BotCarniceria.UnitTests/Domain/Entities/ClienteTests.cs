using BotCarniceria.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Domain.Entities;

public class ClienteTests
{
    [Fact]
    public void Create_ShouldCreateClienteWithCorrectProperties()
    {
        // Arrange
        var numeroTelefono = "5551234567";
        var nombre = "Juan Pérez";
        var direccion = "Calle Principal 123";

        // Act
        var cliente = Cliente.Create(numeroTelefono, nombre, direccion);

        // Assert
        cliente.Should().NotBeNull();
        cliente.NumeroTelefono.Should().Be(numeroTelefono);
        cliente.Nombre.Should().Be(nombre);
        cliente.Direccion.Should().Be(direccion);
        cliente.Activo.Should().BeTrue();
        cliente.FechaAlta.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithoutDireccion_ShouldCreateClienteWithNullDireccion()
    {
        // Arrange
        var numeroTelefono = "5551234567";
        var nombre = "Juan Pérez";

        // Act
        var cliente = Cliente.Create(numeroTelefono, nombre);

        // Assert
        cliente.Should().NotBeNull();
        cliente.NumeroTelefono.Should().Be(numeroTelefono);
        cliente.Nombre.Should().Be(nombre);
        cliente.Direccion.Should().BeNull();
        cliente.Activo.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyTelefono_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "";
        var nombre = "Juan Pérez";

        // Act
        Action act = () => Cliente.Create(numeroTelefono, nombre);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Teléfono requerido");
    }

    [Fact]
    public void Create_WithWhitespaceTelefono_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "   ";
        var nombre = "Juan Pérez";

        // Act
        Action act = () => Cliente.Create(numeroTelefono, nombre);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Teléfono requerido");
    }

    [Fact]
    public void Create_WithEmptyNombre_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "5551234567";
        var nombre = "";

        // Act
        Action act = () => Cliente.Create(numeroTelefono, nombre);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Nombre requerido");
    }

    [Fact]
    public void Create_WithWhitespaceNombre_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "5551234567";
        var nombre = "   ";

        // Act
        Action act = () => Cliente.Create(numeroTelefono, nombre);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Nombre requerido");
    }

    [Fact]
    public void UpdateDireccion_ShouldUpdateDireccion()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        var nuevaDireccion = "Nueva Calle 456";

        // Act
        cliente.UpdateDireccion(nuevaDireccion);

        // Assert
        cliente.Direccion.Should().Be(nuevaDireccion);
    }

    [Fact]
    public void UpdateNombre_WithValidNombre_ShouldUpdateNombre()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        var nuevoNombre = "Juan Carlos Pérez";

        // Act
        cliente.UpdateNombre(nuevoNombre);

        // Assert
        cliente.Nombre.Should().Be(nuevoNombre);
    }

    [Fact]
    public void UpdateNombre_WithEmptyNombre_ShouldNotUpdateNombre()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        var nombreOriginal = cliente.Nombre;

        // Act
        cliente.UpdateNombre("");

        // Assert
        cliente.Nombre.Should().Be(nombreOriginal);
    }

    [Fact]
    public void UpdateNombre_WithWhitespaceNombre_ShouldNotUpdateNombre()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        var nombreOriginal = cliente.Nombre;

        // Act
        cliente.UpdateNombre("   ");

        // Assert
        cliente.Nombre.Should().Be(nombreOriginal);
    }

    [Fact]
    public void ActualizarDatos_ShouldUpdateNombreAndDireccion()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        var nuevoNombre = "Juan Carlos Pérez";
        var nuevaDireccion = "Nueva Calle 789";

        // Act
        cliente.ActualizarDatos(nuevoNombre, nuevaDireccion);

        // Assert
        cliente.Nombre.Should().Be(nuevoNombre);
        cliente.Direccion.Should().Be(nuevaDireccion);
    }

    [Fact]
    public void ActualizarDatos_WithNullDireccion_ShouldSetDireccionToNull()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle Original");
        var nuevoNombre = "Juan Carlos Pérez";

        // Act
        cliente.ActualizarDatos(nuevoNombre, null);

        // Assert
        cliente.Nombre.Should().Be(nuevoNombre);
        cliente.Direccion.Should().BeNull();
    }

    [Fact]
    public void ToggleActivo_WithTrue_ShouldSetActivoToTrue()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        cliente.ToggleActivo(false);

        // Act
        cliente.ToggleActivo(true);

        // Assert
        cliente.Activo.Should().BeTrue();
    }

    [Fact]
    public void ToggleActivo_WithFalse_ShouldSetActivoToFalse()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");

        // Act
        cliente.ToggleActivo(false);

        // Assert
        cliente.Activo.Should().BeFalse();
    }

    [Fact]
    public void Pedidos_ShouldBeEmptyOnCreation()
    {
        // Arrange & Act
        var cliente = Cliente.Create("5551234567", "Juan Pérez");

        // Assert
        cliente.Pedidos.Should().BeEmpty();
    }

    [Fact]
    public void Pedidos_ShouldBeReadOnly()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");

        // Act & Assert
        cliente.Pedidos.Should().BeAssignableTo<IReadOnlyCollection<Pedido>>();
    }
}
