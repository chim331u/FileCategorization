using FileCategorization_Web.Data.Caching;
using FileCategorization_Web.Features.FileManagement.Store;
using Fluxor;

namespace FileCategorization_Web.Services.Caching;

public interface IStateAwareCacheService : ICacheService
{
    Task InvalidateOnStateChangeAsync(FileState previousState, FileState currentState);
    Task WarmupCacheAsync();
    Task<T?> GetWithStateValidationAsync<T>(string key, Func<FileState, bool> stateValidator) where T : class;
}

public class StateAwareCacheService : IStateAwareCacheService, IDisposable
{
    private readonly MemoryCacheService _baseCacheService;
    private readonly IState<FileState> _state;
    private readonly ILogger<StateAwareCacheService> _logger;
    private FileState? _previousState;
    private bool _disposed = false;

    // Events from base cache service
    public event Action<string, object?>? CacheItemSet;
    public event Action<string>? CacheItemRemoved;
    public event Action<CacheInvalidationStrategy>? CacheInvalidated;

    public StateAwareCacheService(
        MemoryCacheService baseCacheService,
        IState<FileState> state,
        ILogger<StateAwareCacheService> logger)
    {
        _baseCacheService = baseCacheService;
        _state = state;
        _logger = logger;

        // Forward events from base service
        _baseCacheService.CacheItemSet += (key, value) => CacheItemSet?.Invoke(key, value);
        _baseCacheService.CacheItemRemoved += key => CacheItemRemoved?.Invoke(key);
        _baseCacheService.CacheInvalidated += strategy => CacheInvalidated?.Invoke(strategy);

        // Subscribe to state changes (Fluxor subscription pattern)
        // Note: In a real implementation, we would use proper Fluxor subscription pattern
        _previousState = _state.Value;

        _logger.LogInformation("StateAwareCacheService initialized");
    }

    private async void OnStateChanged(object? sender, FileState newState)
    {
        try
        {
            if (_previousState != null)
            {
                await InvalidateOnStateChangeAsync(_previousState, newState);
            }
            _previousState = newState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling state change in cache service");
        }
    }

    public async Task InvalidateOnStateChangeAsync(FileState previousState, FileState currentState)
    {
        try
        {
            var invalidationNeeded = false;
            var strategies = new List<CacheInvalidationStrategy>();

            // Check for files changes
            if (!previousState.Files.SequenceEqual(currentState.Files))
            {
                strategies.Add(CacheInvalidationStrategy.FileData);
                invalidationNeeded = true;
                _logger.LogDebug("Files changed, invalidating file data cache");
            }

            // Check for categories changes
            if (!previousState.Categories.SequenceEqual(currentState.Categories))
            {
                strategies.Add(CacheInvalidationStrategy.Categories);
                invalidationNeeded = true;
                _logger.LogDebug("Categories changed, invalidating categories cache");
            }

            // Check for configurations changes
            if (!previousState.Configurations.SequenceEqual(currentState.Configurations))
            {
                strategies.Add(CacheInvalidationStrategy.Configurations);
                invalidationNeeded = true;
                _logger.LogDebug("Configurations changed, invalidating configs cache");
            }

            // Check for search parameter changes
            if (previousState.SearchParameter != currentState.SearchParameter)
            {
                await InvalidateByTagsAsync("ui-data");
                invalidationNeeded = true;
                _logger.LogDebug("Search parameter changed, invalidating UI data cache");
            }

            // Apply invalidation strategies
            foreach (var strategy in strategies.Distinct())
            {
                await _baseCacheService.InvalidateAsync(strategy);
            }

            if (invalidationNeeded)
            {
                _logger.LogInformation("Cache invalidated due to state changes. Strategies: {Strategies}", 
                    string.Join(", ", strategies));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache on state change");
        }
    }

    public async Task WarmupCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting cache warmup");

            var currentState = _state.Value;

            // Pre-cache current files if available
            if (currentState.Files.Any())
            {
                var fileListKey = GenerateFileListKey(currentState.SearchParameter);
                await SetAsync(fileListKey, currentState.Files.ToList(), CachePolicy.FileList);
            }

            // Pre-cache categories if available
            if (currentState.Categories.Any())
            {
                var categoryListKey = GenerateCategoryListKey();
                await SetAsync(categoryListKey, currentState.Categories.ToList(), CachePolicy.Categories);
            }

            // Pre-cache configurations if available
            if (currentState.Configurations.Any())
            {
                var configListKey = GenerateConfigListKey();
                await SetAsync(configListKey, currentState.Configurations.ToList(), CachePolicy.Configurations);
            }

            _logger.LogInformation("Cache warmup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warmup");
        }
    }

    public async Task<T?> GetWithStateValidationAsync<T>(string key, Func<FileState, bool> stateValidator) where T : class
    {
        try
        {
            // Check if current state is valid for this cache entry
            if (!stateValidator(_state.Value))
            {
                _logger.LogDebug("State validation failed for cache key: {Key}, removing from cache", key);
                await RemoveAsync(key);
                return null;
            }

            return await GetAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value with state validation for key: {Key}", key);
            return null;
        }
    }

    // Delegate all ICacheService methods to base service
    public Task<T?> GetAsync<T>(string key) where T : class => _baseCacheService.GetAsync<T>(key);
    
    public Task<T?> GetAsync<T>(string key, Func<Task<T?>> factory, CachePolicy? policy = null) where T : class => 
        _baseCacheService.GetAsync(key, factory, policy);
    
    public Task SetAsync<T>(string key, T value, CachePolicy? policy = null) where T : class => 
        _baseCacheService.SetAsync(key, value, policy);
    
    public Task RemoveAsync(string key) => _baseCacheService.RemoveAsync(key);
    
    public Task RemoveByPatternAsync(string pattern) => _baseCacheService.RemoveByPatternAsync(pattern);
    
    public Task ClearAsync() => _baseCacheService.ClearAllAsync();
    
    public Task InvalidateAsync(CacheInvalidationStrategy strategy) => _baseCacheService.InvalidateAsync(strategy);
    
    public Task InvalidateByTagsAsync(params string[] tags) => _baseCacheService.InvalidateByTagsAsync(tags);
    
    public Task InvalidateByTagAsync(string tag) => _baseCacheService.InvalidateByTagAsync(tag);
    
    public Task ClearAllAsync() => _baseCacheService.ClearAllAsync();
    
    public CacheStatistics GetStatistics() => _baseCacheService.GetStatistics();
    
    public Task<CacheStatistics> GetStatisticsAsync() => _baseCacheService.GetStatisticsAsync();
    
    public string GenerateKey(string prefix, params object[] parameters) => 
        _baseCacheService.GenerateKey(prefix, parameters);
    
    public string GenerateFileListKey(int searchParameter) => _baseCacheService.GenerateFileListKey(searchParameter);
    
    public string GenerateCategoryListKey() => _baseCacheService.GenerateCategoryListKey();
    
    public string GenerateConfigListKey() => _baseCacheService.GenerateConfigListKey();

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Note: In a real implementation, we would unsubscribe from proper Fluxor subscription
            
            if (_baseCacheService is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _logger.LogInformation("StateAwareCacheService disposed");
        }
    }
}