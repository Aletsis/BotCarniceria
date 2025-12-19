using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Persistence.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly OrderRepository _repository;
    private readonly ClienteRepository _clienteRepository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new OrderRepository(_context);
        _clienteRepository = new ClienteRepository(_context);
    }

    [Fact]
    public async Task GenerateNextFolioAsync_ShouldGenerateSequentialFolios()
    {
        // Act
        var folio1 = await _repository.GenerateNextFolioAsync();
        var folio2 = await _repository.GenerateNextFolioAsync();

        // Assert
        folio1.Should().Be("PED-000001");
        folio2.Should().Be("PED-000001"); // Same because no orders were saved
    }

    [Fact]
    public async Task GenerateNextFolioAsync_AfterAddingOrders_ShouldIncrementFolio()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        await _clienteRepository.AddAsync(cliente);
        await _context.SaveChangesAsync();

        var pedido1 = Pedido.Create(cliente.ClienteID, "Pedido 1");
        await _repository.AddAsync(pedido1);
        await _context.SaveChangesAsync();

        // Act
        var folio = await _repository.GenerateNextFolioAsync();

        // Assert
        folio.Should().Be("PED-000002");
    }

    [Fact]
    public async Task GetByFolioAsync_WithExistingFolio_ShouldReturnPedido()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        await _clienteRepository.AddAsync(cliente);
        await _context.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test Order");
        await _repository.AddAsync(pedido);
        await _context.SaveChangesAsync();

        var folio = pedido.Folio.Value;

        // Act
        var result = await _repository.GetByFolioAsync(folio);

        // Assert
        result.Should().NotBeNull();
        result!.Folio.Value.Should().Be(folio);
        result.Cliente.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByFolioAsync_WithNonExistingFolio_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByFolioAsync("PED-999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByFolioAsync_WithInvalidFolio_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByFolioAsync("INVALID");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByFolioAsync_WithEmptyFolio_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByFolioAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnPedidoWithCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        await _clienteRepository.AddAsync(cliente);
        await _context.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test Order");
        await _repository.AddAsync(pedido);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(pedido.PedidoID);

        // Assert
        result.Should().NotBeNull();
        result!.PedidoID.Should().Be(pedido.PedidoID);
        result.Cliente.Should().NotBeNull();
        result.Cliente.Nombre.Should().Be("Test Cliente");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999999L);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddPedido()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Test Cliente", "Test Address");
        await _clienteRepository.AddAsync(cliente);
        await _context.SaveChangesAsync();

        var pedido = Pedido.Create(cliente.ClienteID, "Test Order", "Test Notes", "Efectivo");

        // Act
        await _repository.AddAsync(pedido);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Pedidos.FindAsync(pedido.PedidoID);
        result.Should().NotBeNull();
        result!.Contenido.Should().Be("Test Order");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
