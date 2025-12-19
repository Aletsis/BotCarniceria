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

public class MessageRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly MessageRepository _repository;

    public MessageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new MessageRepository(_context);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithExistingMessages_ShouldReturnMessages()
    {
        // Arrange
        var phone = "5551234567";
        var mensaje1 = Mensaje.CrearEntrante(phone, "Mensaje 1", TipoContenidoMensaje.Texto);
        var mensaje2 = Mensaje.CrearEntrante(phone, "Mensaje 2", TipoContenidoMensaje.Texto);
        var mensaje3 = Mensaje.CrearSaliente(phone, "Mensaje 3", TipoContenidoMensaje.Texto);

        await _context.Mensajes.AddRangeAsync(mensaje1, mensaje2, mensaje3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(m => m.NumeroTelefono == phone);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithNoMessages_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByPhoneAsync("9999999999");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPhoneAsync_ShouldReturnMessagesInChronologicalOrder()
    {
        // Arrange
        var phone = "5551234567";
        var mensaje1 = Mensaje.CrearEntrante(phone, "Primer mensaje", TipoContenidoMensaje.Texto);
        await Task.Delay(10); // Ensure different timestamps
        var mensaje2 = Mensaje.CrearEntrante(phone, "Segundo mensaje", TipoContenidoMensaje.Texto);
        await Task.Delay(10);
        var mensaje3 = Mensaje.CrearSaliente(phone, "Tercer mensaje", TipoContenidoMensaje.Texto);

        await _context.Mensajes.AddRangeAsync(mensaje3, mensaje1, mensaje2); // Add in random order
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone);

        // Assert
        result.Should().HaveCount(3);
        result[0].Contenido.Should().Be("Primer mensaje");
        result[1].Contenido.Should().Be("Segundo mensaje");
        result[2].Contenido.Should().Be("Tercer mensaje");
    }

    [Fact]
    public async Task GetByPhoneAsync_WithCountLimit_ShouldReturnLimitedMessages()
    {
        // Arrange
        var phone = "5551234567";
        for (int i = 0; i < 100; i++)
        {
            var mensaje = Mensaje.CrearEntrante(phone, $"Mensaje {i}", TipoContenidoMensaje.Texto);
            await _context.Mensajes.AddAsync(mensaje);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone, count: 10);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithMultiplePhones_ShouldReturnOnlyMatchingPhone()
    {
        // Arrange
        var phone1 = "5551111111";
        var phone2 = "5552222222";

        var mensaje1 = Mensaje.CrearEntrante(phone1, "Mensaje Phone 1", TipoContenidoMensaje.Texto);
        var mensaje2 = Mensaje.CrearEntrante(phone2, "Mensaje Phone 2", TipoContenidoMensaje.Texto);
        var mensaje3 = Mensaje.CrearSaliente(phone1, "Otro mensaje Phone 1", TipoContenidoMensaje.Texto);

        await _context.Mensajes.AddRangeAsync(mensaje1, mensaje2, mensaje3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.NumeroTelefono == phone1);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessage()
    {
        // Arrange
        var mensaje = Mensaje.CrearEntrante("5551234567", "Test message", TipoContenidoMensaje.Texto);

        // Act
        await _repository.AddAsync(mensaje);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Mensajes.FindAsync(mensaje.MensajeID);
        result.Should().NotBeNull();
        result!.Contenido.Should().Be("Test message");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteMessage()
    {
        // Arrange
        var mensaje = Mensaje.CrearEntrante("5551234567", "Test message", TipoContenidoMensaje.Texto);
        await _context.Mensajes.AddAsync(mensaje);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(mensaje);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Mensajes.FindAsync(mensaje.MensajeID);
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
