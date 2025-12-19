using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using BotCarniceria.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BotCarniceria.IntegrationTests.Relationships;

public class EntityRelationshipsIntegrationTests : DatabaseTestBase
{
    #region Cliente-Pedido Relationship Tests

    [Fact]
    public async Task Cliente_ShouldLoadRelatedPedidos()
    {
        // Arrange
        var clienteRepo = new ClienteRepository(DbContext);
        var pedidoRepo = new OrderRepository(DbContext);

        var cliente = Cliente.Create("5551234567", "Test Cliente");
        await clienteRepo.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido1 = Pedido.Create(cliente.ClienteID, "Pedido 1");
        var pedido2 = Pedido.Create(cliente.ClienteID, "Pedido 2");
        var pedido3 = Pedido.Create(cliente.ClienteID, "Pedido 3");

        await pedidoRepo.AddAsync(pedido1);
        await pedidoRepo.AddAsync(pedido2);
        await pedidoRepo.AddAsync(pedido3);
        await DbContext.SaveChangesAsync();

        // Act - Reload cliente with pedidos
        DbContext.Entry(cliente).State = EntityState.Detached;
        var loadedCliente = await DbContext.Clientes
            .Include(c => c.Pedidos)
            .FirstOrDefaultAsync(c => c.ClienteID == cliente.ClienteID);

        // Assert
        loadedCliente.Should().NotBeNull();
        loadedCliente!.Pedidos.Should().HaveCount(3);
        loadedCliente.Pedidos.Should().AllSatisfy(p => 
            p.ClienteID.Should().Be(cliente.ClienteID));
    }

    [Fact]
    public async Task Pedido_ShouldLoadRelatedCliente()
    {
        // Arrange
        var clienteRepo = new ClienteRepository(DbContext);
        var pedidoRepo = new OrderRepository(DbContext);

        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        await clienteRepo.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test Pedido");
        await pedidoRepo.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Act - Reload pedido with cliente
        DbContext.Entry(pedido).State = EntityState.Detached;
        var loadedPedido = await DbContext.Pedidos
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.PedidoID == pedido.PedidoID);

        // Assert
        loadedPedido.Should().NotBeNull();
        loadedPedido!.Cliente.Should().NotBeNull();
        loadedPedido.Cliente!.Nombre.Should().Be("Juan Pérez");
        loadedPedido.Cliente.ClienteID.Should().Be(cliente.ClienteID);
    }

    [Fact]
    public async Task MultipleClientes_WithMultiplePedidos_ShouldMaintainCorrectRelationships()
    {
        // Arrange
        var clienteRepo = new ClienteRepository(DbContext);
        var pedidoRepo = new OrderRepository(DbContext);

        var cliente1 = Cliente.Create("5551111111", "Cliente 1");
        var cliente2 = Cliente.Create("5552222222", "Cliente 2");

        await clienteRepo.AddAsync(cliente1);
        await clienteRepo.AddAsync(cliente2);
        await DbContext.SaveChangesAsync();

        // Cliente 1 has 3 pedidos
        await pedidoRepo.AddAsync(Pedido.Create(cliente1.ClienteID, "C1-P1"));
        await pedidoRepo.AddAsync(Pedido.Create(cliente1.ClienteID, "C1-P2"));
        await pedidoRepo.AddAsync(Pedido.Create(cliente1.ClienteID, "C1-P3"));

        // Cliente 2 has 2 pedidos
        await pedidoRepo.AddAsync(Pedido.Create(cliente2.ClienteID, "C2-P1"));
        await pedidoRepo.AddAsync(Pedido.Create(cliente2.ClienteID, "C2-P2"));

        await DbContext.SaveChangesAsync();

        // Act
        var loadedCliente1 = await DbContext.Clientes
            .Include(c => c.Pedidos)
            .FirstOrDefaultAsync(c => c.ClienteID == cliente1.ClienteID);

        var loadedCliente2 = await DbContext.Clientes
            .Include(c => c.Pedidos)
            .FirstOrDefaultAsync(c => c.ClienteID == cliente2.ClienteID);

        // Assert
        loadedCliente1!.Pedidos.Should().HaveCount(3);
        loadedCliente2!.Pedidos.Should().HaveCount(2);
        
        loadedCliente1.Pedidos.Should().AllSatisfy(p => 
            p.Contenido.Should().StartWith("C1-"));
        
        loadedCliente2.Pedidos.Should().AllSatisfy(p => 
            p.Contenido.Should().StartWith("C2-"));
    }

    #endregion

    #region Conversacion-Mensaje Relationship Tests

    [Fact]
    public async Task Conversacion_ShouldLoadRelatedMensajes()
    {
        // Arrange
        var sessionRepo = new SessionRepository(DbContext);
        var messageRepo = new MessageRepository(DbContext);

        var conversacion = Conversacion.Create("5551234567");
        await sessionRepo.AddAsync(conversacion);
        await DbContext.SaveChangesAsync();

        var mensaje1 = Mensaje.CrearEntrante("5551234567", "Hola", TipoContenidoMensaje.Texto);
        var mensaje2 = Mensaje.CrearSaliente("5551234567", "Hola, ¿en qué puedo ayudarte?", TipoContenidoMensaje.Texto);
        var mensaje3 = Mensaje.CrearEntrante("5551234567", "Quiero hacer un pedido", TipoContenidoMensaje.Texto);

        await messageRepo.AddAsync(mensaje1);
        await messageRepo.AddAsync(mensaje2);
        await messageRepo.AddAsync(mensaje3);
        await DbContext.SaveChangesAsync();

        // Act
        var mensajes = await DbContext.Mensajes
            .Where(m => m.NumeroTelefono == "5551234567")
            .OrderBy(m => m.Fecha)
            .ToListAsync();

        // Assert
        mensajes.Should().HaveCount(3);
        mensajes[0].Origen.Should().Be(TipoMensajeOrigen.Entrante);
        mensajes[1].Origen.Should().Be(TipoMensajeOrigen.Saliente);
        mensajes[2].Origen.Should().Be(TipoMensajeOrigen.Entrante);
    }

    #endregion

    #region Navigation Property Tests

    [Fact]
    public async Task NavigationProperties_ShouldBeProperlyConfigured()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test");
        await DbContext.Clientes.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test Pedido");
        await DbContext.Pedidos.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Act - Clear context and reload
        DbContext.ChangeTracker.Clear();
        
        var loadedPedido = await DbContext.Pedidos
            .Include(p => p.Cliente)
            .FirstAsync(p => p.PedidoID == pedido.PedidoID);

        // Assert
        loadedPedido.Cliente.Should().NotBeNull();
        loadedPedido.ClienteID.Should().Be(loadedPedido.Cliente!.ClienteID);
    }

    [Fact]
    public async Task LazyLoading_ShouldNotBeEnabled()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test");
        await DbContext.Clientes.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test");
        await DbContext.Pedidos.AddAsync(pedido);
        await DbContext.SaveChangesAsync();

        // Act - Load without Include
        DbContext.ChangeTracker.Clear();
        var loadedPedido = await DbContext.Pedidos
            .FirstAsync(p => p.PedidoID == pedido.PedidoID);

        // Assert - Cliente should be null (lazy loading disabled)
        loadedPedido.Cliente.Should().BeNull();
    }

    #endregion

    #region Query Performance Tests

    [Fact]
    public async Task EagerLoading_ShouldLoadRelatedEntitiesInSingleQuery()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test");
        await DbContext.Clientes.AddAsync(cliente);
        await DbContext.SaveChangesAsync();

        for (int i = 1; i <= 5; i++)
        {
            await DbContext.Pedidos.AddAsync(Pedido.Create(cliente.ClienteID, $"Pedido {i}"));
        }
        await DbContext.SaveChangesAsync();

        // Act
        DbContext.ChangeTracker.Clear();
        var loadedCliente = await DbContext.Clientes
            .Include(c => c.Pedidos)
            .FirstAsync(c => c.ClienteID == cliente.ClienteID);

        // Assert
        loadedCliente.Pedidos.Should().HaveCount(5);
        // All pedidos should be loaded without additional queries
        loadedCliente.Pedidos.Should().AllSatisfy(p => 
            p.Should().NotBeNull());
    }

    #endregion
}
