using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using BotCarniceria.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.IntegrationTests.Repositories;

public class RepositoryIntegrationTests : DatabaseTestBase
{
    #region Pedido Repository Tests

    [Fact]
    public async Task PedidoRepository_AddAsync_ShouldPersistToDatabase()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var clienteRepo = new ClienteRepository(DbContext);
        var cliente = Cliente.Create("5550000000", "Test User");
        await clienteRepo.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "2 kg de carne molida");

        // Act
        await repository.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await repository.GetByIdAsync(pedido.PedidoID);
        saved.Should().NotBeNull();
        saved!.Contenido.Should().Be("2 kg de carne molida");
    }

    [Fact]
    public async Task PedidoRepository_FindAsync_WithSpecification_ShouldReturnMatching()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(2, "Pedido 2");
        var pedido3 = Pedido.Create(1, "Pedido 3");

        await repository.AddAsync(pedido1);
        await repository.AddAsync(pedido2);
        await repository.AddAsync(pedido3);
        await DbContext.SaveChangesAsync();

        var spec = new PedidosByClienteSpecification(1);

        // Act
        var result = await repository.FindAsync(spec);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.ClienteID.Should().Be(1));
    }

    [Fact]
    public async Task PedidoRepository_UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var clienteRepo = new ClienteRepository(DbContext);
        var cliente = Cliente.Create("5550000000", "Test User");
        await clienteRepo.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Original");
        await repository.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Act
        pedido.CambiarEstado(EstadoPedido.EnRuta);
        await repository.UpdateAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Assert
        DbContext.Entry(pedido).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        var updated = await repository.GetByIdAsync(pedido.PedidoID);
        updated!.Estado.Should().Be(EstadoPedido.EnRuta);
    }

    [Fact]
    public async Task PedidoRepository_DeleteAsync_ShouldRemoveFromDatabase()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var pedido = Pedido.Create(1, "To Delete");
        await repository.AddAsync(pedido);
        await DbContext.SaveChangesAsync();
        var id = pedido.PedidoID;

        // Act
        await repository.DeleteAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Assert
        var deleted = await repository.GetByIdAsync(id);
        deleted.Should().BeNull();
    }

    #endregion

    #region Cliente Repository Tests

    [Fact]
    public async Task ClienteRepository_AddAsync_ShouldPersistToDatabase()
    {
        // Arrange
        var repository = new ClienteRepository(DbContext);
        var cliente = Cliente.Create("5551234567", "Juan Pérez");

        // Act
        await repository.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await repository.GetByIdAsync(cliente.ClienteID);
        saved.Should().NotBeNull();
        saved!.Nombre.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task ClienteRepository_FindAsync_WithSpecification_ShouldReturnMatching()
    {
        // Arrange
        var repository = new ClienteRepository(DbContext);
        var cliente1 = Cliente.Create("5551111111", "Active");
        var cliente2 = Cliente.Create("5552222222", "Inactive");
        
        cliente2.ToggleActivo(false);

        await repository.AddAsync(cliente1);
        await repository.AddAsync(cliente2);
        await DbContext.SaveChangesAsync();

        var spec = new ClientesActiveSpecification();

        // Act
        var result = await repository.FindAsync(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Activo.Should().BeTrue();
    }

    #endregion

    #region Specification Tests

    [Fact]
    public async Task Repository_CountAsync_WithSpecification_ShouldReturnCorrectCount()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(1, "Pedido 2");
        var pedido3 = Pedido.Create(2, "Pedido 3");

        await repository.AddAsync(pedido1);
        await repository.AddAsync(pedido2);
        await repository.AddAsync(pedido3);
        await DbContext.SaveChangesAsync();

        var spec = new PedidosByClienteSpecification(1);

        // Act
        var count = await repository.CountAsync(spec);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task Repository_AnyAsync_WithSpecification_ShouldReturnCorrectResult()
    {
        // Arrange
        var repository = new OrderRepository(DbContext);
        var pedido = Pedido.Create(1, "Test");
        await repository.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        var specExists = new PedidosByClienteSpecification(1);
        var specNotExists = new PedidosByClienteSpecification(999);

        // Act
        var exists = await repository.AnyAsync(specExists);
        var notExists = await repository.AnyAsync(specNotExists);

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    #endregion
}
