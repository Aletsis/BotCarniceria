using BotCarniceria.Core.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BotCarniceria.Infrastructure.Services.Caching;

/// <summary>
/// In-memory implementation of caching service using IMemoryCache.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    
    // Track cache keys for prefix-based removal
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();
    
    // Default cache settings
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromHours(1);
    
    public MemoryCacheService(
        IMemoryCache cache,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }
    
    /// <summary>
    /// Sets a value in the cache with optional expiration.
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                // Use absolute expiration if specified
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Use default sliding expiration
                options.SlidingExpiration = DefaultSlidingExpiration;
                options.AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpiration;
            }
            
            // Register callback to remove key from tracking when evicted
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _cacheKeys.TryRemove(key.ToString()!, out _);
                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
            });
            
            _cache.Set(key, value, options);
            _cacheKeys.TryAdd(key, 0);
            
            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", 
                key, expiration?.ToString() ?? "Default");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        try
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            
            _logger.LogDebug("Cache removed for key: {Key}", key);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Removes all cache entries with keys starting with the specified prefix.
    /// </summary>
    public Task RemoveByPrefixAsync(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));
        }
        
        try
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }
            
            _logger.LogDebug("Cache removed {Count} entries with prefix: {Prefix}", 
                keysToRemove.Count, prefix);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries with prefix: {Prefix}", prefix);
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        try
        {
            var exists = _cacheKeys.ContainsKey(key);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return Task.FromResult(false);
        }
    }
}
