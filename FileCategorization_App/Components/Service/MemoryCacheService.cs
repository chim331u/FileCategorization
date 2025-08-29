using System.Collections.Concurrent;
using System.Text.Json;
using FileCategorization_App.Components.Interface;
using FileCategorization_Shared.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Memory-based cache service optimized for mobile app performance
/// </summary>
public class MemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    // Cache statistics tracking
    private int _hitCount = 0;
    private int _missCount = 0;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps = new();

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets cached item or executes factory method with Result pattern
    /// </summary>
    public async Task<Result<T>> GetOrSetAsync<T>(string key, Func<Task<Result<T>>> getItem, TimeSpan? expiry = null)
    {
        if (await ContainsKeyAsync(key))
        {
            var cachedResult = await GetAsync<T>(key);
            if (cachedResult != null)
            {
                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug($"Cache HIT for key: {key}");
                return Result<T>.Success(cachedResult);
            }
        }

        Interlocked.Increment(ref _missCount);
        _logger.LogDebug($"Cache MISS for key: {key}");

        try
        {
            var result = await getItem();
            
            if (result.IsSuccess && result.Value != null)
            {
                await SetAsync(key, result.Value, expiry ?? GetDefaultExpiry(key));
                _logger.LogInformation($"Cached result for key: {key}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing factory method for key {key}: {ex.Message}");
            return Result<T>.Failure($"Cache factory method failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets cached item or executes factory method (non-Result version)
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null) where T : class
    {
        if (await ContainsKeyAsync(key))
        {
            var cachedItem = await GetAsync<T>(key);
            if (cachedItem != null)
            {
                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug($"Cache HIT for key: {key}");
                return cachedItem;
            }
        }

        Interlocked.Increment(ref _missCount);
        _logger.LogDebug($"Cache MISS for key: {key}");

        try
        {
            var item = await getItem();
            if (item != null)
            {
                await SetAsync(key, item, expiry ?? GetDefaultExpiry(key));
                _logger.LogInformation($"Cached item for key: {key}");
            }
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing factory method for key {key}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets cached item if exists
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out var cachedItem) && cachedItem is T typedItem)
            {
                _keyTimestamps.TryGetValue(key, out var timestamp);
                _logger.LogDebug($"Retrieved cached item for key: {key} (cached at: {timestamp})");
                return typedItem;
            }
            return default(T);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Sets item in cache with expiry and memory management
    /// </summary>
    public async Task SetAsync<T>(string key, T item, TimeSpan? expiry = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? GetDefaultExpiry(key),
                SlidingExpiration = GetSlidingExpiry(key),
                Priority = GetCachePriority(key)
            };

            // Add callback for eviction tracking
            options.RegisterPostEvictionCallback(OnCacheItemEvicted);

            _cache.Set(key, item, options);
            _keyTimestamps.TryAdd(key, DateTime.UtcNow);
            
            _logger.LogDebug($"Cached item for key: {key} with expiry: {expiry}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes specific item from cache
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            _cache.Remove(key);
            _keyTimestamps.TryRemove(key, out _);
            _logger.LogDebug($"Removed cached item for key: {key}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes all items matching pattern
    /// </summary>
    public async Task RemovePatternAsync(string pattern)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Convert simple wildcard pattern to regex-like matching
            var keysToRemove = _keyTimestamps.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _keyTimestamps.TryRemove(key, out _);
            }

            _logger.LogInformation($"Removed {keysToRemove.Count} cached items matching pattern: {pattern}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Clears entire cache
    /// </summary>
    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // IMemoryCache doesn't have a clear method, so we track and remove keys
            var keysToRemove = _keyTimestamps.Keys.ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
            _keyTimestamps.Clear();
            _logger.LogInformation("Cleared entire cache");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalItems = _keyTimestamps.Count,
            HitCount = _hitCount,
            MissCount = _missCount,
            MemoryUsageBytes = EstimateMemoryUsage(),
            LastResetTime = _startTime
        };
    }

    /// <summary>
    /// Checks if cache contains key
    /// </summary>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _cache.TryGetValue(key, out _);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets default expiry based on data type
    /// </summary>
    private TimeSpan GetDefaultExpiry(string key)
    {
        return key switch
        {
            var k when k.StartsWith("categories") => TimeSpan.FromMinutes(10), // Categories rarely change
            var k when k.StartsWith("files:") => TimeSpan.FromMinutes(2), // File lists change moderately
            var k when k.StartsWith("threads:") => TimeSpan.FromMinutes(1), // DD threads change frequently
            var k when k.StartsWith("links:") => TimeSpan.FromSeconds(30), // DD links change very frequently
            _ => TimeSpan.FromMinutes(5) // Default expiry
        };
    }

    /// <summary>
    /// Gets sliding expiry for frequently accessed items
    /// </summary>
    private TimeSpan? GetSlidingExpiry(string key)
    {
        return key switch
        {
            var k when k.StartsWith("categories") => TimeSpan.FromMinutes(5), // Keep active categories longer
            var k when k.StartsWith("files:") => TimeSpan.FromMinutes(1), // Moderate sliding window
            _ => null // No sliding expiry for other items
        };
    }

    /// <summary>
    /// Gets cache priority based on data type
    /// </summary>
    private CacheItemPriority GetCachePriority(string key)
    {
        return key switch
        {
            var k when k.StartsWith("categories") => CacheItemPriority.High, // Categories are critical
            var k when k.StartsWith("files:") => CacheItemPriority.Normal, // File lists are important
            var k when k.StartsWith("threads:") => CacheItemPriority.Low, // DD data is less critical
            _ => CacheItemPriority.Normal
        };
    }

    /// <summary>
    /// Simple pattern matching for cache key filtering
    /// </summary>
    private bool IsPatternMatch(string key, string pattern)
    {
        if (pattern.EndsWith("*"))
        {
            return key.StartsWith(pattern[..^1]);
        }
        if (pattern.StartsWith("*"))
        {
            return key.EndsWith(pattern[1..]);
        }
        return key == pattern;
    }

    /// <summary>
    /// Estimates memory usage (rough calculation)
    /// </summary>
    private long EstimateMemoryUsage()
    {
        // Rough estimate: 500 bytes per cache entry on average
        return _keyTimestamps.Count * 500L;
    }

    /// <summary>
    /// Callback for cache item eviction logging
    /// </summary>
    private void OnCacheItemEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        _keyTimestamps.TryRemove(key.ToString()!, out _);
        _logger.LogDebug($"Cache item evicted - Key: {key}, Reason: {reason}");
    }

    #endregion

    public void Dispose()
    {
        _semaphore?.Dispose();
        _cache?.Dispose();
    }
}