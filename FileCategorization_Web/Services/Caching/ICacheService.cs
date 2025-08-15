using FileCategorization_Web.Data.Caching;

namespace FileCategorization_Web.Services.Caching;

public interface ICacheService
{
    // Basic cache operations
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<T?> GetAsync<T>(string key, Func<Task<T?>> factory, CachePolicy? policy = null) where T : class;
    Task SetAsync<T>(string key, T value, CachePolicy? policy = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAllAsync();

    // Cache invalidation
    Task InvalidateAsync(CacheInvalidationStrategy strategy);
    Task InvalidateByTagAsync(string tag);
    Task InvalidateByTagsAsync(params string[] tags);

    // Cache statistics
    CacheStatistics GetStatistics();
    Task<CacheStatistics> GetStatisticsAsync();

    // Cache key generation
    string GenerateKey(string prefix, params object[] parameters);
    string GenerateFileListKey(int searchParameter);
    string GenerateCategoryListKey();
    string GenerateConfigListKey();

    // Events for cache changes
    event Action<string, object?> CacheItemSet;
    event Action<string> CacheItemRemoved;
    event Action<CacheInvalidationStrategy> CacheInvalidated;
}