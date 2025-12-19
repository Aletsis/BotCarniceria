using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using BotCarniceria.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BotCarniceria.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides an in-memory database context.
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    protected BotCarniceriaDbContext DbContext { get; private set; }

    protected DatabaseTestBase()
    {
        var options = new DbContextOptionsBuilder<BotCarniceriaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        var mockMediator = new Mock<IMediator>();
        DbContext = new BotCarniceriaDbContext(options, mockMediator.Object);
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Seeds the database with initial test data.
    /// Override this method in derived classes to provide custom seed data.
    /// </summary>
    protected virtual void SeedDatabase()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    protected void ClearDatabase()
    {
        DbContext.Pedidos.RemoveRange(DbContext.Pedidos);
        DbContext.Clientes.RemoveRange(DbContext.Clientes);
        DbContext.Usuarios.RemoveRange(DbContext.Usuarios);
        DbContext.Conversaciones.RemoveRange(DbContext.Conversaciones);
        DbContext.Mensajes.RemoveRange(DbContext.Mensajes);
        DbContext.Configuraciones.RemoveRange(DbContext.Configuraciones);
        DbContext.SaveChanges();
    }

    /// <summary>
    /// Creates a UnitOfWork instance with all repositories initialized.
    /// </summary>
    protected UnitOfWork CreateUnitOfWork()
    {
        var orderRepo = new OrderRepository(DbContext);
        var clienteRepo = new ClienteRepository(DbContext);
        var sessionRepo = new SessionRepository(DbContext);
        var messageRepo = new MessageRepository(DbContext);
        var configRepo = new ConfiguracionRepository(DbContext);
        var usuarioRepo = new UsuarioRepository(DbContext);

        return new UnitOfWork(
            DbContext,
            orderRepo,
            clienteRepo,
            sessionRepo,
            messageRepo,
            configRepo,
            usuarioRepo
        );
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
