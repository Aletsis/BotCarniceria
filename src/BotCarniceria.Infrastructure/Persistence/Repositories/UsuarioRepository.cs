using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BotCarniceria.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByUsernameAsync(string username)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == username);
    }
}
