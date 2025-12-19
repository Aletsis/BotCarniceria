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

public class ConfiguracionRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly ConfiguracionRepository _repository;

    public ConfiguracionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new ConfiguracionRepository(_context);
    }

    [Fact]
    public async Task GetValorAsync_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var config = Configuracion.Create("TestKey", "TestValue", TipoConfiguracion.Texto, "Test Description");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValorAsync("TestKey");

        // Assert
        result.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetByClaveAsync_WithExistingKey_ShouldReturnConfiguracion()
    {
        // Arrange
        var config = Configuracion.Create("TestKey", "TestValue", TipoConfiguracion.Texto, "Test Description");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByClaveAsync("TestKey");

        // Assert
        result.Should().NotBeNull();
        result!.Clave.Should().Be("TestKey");
        result.Valor.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetValorAsync_Generic_WithIntValue_ShouldReturnInt()
    {
        // Arrange
        var config = Configuracion.Create("IntKey", "42", TipoConfiguracion.Numero, "Integer value");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValorAsync<int>("IntKey");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetValorAsync_Generic_WithBoolValue_ShouldReturnBool()
    {
        // Arrange
        var config = Configuracion.Create("BoolKey", "true", TipoConfiguracion.Booleano, "Boolean value");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValorAsync<bool>("BoolKey");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetValorAsync_Generic_WithDecimalValue_ShouldReturnDecimal()
    {
        // Arrange
        var decimalValue = 123.45m;
        var decimalString = decimalValue.ToString(); // Uses current culture
        var config = Configuracion.Create("DecimalKey", decimalString, TipoConfiguracion.Numero, "Decimal value");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValorAsync<decimal>("DecimalKey");

        // Assert
        result.Should().Be(decimalValue);
    }

    [Fact]
    public async Task GetValorAsync_Generic_WithInvalidValue_ShouldReturnDefault()
    {
        // Arrange
        var config = Configuracion.Create("InvalidKey", "NotANumber", TipoConfiguracion.Numero, "Invalid integer");
        await _context.Configuraciones.AddAsync(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValorAsync<int>("InvalidKey");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_ShouldAddConfiguracion()
    {
        // Arrange
        var config = Configuracion.Create("NewKey", "NewValue", TipoConfiguracion.Texto, "New Description");

        // Act
        await _repository.AddAsync(config);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Configuraciones.FindAsync(config.ConfigID);
        result.Should().NotBeNull();
        result!.Clave.Should().Be("NewKey");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
