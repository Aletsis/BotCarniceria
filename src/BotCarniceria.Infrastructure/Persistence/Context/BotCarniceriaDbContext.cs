using BotCarniceria.Core.Domain.Common;
using BotCarniceria.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BotCarniceria.Infrastructure.Persistence.Context;

public class BotCarniceriaDbContext : DbContext
{
    private readonly IMediator _mediator;

    public BotCarniceriaDbContext(
        DbContextOptions<BotCarniceriaDbContext> options,
        IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Conversacion> Conversaciones { get; set; } = null!;
    public DbSet<Mensaje> Mensajes { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Configuracion> Configuraciones { get; set; } = null!;
    public DbSet<SolicitudFactura> SolicitudesFactura { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync();

        // Save changes
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // Clear events from entities
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // Publish events
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}

