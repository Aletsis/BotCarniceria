using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;

namespace BotCarniceria.Core.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Specification<T> spec);
    Task<T> AddAsync(T entity); // Standard name Add instead of Create
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Specification<T> spec);
    Task<bool> AnyAsync(Specification<T> spec);
}

public interface IOrderRepository : IRepository<Pedido>
{
    // Métodos específicos de Order si son necesarios más allá de Specifications
    Task<Pedido?> GetByFolioAsync(string folio);
    Task<string> GenerateNextFolioAsync();
}

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByPhoneAsync(string phone);
}

public interface ISessionRepository : IRepository<Conversacion>
{
    Task<Conversacion?> GetByPhoneAsync(string phone);
}

public interface IMessageRepository : IRepository<Mensaje>
{
    Task<List<Mensaje>> GetByPhoneAsync(string phone, int count = 50, int skip = 0);
}

public interface IConfiguracionRepository : IRepository<Configuracion>
{
    Task<string?> GetValorAsync(string clave);
    Task<T?> GetValorAsync<T>(string clave);
    Task<Configuracion?> GetByClaveAsync(string clave);
}

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByUsernameAsync(string username);
}

public interface IUnitOfWork
{
    IOrderRepository Orders { get; }
    IClienteRepository Clientes { get; }
    ISessionRepository Sessions { get; }
    IMessageRepository Messages { get; }
    IConfiguracionRepository Settings { get; }
    IUsuarioRepository Users { get; }
    public IConfiguracionRepository Configuraciones => Settings; // Alias for ease of use

    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
