using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Persistence.Repositories;

public class ClienteRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly ClienteRepository _repository;

    public ClienteRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new ClienteRepository(_context);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithExistingPhone_ShouldReturnCliente()
    {
        // Arrange
        var phone = "5551234567";
        var cliente = Cliente.Create(phone, "Juan Pérez", "Calle Principal 123");
        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone);

        // Assert
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be(phone);
        result.Nombre.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task GetByPhoneAsync_WithNonExistingPhone_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByPhoneAsync("9999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPhoneAsync_WithMultipleClientes_ShouldReturnCorrectOne()
    {
        // Arrange
        var cliente1 = Cliente.Create("5551111111", "Cliente 1", "Dirección 1");
        var cliente2 = Cliente.Create("5552222222", "Cliente 2", "Dirección 2");
        var cliente3 = Cliente.Create("5553333333", "Cliente 3", "Dirección 3");

        await _context.Clientes.AddRangeAsync(cliente1, cliente2, cliente3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync("5552222222");

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Cliente 2");
    }

    [Fact]
    public async Task AddAsync_ShouldAddCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");

        // Act
        await _repository.AddAsync(cliente);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Clientes.FindAsync(cliente.ClienteID);
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be("5551234567");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Original Name", "Original Address");
        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();

        // Act
        cliente.UpdateNombre("Updated Name");
        cliente.UpdateDireccion("Updated Address");
        await _repository.UpdateAsync(cliente);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Clientes.FindAsync(cliente.ClienteID);
        result!.Nombre.Should().Be("Updated Name");
        result.Direccion.Should().Be("Updated Address");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(cliente);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Clientes.FindAsync(cliente.ClienteID);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPhoneAsync_WithInactiveCliente_ShouldReturnCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        cliente.ToggleActivo(false);
        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync("5551234567");

        // Assert
        result.Should().NotBeNull();
        result!.Activo.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
