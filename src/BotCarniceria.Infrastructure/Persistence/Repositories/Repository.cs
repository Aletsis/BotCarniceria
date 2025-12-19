using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BotCarniceria.Infrastructure.Persistence.Repositories;

public abstract class Repository<T> : IRepository<T> where T : class
{
    protected readonly BotCarniceriaDbContext _context;
    
    public Repository(BotCarniceriaDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<List<T>> FindAsync(Specification<T> spec)
    {
        // Simple evaluator
        var query = _context.Set<T>().AsQueryable();
        query = query.Where(spec.ToExpression());
        
        return await query.ToListAsync();
    }
    
    public async Task<int> CountAsync(Specification<T> spec)
    {
         var query = _context.Set<T>().AsQueryable();
         return await query.CountAsync(spec.ToExpression());
    }
    
    public async Task<bool> AnyAsync(Specification<T> spec)
    {
         var query = _context.Set<T>().AsQueryable();
         return await query.AnyAsync(spec.ToExpression());
    }
}
