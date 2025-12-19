using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BotCarniceria.IntegrationTests.Repositories;

public class UnitOfWorkIntegrationTests : DatabaseTestBase
{
    #region Transaction Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldCommitAllChangesInSingleTransaction()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var cliente = Cliente.Create("5551234567", "Test Cliente");
        var pedido = Pedido.Create(1, "Test Pedido");

        // Act
        await unitOfWork.Clientes.AddAsync(cliente);
        await unitOfWork.Orders.AddAsync(pedido);
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(2); // 2 entities saved
        
        var savedCliente = await unitOfWork.Clientes.GetByIdAsync(cliente.ClienteID);
        var savedPedido = await unitOfWork.Orders.GetByIdAsync(pedido.PedidoID);
        
        savedCliente.Should().NotBeNull();
        savedPedido.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleRepositories_ShouldShareSameContext()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var cliente = Cliente.Create("5551234567", "Test Cliente");
        var usuario = Usuario.Create("admin", "hash123", "Admin User", RolUsuario.Admin);

        // Act
        await unitOfWork.Clientes.AddAsync(cliente);
        await unitOfWork.Users.AddAsync(usuario);
        
        // Verify they're tracked before save
        DbContext.ChangeTracker.Entries().Should().HaveCount(2);
        
        await unitOfWork.SaveChangesAsync();

        // Assert
        var savedCliente = await unitOfWork.Clientes.GetByIdAsync(cliente.ClienteID);
        var savedUsuario = await unitOfWork.Users.GetByIdAsync(usuario.UsuarioID);
        
        savedCliente.Should().NotBeNull();
        savedUsuario.Should().NotBeNull();
    }

    [Fact]
    public async Task ComplexTransaction_WithMultipleUpdates_ShouldMaintainConsistency()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        
        var cliente = Cliente.Create("5551234567", "Original Name");
        var pedido1 = Pedido.Create(1, "Pedido 1");
        var pedido2 = Pedido.Create(1, "Pedido 2");

        await unitOfWork.Clientes.AddAsync(cliente);
        await unitOfWork.Orders.AddAsync(pedido1);
        await unitOfWork.Orders.AddAsync(pedido2);
        await unitOfWork.SaveChangesAsync();

        // Act - Update multiple entities in one transaction
        cliente.ActualizarDatos("Updated Name", "New Address");
        pedido1.CambiarEstado(EstadoPedido.EnRuta);
        pedido2.CambiarEstado(EstadoPedido.Entregado);

        await unitOfWork.Clientes.UpdateAsync(cliente);
        await unitOfWork.Orders.UpdateAsync(pedido1);
        await unitOfWork.Orders.UpdateAsync(pedido2);
        
        var changesCount = await unitOfWork.SaveChangesAsync();

        // Assert
        changesCount.Should().Be(3);
        
        // Detach and reload to verify persistence
        DbContext.Entry(cliente).State = EntityState.Detached;
        DbContext.Entry(pedido1).State = EntityState.Detached;
        DbContext.Entry(pedido2).State = EntityState.Detached;
        
        var updatedCliente = await unitOfWork.Clientes.GetByIdAsync(cliente.ClienteID);
        var updatedPedido1 = await unitOfWork.Orders.GetByIdAsync(pedido1.PedidoID);
        var updatedPedido2 = await unitOfWork.Orders.GetByIdAsync(pedido2.PedidoID);
        
        updatedCliente!.Nombre.Should().Be("Updated Name");
        updatedPedido1!.Estado.Should().Be(EstadoPedido.EnRuta);
        updatedPedido2!.Estado.Should().Be(EstadoPedido.Entregado);
    }

    #endregion

    #region Repository Access Tests

    [Fact]
    public void UnitOfWork_ShouldProvideAccessToAllRepositories()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();

        // Assert
        unitOfWork.Orders.Should().NotBeNull();
        unitOfWork.Clientes.Should().NotBeNull();
        unitOfWork.Users.Should().NotBeNull();
        unitOfWork.Sessions.Should().NotBeNull();
        unitOfWork.Messages.Should().NotBeNull();
        unitOfWork.Settings.Should().NotBeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();

        // Act & Assert - Multiple disposes should not throw
        unitOfWork.Dispose();
        Action act = () => unitOfWork.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
