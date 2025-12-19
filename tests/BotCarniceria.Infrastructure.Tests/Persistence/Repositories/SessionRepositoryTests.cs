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

public class SessionRepositoryTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private readonly SessionRepository _repository;

    public SessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
        _repository = new SessionRepository(_context);
    }

    [Fact]
    public async Task GetByPhoneAsync_WithExistingPhone_ShouldReturnSession()
    {
        // Arrange
        var phone = "5551234567";
        var session = Conversacion.Create(phone);
        await _context.Conversaciones.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync(phone);

        // Assert
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be(phone);
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
    public async Task GetByPhoneAsync_WithMultipleSessions_ShouldReturnCorrectOne()
    {
        // Arrange
        var session1 = Conversacion.Create("5551111111");
        var session2 = Conversacion.Create("5552222222");
        var session3 = Conversacion.Create("5553333333");

        await _context.Conversaciones.AddRangeAsync(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync("5552222222");

        // Assert
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be("5552222222");
    }

    [Fact]
    public async Task AddAsync_ShouldAddSession()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");

        // Act
        await _repository.AddAsync(session);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Conversaciones.FindAsync(session.NumeroTelefono);
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be("5551234567");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateSession()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        await _context.Conversaciones.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        session.CambiarEstado(ConversationState.MENU);
        session.GuardarBuffer("Test buffer");
        await _repository.UpdateAsync(session);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Conversaciones.FindAsync(session.NumeroTelefono);
        result!.Estado.Should().Be(ConversationState.MENU);
        result.Buffer.Should().Be("Test buffer");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteSession()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        await _context.Conversaciones.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(session);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Conversaciones.FindAsync(session.NumeroTelefono);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPhoneAsync_WithSessionInDifferentStates_ShouldReturnSession()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        session.CambiarEstado(ConversationState.TAKING_ORDER);
        await _context.Conversaciones.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPhoneAsync("5551234567");

        // Assert
        result.Should().NotBeNull();
        result!.Estado.Should().Be(ConversationState.TAKING_ORDER);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
