using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Persistence.Repositories;

public class SolicitudFacturaRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly SolicitudFacturaRepository _repository;

    public SolicitudFacturaRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"SolicitudFacturaRepoTests_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new SolicitudFacturaRepository(_context);
    }

    private async Task<(Cliente cliente, SolicitudFactura solicitud)> SeedSampleDataAsync(string folio = "FOL-001", int clienteId = 1)
    {
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle 1");
        var datos = new DatosFacturacion("Mi Empresa SA", "XAXX010101000", "Calle 1", "10", "Centro", "64000", "test@test.com", "601");
        cliente.UpdateDatosFacturacion(datos);

        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();

        var solicitud = SolicitudFactura.Create(cliente.ClienteID, folio, 500.00m, "G03", datos, "Notas");
        await _context.SolicitudesFactura.AddAsync(solicitud);
        await _context.SaveChangesAsync();

        return (cliente, solicitud);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSolicitudesWithCliente()
    {
        // Arrange
        var (_, s1) = await SeedSampleDataAsync("FOL-001");
        var (_, s2) = await SeedSampleDataAsync("FOL-002");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.Cliente != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByClienteIdAsync_ShouldReturnOnlyClienteSolicitudes()
    {
        // Arrange
        var (c1, s1) = await SeedSampleDataAsync("FOL-001");
        var (c2, s2) = await SeedSampleDataAsync("FOL-002");

        // Act
        var result = await _repository.GetByClienteIdAsync(c1.ClienteID);

        // Assert
        result.Should().HaveCount(1);
        result.First().Folio.Should().Be("FOL-001");
        result.First().Cliente.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByFolioAsync_WhenExists_ShouldReturnSolicitud()
    {
        // Arrange
        await SeedSampleDataAsync("FOL-1234");

        // Act
        var result = await _repository.GetByFolioAsync("FOL-1234");

        // Assert
        result.Should().NotBeNull();
        result!.Folio.Should().Be("FOL-1234");
        result.Cliente.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NON_EXISTING")]
    public async Task GetByFolioAsync_WhenNotExistsOrEmpty_ShouldReturnNull(string? folio)
    {
        // Arrange
        await SeedSampleDataAsync("FOL-999");

        // Act
        var result = await _repository.GetByFolioAsync(folio!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnSolicitud()
    {
        // Arrange
        var (_, solicitud) = await SeedSampleDataAsync("FOL-555");

        // Act
        var result = await _repository.GetByIdAsync(solicitud.SolicitudFacturaID);

        // Assert
        result.Should().NotBeNull();
        result!.SolicitudFacturaID.Should().Be(solicitud.SolicitudFacturaID);
        result.Cliente.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
