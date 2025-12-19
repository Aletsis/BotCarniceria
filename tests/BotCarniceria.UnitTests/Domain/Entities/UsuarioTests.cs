using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Domain.Entities;

public class UsuarioTests
{
    [Fact]
    public void Create_ShouldCreateUsuarioWithCorrectProperties()
    {
        // Arrange
        var username = "admin";
        var passwordHash = "hashedpassword123";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;
        var telefono = "5551234567";

        // Act
        var usuario = Usuario.Create(username, passwordHash, nombre, rol, telefono);

        // Assert
        usuario.Should().NotBeNull();
        usuario.Username.Should().Be(username);
        usuario.PasswordHash.Should().Be(passwordHash);
        usuario.Nombre.Should().Be(nombre);
        usuario.Rol.Should().Be(rol);
        usuario.Telefono.Should().Be(telefono);
        usuario.Activo.Should().BeTrue();
        usuario.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        usuario.UltimoAcceso.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutTelefono_ShouldCreateUsuarioWithNullTelefono()
    {
        // Arrange
        var username = "admin";
        var passwordHash = "hashedpassword123";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;

        // Act
        var usuario = Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        usuario.Should().NotBeNull();
        usuario.Username.Should().Be(username);
        usuario.Telefono.Should().BeNull();
        usuario.Activo.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyUsername_ShouldThrowArgumentException()
    {
        // Arrange
        var username = "";
        var passwordHash = "hashedpassword123";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;

        // Act
        Action act = () => Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Username requerido");
    }

    [Fact]
    public void Create_WithWhitespaceUsername_ShouldThrowArgumentException()
    {
        // Arrange
        var username = "   ";
        var passwordHash = "hashedpassword123";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;

        // Act
        Action act = () => Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Username requerido");
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ShouldThrowArgumentException()
    {
        // Arrange
        var username = "admin";
        var passwordHash = "";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;

        // Act
        Action act = () => Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash requerido");
    }

    [Fact]
    public void Create_WithWhitespacePasswordHash_ShouldThrowArgumentException()
    {
        // Arrange
        var username = "admin";
        var passwordHash = "   ";
        var nombre = "Administrador";
        var rol = RolUsuario.Admin;

        // Act
        Action act = () => Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash requerido");
    }

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Editor)]
    [InlineData(RolUsuario.Viewer)]
    public void Create_WithDifferentRoles_ShouldCreateUsuarioWithCorrectRole(RolUsuario rol)
    {
        // Arrange
        var username = "testuser";
        var passwordHash = "hashedpassword123";
        var nombre = "Test User";

        // Act
        var usuario = Usuario.Create(username, passwordHash, nombre, rol);

        // Assert
        usuario.Rol.Should().Be(rol);
    }

    [Fact]
    public void ActualizarUltimoAcceso_ShouldSetUltimoAccesoToCurrentTime()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin);

        // Act
        usuario.ActualizarUltimoAcceso();

        // Assert
        usuario.UltimoAcceso.Should().NotBeNull();
        usuario.UltimoAcceso.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ActualizarUltimoAcceso_CalledMultipleTimes_ShouldUpdateToLatestTime()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin);
        usuario.ActualizarUltimoAcceso();
        var primerAcceso = usuario.UltimoAcceso;

        // Act
        System.Threading.Thread.Sleep(100);
        usuario.ActualizarUltimoAcceso();

        // Assert
        usuario.UltimoAcceso.Should().NotBeNull();
        usuario.UltimoAcceso.Should().BeAfter(primerAcceso!.Value);
    }

    [Fact]
    public void CambiarPassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "oldhash123", "Admin", RolUsuario.Admin);
        var nuevoHash = "newhash456";

        // Act
        usuario.CambiarPassword(nuevoHash);

        // Assert
        usuario.PasswordHash.Should().Be(nuevoHash);
    }

    [Fact]
    public void ActualizarDatos_ShouldUpdateNombreTelefonoAndRol()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin Original", RolUsuario.Viewer);
        var nuevoNombre = "Admin Actualizado";
        var nuevoTelefono = "5559876543";
        var nuevoRol = RolUsuario.Admin;

        // Act
        usuario.ActualizarDatos(nuevoNombre, nuevoTelefono, nuevoRol);

        // Assert
        usuario.Nombre.Should().Be(nuevoNombre);
        usuario.Telefono.Should().Be(nuevoTelefono);
        usuario.Rol.Should().Be(nuevoRol);
    }

    [Fact]
    public void ActualizarDatos_WithNullTelefono_ShouldSetTelefonoToNull()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin, "5551234567");
        var nuevoNombre = "Admin Actualizado";
        var nuevoRol = RolUsuario.Supervisor;

        // Act
        usuario.ActualizarDatos(nuevoNombre, null, nuevoRol);

        // Assert
        usuario.Nombre.Should().Be(nuevoNombre);
        usuario.Telefono.Should().BeNull();
        usuario.Rol.Should().Be(nuevoRol);
    }

    [Fact]
    public void ToggleActivo_WithTrue_ShouldSetActivoToTrue()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin);
        usuario.ToggleActivo(false);

        // Act
        usuario.ToggleActivo(true);

        // Assert
        usuario.Activo.Should().BeTrue();
    }

    [Fact]
    public void ToggleActivo_WithFalse_ShouldSetActivoToFalse()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin);

        // Act
        usuario.ToggleActivo(false);

        // Assert
        usuario.Activo.Should().BeFalse();
    }

    [Fact]
    public void Usuario_ShouldHaveCorrectInitialState()
    {
        // Arrange & Act
        var usuario = Usuario.Create("admin", "hash123", "Admin", RolUsuario.Admin);

        // Assert
        usuario.Activo.Should().BeTrue();
        usuario.UltimoAcceso.Should().BeNull();
        usuario.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAllRoles_ShouldMaintainRoleHierarchy()
    {
        // Arrange & Act
        var admin = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin);
        var supervisor = Usuario.Create("supervisor", "hash", "Supervisor", RolUsuario.Supervisor);
        var editor = Usuario.Create("editor", "hash", "Editor", RolUsuario.Editor);
        var viewer = Usuario.Create("viewer", "hash", "Viewer", RolUsuario.Viewer);

        // Assert
        admin.Rol.Should().Be(RolUsuario.Admin);
        supervisor.Rol.Should().Be(RolUsuario.Supervisor);
        editor.Rol.Should().Be(RolUsuario.Editor);
        viewer.Rol.Should().Be(RolUsuario.Viewer);
    }
}
