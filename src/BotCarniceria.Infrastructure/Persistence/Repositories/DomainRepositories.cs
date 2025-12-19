using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BotCarniceria.Infrastructure.Persistence.Repositories;

public class OrderRepository : Repository<Pedido>, IOrderRepository
{
    public OrderRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<string> GenerateNextFolioAsync()
    {
        // Simple folio generation logic
        // Could be moved to database sequence or procedure
        var count = await _context.Pedidos.CountAsync();
        return Folio.From($"PED-{count + 1:D6}").Value; 
    }

    public async Task<Pedido?> GetByFolioAsync(string folio)
    {
        // Assuming Folio.From handles validation or creation properly.
        // EF Core conversion allows comparing ValueObject with ValueObject property.
        // But if Folio.From throws on empty, check first.
        if (string.IsNullOrWhiteSpace(folio)) return null;
        
        try 
        {
            var folioVo = Folio.From(folio);
            return await _context.Pedidos
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.Folio == folioVo);
        }
        catch
        {
            // Invalid folio format
            return null;
        }
    }
    
    // Override GetById to include navigation properties if needed
    public new async Task<Pedido?> GetByIdAsync(object id)
    {
        return await _context.Pedidos
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.PedidoID == (long)id);
    }
}

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<Cliente?> GetByPhoneAsync(string phone)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.NumeroTelefono == phone);
    }
}

public class SessionRepository : Repository<Conversacion>, ISessionRepository
{
    public SessionRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<Conversacion?> GetByPhoneAsync(string phone)
    {
        return await _context.Conversaciones
            .FirstOrDefaultAsync(c => c.NumeroTelefono == phone);
    }
}

public class MessageRepository : Repository<Mensaje>, IMessageRepository
{
    public MessageRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<List<Mensaje>> GetByPhoneAsync(string phone, int count = 50, int skip = 0)
    {
        return await _context.Mensajes
            .Where(m => m.NumeroTelefono == phone)
            .OrderByDescending(m => m.Fecha)
            .Skip(skip)
            .Take(count)
            .OrderBy(m => m.Fecha) // Return in chronological order
            .ToListAsync();
    }
}
