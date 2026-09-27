using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Infrastructure.Persistence;
using BotCarniceria.Infrastructure.Persistence.Context;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Persistence;

public class DbInitializerTests : IDisposable
{
    private readonly BotCarniceriaDbContext _context;

    public DbInitializerTests()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: $"DbInitializerTests_{Guid.NewGuid()}")
            .Options;

        var mockMediator = new Mock<IMediator>();
        _context = new BotCarniceriaDbContext(options, mockMediator.Object);
    }

    [Fact]
    public async Task InitializeAsync_FreshDatabase_ShouldSeedDefaultConfigurationsAndAdminUser()
    {
        // Act
        await DbInitializer.InitializeAsync(_context);

        // Assert
        var configs = await _context.Configuraciones.ToListAsync();
        configs.Should().NotBeEmpty();
        configs.Should().Contain(c => c.Clave == ConfigurationKeys.WhatsApp.PhoneNumberId);
        configs.Should().Contain(c => c.Clave == ConfigurationKeys.Business.Schedule);
        configs.Should().Contain(c => c.Clave == ConfigurationKeys.Printers.IpAddress);
        configs.Should().Contain(c => c.Clave == ConfigurationKeys.Orders.LateOrderWarningStartHour);

        var adminUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == "admin");
        adminUser.Should().NotBeNull();
        adminUser!.Rol.Should().Be(RolUsuario.Admin);
        adminUser.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadySeeded_ShouldNotDuplicateConfigs()
    {
        // Act
        await DbInitializer.InitializeAsync(_context);
        var initialConfigCount = await _context.Configuraciones.CountAsync();

        // Run again
        await DbInitializer.InitializeAsync(_context);
        var secondConfigCount = await _context.Configuraciones.CountAsync();

        // Assert
        secondConfigCount.Should().Be(initialConfigCount);

        var adminUsers = await _context.Usuarios.Where(u => u.Username == "admin").ToListAsync();
        adminUsers.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
