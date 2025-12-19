using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Persistence.Repositories;

public class UsuarioRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly UsuarioRepository _repository;

    public UsuarioRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new UsuarioRepository(_context);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ShouldReturnUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("testuser", "hashedpassword", "Test User", RolUsuario.Admin);
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Nombre.Should().Be("Test User");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistingUsername_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_WithMultipleUsuarios_ShouldReturnCorrectOne()
    {
        // Arrange
        var usuario1 = Usuario.Create("user1", "hash1", "User 1", RolUsuario.Admin);
        var usuario2 = Usuario.Create("user2", "hash2", "User 2", RolUsuario.Editor);
        var usuario3 = Usuario.Create("user3", "hash3", "User 3", RolUsuario.Editor);

        await _context.Usuarios.AddRangeAsync(usuario1, usuario2, usuario3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUsernameAsync("user2");

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("User 2");
        result.Rol.Should().Be(RolUsuario.Editor);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("newuser", "hashedpass", "New User", RolUsuario.Editor, "5551234567");

        // Act
        await _repository.AddAsync(usuario);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Usuarios.FindAsync(usuario.UsuarioID);
        result.Should().NotBeNull();
        result!.Username.Should().Be("newuser");
        result.Telefono.Should().Be("5551234567");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("testuser", "hashedpass", "Original Name", RolUsuario.Editor);
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        // Act
        usuario.ActualizarDatos("Updated Name", null, RolUsuario.Editor);
        await _repository.UpdateAsync(usuario);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Usuarios.FindAsync(usuario.UsuarioID);
        result!.Nombre.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("testuser", "hashedpass", "Test User", RolUsuario.Editor);
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(usuario);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Usuarios.FindAsync(usuario.UsuarioID);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_WithInactiveUsuario_ShouldReturnUsuario()
    {
        // Arrange
        var usuario = Usuario.Create("testuser", "hashedpass", "Test User", RolUsuario.Editor);
        usuario.ToggleActivo(false);
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Activo.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
