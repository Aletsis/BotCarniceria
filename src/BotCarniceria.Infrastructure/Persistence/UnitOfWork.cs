using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace BotCarniceria.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly BotCarniceriaDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public IOrderRepository Orders { get; }
    public IClienteRepository Clientes { get; }
    public ISessionRepository Sessions { get; }
    public IMessageRepository Messages { get; }
    public IConfiguracionRepository Settings { get; }
    public IUsuarioRepository Users { get; }

    public UnitOfWork(
        BotCarniceriaDbContext context,
        IOrderRepository orderRepository,
        IClienteRepository clienteRepository,
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IConfiguracionRepository settingsRepository,
        IUsuarioRepository usuarioRepository)
    {
        _context = context;
        Orders = orderRepository;
        Clientes = clienteRepository;
        Sessions = sessionRepository;
        Messages = messageRepository;
        Settings = settingsRepository;
        Users = usuarioRepository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
