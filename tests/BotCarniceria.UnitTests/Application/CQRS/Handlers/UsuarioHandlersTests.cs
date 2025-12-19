using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

using Microsoft.Extensions.Logging;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class UsuarioHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUsuarioRepository> _mockUsuarioRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ILogger<UsuarioHandlers>> _mockLogger;
    private readonly UsuarioHandlers _handler;

    public UsuarioHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUsuarioRepository = new Mock<IUsuarioRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockLogger = new Mock<ILogger<UsuarioHandlers>>();
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUsuarioRepository.Object);
        _handler = new UsuarioHandlers(_mockUnitOfWork.Object, _mockPasswordHasher.Object, _mockLogger.Object);
    }

    #region GetAllUsuariosQuery Tests

    [Fact]
    public async Task GetAllUsuariosQuery_ShouldReturnAllUsuarios()
    {
        // Arrange
        var usuarios = new List<Usuario>
        {
            Usuario.Create("admin", "hash1", "Administrador", RolUsuario.Admin),
            Usuario.Create("editor", "hash2", "Editor", RolUsuario.Editor),
            Usuario.Create("viewer", "hash3", "Viewer", RolUsuario.Viewer)
        };

        _mockUsuarioRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(usuarios);

        var query = new GetAllUsuariosQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.NombreUsuario == "admin");
        result.Should().Contain(u => u.NombreUsuario == "editor");
        result.Should().Contain(u => u.NombreUsuario == "viewer");
    }

    [Fact]
    public async Task GetAllUsuariosQuery_WhenNoUsuarios_ShouldReturnEmptyList()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Usuario>());

        var query = new GetAllUsuariosQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUsuarioByIdQuery Tests

    [Fact]
    public async Task GetUsuarioByIdQuery_WhenUsuarioExists_ShouldReturnUsuarioDto()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash123", "Administrador", RolUsuario.Admin, "5551234567");
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(usuario);

        var query = new GetUsuarioByIdQuery(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.NombreUsuario.Should().Be("admin");
        result.NombreCompleto.Should().Be("Administrador");
        result.Rol.Should().Be(RolUsuario.Admin.ToString());
    }

    [Fact]
    public async Task GetUsuarioByIdQuery_WhenUsuarioNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Usuario?)null);

        var query = new GetUsuarioByIdQuery(999);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region LoginUserQuery Tests

    [Fact]
    public async Task LoginUserQuery_WithValidCredentials_ShouldReturnUsuarioDto()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hashedpassword", "Administrador", RolUsuario.Admin);
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(usuario);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("password123", "hashedpassword"))
            .Returns(true);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new LoginUserQuery("admin", "password123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.NombreUsuario.Should().Be("admin");
        _mockUsuarioRepository.Verify(x => x.UpdateAsync(It.IsAny<Usuario>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginUserQuery_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hashedpassword", "Administrador", RolUsuario.Admin);
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(usuario);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("wrongpassword", "hashedpassword"))
            .Returns(false);

        var query = new LoginUserQuery("admin", "wrongpassword");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        // Assert
        result.Should().BeNull();
        _mockUsuarioRepository.Verify(x => x.UpdateAsync(It.IsAny<Usuario>()), Times.Once); // Now we update to record failed attempt
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginUserQuery_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);

        var query = new LoginUserQuery("nonexistent", "password123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserQuery_WithInactiveUser_ShouldReturnNull()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hashedpassword", "Administrador", RolUsuario.Admin);
        usuario.ToggleActivo(false);
        
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync("admin"))
            .ReturnsAsync(usuario);

        var query = new LoginUserQuery("admin", "password123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateUsuarioCommand Tests

    [Fact]
    public async Task CreateUsuarioCommand_ShouldCreateUsuarioSuccessfully()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((Usuario?)null);
        _mockPasswordHasher.Setup(x => x.HashPassword("password123"))
            .Returns("hashedpassword");
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateUsuarioCommand("newuser", "password123", "Nuevo Usuario", RolUsuario.Editor, "5551234567");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockPasswordHasher.Verify(x => x.HashPassword("password123"), Times.Once);
        _mockUsuarioRepository.Verify(x => x.AddAsync(It.IsAny<Usuario>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUsuarioCommand_WhenUsernameExists_ShouldReturnFalse()
    {
        // Arrange
        var existingUser = Usuario.Create("existinguser", "hash", "Existing", RolUsuario.Viewer);
        _mockUsuarioRepository.Setup(x => x.GetByUsernameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        var command = new CreateUsuarioCommand("existinguser", "password123", "Test", RolUsuario.Editor, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockUsuarioRepository.Verify(x => x.AddAsync(It.IsAny<Usuario>()), Times.Never);
    }

    #endregion

    #region UpdateUsuarioCommand Tests

    [Fact]
    public async Task UpdateUsuarioCommand_ShouldUpdateUsuarioSuccessfully()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash", "Admin Original", RolUsuario.Viewer);
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(usuario);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateUsuarioCommand(1, "Admin Actualizado", RolUsuario.Admin, "5559876543");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        usuario.Nombre.Should().Be("Admin Actualizado");
        usuario.Telefono.Should().Be("5559876543");
        usuario.Rol.Should().Be(RolUsuario.Admin);
        _mockUsuarioRepository.Verify(x => x.UpdateAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUsuarioCommand_WhenUsuarioNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Usuario?)null);

        var command = new UpdateUsuarioCommand(999, "Test", RolUsuario.Admin, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockUsuarioRepository.Verify(x => x.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    #endregion

    #region ToggleUsuarioActivoCommand Tests

    [Fact]
    public async Task ToggleUsuarioActivoCommand_ShouldActivateUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin);
        usuario.ToggleActivo(false);
        
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(usuario);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleUsuarioActivoCommand(1, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        usuario.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleUsuarioActivoCommand_WhenUsuarioNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Usuario?)null);

        var command = new ToggleUsuarioActivoCommand(999, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ChangePasswordCommand Tests

    [Fact]
    public async Task ChangePasswordCommand_ShouldChangePasswordSuccessfully()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "oldhash", "Admin", RolUsuario.Admin);
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(usuario);
        _mockPasswordHasher.Setup(x => x.HashPassword("newpassword123"))
            .Returns("newhash");
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ChangePasswordCommand(1, "newpassword123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        usuario.PasswordHash.Should().Be("newhash");
        _mockPasswordHasher.Verify(x => x.HashPassword("newpassword123"), Times.Once);
        _mockUsuarioRepository.Verify(x => x.UpdateAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordCommand_WhenUsuarioNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockUsuarioRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Usuario?)null);

        var command = new ChangePasswordCommand(999, "newpassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockPasswordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    #endregion
}
