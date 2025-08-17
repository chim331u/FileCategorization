using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using FileCategorization_Web.Data.Caching;

namespace FileCategorization_Web.Services.Caching;

public class MemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, List<string>> _taggedKeys;
    private readonly ConcurrentDictionary<string, DateTime> _keyCreationTimes;
    private int _hitCount = 0;
    private int _missCount = 0;
    private bool _disposed = false;

    // Events
    public event Action<string, object?>? CacheItemSet;
    public event Action<string>? CacheItemRemoved;
    public event Action<CacheInvalidationStrategy>? CacheInvalidated;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _taggedKeys = new ConcurrentDictionary<string, List<string>>();
        _keyCreationTimes = new ConcurrentDictionary<string, DateTime>();
        
        _logger.LogInformation("MemoryCacheService initialized");
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                
                return cachedValue as T;
            }

            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return null;
        }
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T?>> factory, CachePolicy? policy = null) where T : class
    {
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        try
        {
            _logger.LogDebug("Cache miss, executing factory for key: {Key}", key);
            var factoryResult = await factory();
            
            if (factoryResult != null)
            {
                await SetAsync(key, factoryResult, policy);
            }
            
            return factoryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing factory for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy? policy = null) where T : class
    {
        try
        {
            if (value == null)
            {
                _logger.LogWarning("Attempted to cache null value for key: {Key}", key);
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Attempted to cache with null or empty key");
                return;
            }

            var cachePolicy = policy ?? CachePolicy.Medium;
            var options = CreateMemoryCacheEntryOptions(cachePolicy, key);
            
            _logger.LogDebug("Setting cache for key: {Key}, value type: {ValueType}", key, typeof(T).Name);
            _memoryCache.Set(key, value, options);
            _keyCreationTimes[key] = DateTime.UtcNow;
            
            // Tag management - safe handling of tags
            if (cachePolicy.Tags != null && cachePolicy.Tags.Any())
            {
                foreach (var tag in cachePolicy.Tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        _taggedKeys.AddOrUpdate(tag, 
                            new List<string> { key }, 
                            (_, existing) => 
                            {
                                if (!existing.Contains(key))
                                {
                                    existing.Add(key);
                                }
                                return existing;
                            });
                    }
                }
            }

            _logger.LogDebug("Cached value for key: {Key} with policy: {Policy}", key, cachePolicy.GetType().Name);
            CacheItemSet?.Invoke(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            _keyCreationTimes.TryRemove(key, out _);
            
            // Remove from tag tracking
            foreach (var tagKeys in _taggedKeys.Values)
            {
                tagKeys.Remove(key);
            }

            _logger.LogDebug("Removed cache entry for key: {Key}", key);
            CacheItemRemoved?.Invoke(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _keyCreationTimes.Keys.Where(key => regex.IsMatch(key)).ToList();
            
            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }

            _logger.LogInformation("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            if (_memoryCache is MemoryCache mc)
            {
                mc.Clear();
            }
            
            _taggedKeys.Clear();
            _keyCreationTimes.Clear();
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);

            _logger.LogInformation("Cache cleared");
            CacheInvalidated?.Invoke(CacheInvalidationStrategy.All);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    public async Task InvalidateAsync(CacheInvalidationStrategy strategy)
    {
        try
        {
            var patterns = strategy switch
            {
                CacheInvalidationStrategy.All => new[] { ".*" },
                CacheInvalidationStrategy.FileData => new[] { "files:.*", "file-list:.*" },
                CacheInvalidationStrategy.Categories => new[] { "categories:.*", "category-list" },
                CacheInvalidationStrategy.Configurations => new[] { "configs:.*", "config-list" },
                CacheInvalidationStrategy.UserInterface => new[] { "ui:.*", "files:.*" },
                CacheInvalidationStrategy.Metadata => new[] { "categories:.*", "configs:.*" },
                _ => Array.Empty<string>()
            };

            foreach (var pattern in patterns)
            {
                await RemoveByPatternAsync(pattern);
            }

            _logger.LogInformation("Cache invalidated with strategy: {Strategy}", strategy);
            CacheInvalidated?.Invoke(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache with strategy: {Strategy}", strategy);
        }
    }

    public async Task InvalidateByTagsAsync(params string[] tags)
    {
        try
        {
            var keysToRemove = new HashSet<string>();
            
            foreach (var tag in tags)
            {
                if (_taggedKeys.TryGetValue(tag, out var taggedKeys))
                {
                    foreach (var key in taggedKeys)
                    {
                        keysToRemove.Add(key);
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }

            _logger.LogInformation("Invalidated {Count} cache entries with tags: {Tags}", keysToRemove.Count, string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public CacheStatistics GetStatistics()
    {
        try
        {
            var totalItems = _keyCreationTimes.Count;
            var totalMemoryUsage = EstimateMemoryUsage();

            return new CacheStatistics
            {
                TotalItems = totalItems,
                TotalMemoryUsage = totalMemoryUsage,
                HitCount = _hitCount,
                MissCount = _missCount,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await Task.FromResult(GetStatistics());
    }

    public async Task InvalidateByTagAsync(string tag)
    {
        await InvalidateByTagsAsync(tag);
    }

    public async Task ClearAllAsync()
    {
        try
        {
            var allKeys = _keyCreationTimes.Keys.ToList();
            foreach (var key in allKeys)
            {
                await RemoveAsync(key);
            }
            
            CacheInvalidated?.Invoke(CacheInvalidationStrategy.All);
            _logger?.LogInformation("Cache cleared - all entries removed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    public string GenerateKey(string prefix, params object[] parameters)
    {
        var keyParts = new List<string> { prefix };
        keyParts.AddRange(parameters.Select(p => p?.ToString() ?? "null"));
        return string.Join(":", keyParts);
    }

    public string GenerateFileListKey(int searchParameter) => GenerateKey("files", "list", searchParameter);
    public string GenerateCategoryListKey() => GenerateKey("categories", "list");
    public string GenerateConfigListKey() => GenerateKey("configs", "list");

    private MemoryCacheEntryOptions CreateMemoryCacheEntryOptions(CachePolicy policy, string key)
    {
        var options = new MemoryCacheEntryOptions();

        if (policy.AbsoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = policy.AbsoluteExpiration.Value;
        }

        if (policy.SlidingExpiration.HasValue)
        {
            options.SlidingExpiration = policy.SlidingExpiration.Value;
        }

        options.Priority = policy.Priority switch
        {
            CachePriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
            CachePriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
            CachePriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
            CachePriority.Critical => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
            _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
        };

        // Set size for memory cache sizing (required when SizeLimit is configured)
        // Estimate size based on cache policy and key type
        options.Size = EstimateCacheEntrySize(key);

        // Add eviction callback for cleanup
        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            _keyCreationTimes.TryRemove(evictedKey.ToString() ?? "", out _);
            
            // Remove from tag tracking
            foreach (var tagKeys in _taggedKeys.Values)
            {
                tagKeys.Remove(evictedKey.ToString() ?? "");
            }

            _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
            CacheItemRemoved?.Invoke(evictedKey?.ToString() ?? "");
        });

        return options;
    }

    private long EstimateCacheEntrySize(string key)
    {
        // Estimate cache entry size based on key patterns
        // This is required when MemoryCache has SizeLimit configured
        if (string.IsNullOrEmpty(key))
            return 1;

        return key switch
        {
            var k when k.StartsWith("files") => 10,        // File lists can be large
            var k when k.StartsWith("categories") => 2,    // Category lists are small
            var k when k.StartsWith("configs") => 5,       // Config lists are medium
            var k when k.StartsWith("cache-stats") => 1,   // Stats are tiny
            _ => 3  // Default size for other entries
        };
    }

    private long EstimateMemoryUsage()
    {
        try
        {
            // Simple estimation based on key count and average object size
            const long averageObjectSize = 1024; // 1KB average per cached object
            return _keyCreationTimes.Count * averageObjectSize;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _taggedKeys.Clear();
            _keyCreationTimes.Clear();
            _logger.LogInformation("MemoryCacheService disposed");
        }
    }
}