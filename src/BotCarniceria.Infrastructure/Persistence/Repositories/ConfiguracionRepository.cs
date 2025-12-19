using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BotCarniceria.Infrastructure.Persistence.Repositories;

public class ConfiguracionRepository : Repository<Configuracion>, IConfiguracionRepository
{
    public ConfiguracionRepository(BotCarniceriaDbContext context) : base(context)
    {
    }

    public async Task<string?> GetValorAsync(string clave)
    {
        var config = await _context.Set<Configuracion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Clave == clave);
        return config?.Valor;
    }

    public async Task<Configuracion?> GetByClaveAsync(string clave)
    {
        return await _context.Configuraciones
            .FirstOrDefaultAsync(c => c.Clave == clave);
    }

    public async Task<T?> GetValorAsync<T>(string clave)
    {
        var valorStr = await GetValorAsync(clave);
        if (string.IsNullOrEmpty(valorStr)) return default;

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return (T?)converter.ConvertFrom(valorStr);
            }
            return (T)Convert.ChangeType(valorStr, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
