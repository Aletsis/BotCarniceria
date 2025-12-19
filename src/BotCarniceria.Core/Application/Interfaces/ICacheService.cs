namespace BotCarniceria.Core.Application.Interfaces;

/// <summary>
/// Service for caching data to improve performance and reduce database load.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>The cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Sets a value in the cache with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Optional expiration time. If null, uses default sliding expiration.</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    
    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Removes all cache entries with keys starting with the specified prefix.
    /// </summary>
    /// <param name="prefix">Key prefix to match</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RemoveByPrefixAsync(string prefix);
    
    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key);
}
