using FileCategorization_Shared.Common;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Interface for cache service providing intelligent caching strategies for mobile app performance
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets cached item or executes factory method and caches result
    /// </summary>
    Task<Result<T>> GetOrSetAsync<T>(string key, Func<Task<Result<T>>> getItem, TimeSpan? expiry = null);

    /// <summary>
    /// Gets cached item or executes factory method and caches result (non-Result version)
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null) where T : class;

    /// <summary>
    /// Gets cached item if exists
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets item in cache with optional expiry
    /// </summary>
    Task SetAsync<T>(string key, T item, TimeSpan? expiry = null);

    /// <summary>
    /// Removes specific item from cache
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all items matching pattern (e.g., "files:*")
    /// </summary>
    Task RemovePatternAsync(string pattern);

    /// <summary>
    /// Clears entire cache
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Checks if cache contains key
    /// </summary>
    Task<bool> ContainsKeyAsync(string key);
}

/// <summary>
/// Cache statistics for performance monitoring
/// </summary>
public class CacheStatistics
{
    public int TotalItems { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
    public int TotalRequests => HitCount + MissCount;
    public long MemoryUsageBytes { get; set; }
    public DateTime LastResetTime { get; set; }
}