using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using Fluxor;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Features.FileManagement.Store;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Features.FileManagement.Effects;
using FileCategorization_Web.Data.Caching;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using FileCategorization_Web.Tests.Helpers;

namespace FileCategorization_Web.Tests.Integration;

public class CachingIntegrationTests : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ICacheService _cacheService;
    private readonly IState<FileState> _fileState;
    private readonly IDispatcher _dispatcher;

    public CachingIntegrationTests()
    {
        var services = FluxorTestHelper.CreateTestServiceCollection();
        
        // Add real caching services
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
        });
        services.AddScoped<MemoryCacheService>();
        services.AddScoped<StateAwareCacheService>();
        services.AddScoped<ICacheService>(provider => provider.GetRequiredService<StateAwareCacheService>());

        // Add mock file service
        services.AddMockServices();

        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _fileState = _serviceProvider.GetRequiredService<IState<FileState>>();
        _dispatcher = _serviceProvider.GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task CacheService_StoreAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var key = "integration-test-key";
        var value = new List<string> { "Item1", "Item2", "Item3" };
        var policy = CachePolicy.Categories;

        // Act
        await _cacheService.SetAsync(key, value, policy);
        var result = await _cacheService.GetAsync<List<string>>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public async Task CacheService_TagBasedInvalidation_WorksCorrectly()
    {
        // Arrange
        var filesKey1 = "files-test-1";
        var filesKey2 = "files-test-2";
        var categoriesKey = "categories-test";
        
        var filesValue = new List<FilesDetailDto> { new() { Id = 1, Name = "Test.txt" } };
        var categoriesValue = new List<string> { "Category1" };

        await _cacheService.SetAsync(filesKey1, filesValue, CachePolicy.FileList);
        await _cacheService.SetAsync(filesKey2, filesValue, CachePolicy.FileList);
        await _cacheService.SetAsync(categoriesKey, categoriesValue, CachePolicy.Categories);

        // Act - Invalidate only "files" tagged items
        await _cacheService.InvalidateByTagAsync("files");

        // Assert
        (await _cacheService.GetAsync<List<FilesDetailDto>>(filesKey1)).Should().BeNull();
        (await _cacheService.GetAsync<List<FilesDetailDto>>(filesKey2)).Should().BeNull();
        (await _cacheService.GetAsync<List<string>>(categoriesKey)).Should().NotBeNull(); // Different tag
    }

    [Fact]
    public async Task CacheService_Statistics_UpdateCorrectly()
    {
        // Arrange
        var key1 = "stats-test-1";
        var key2 = "stats-test-2";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key1, value, CachePolicy.FileList);
        await _cacheService.SetAsync(key2, value, CachePolicy.FileList);
        
        // Trigger some hits and misses
        await _cacheService.GetAsync<string>(key1); // Hit
        await _cacheService.GetAsync<string>("non-existent"); // Miss

        var stats = await _cacheService.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalItems.Should().BeGreaterThan(0);
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task StateAwareCacheService_DelegatesCorrectly()
    {
        // Arrange
        var stateAwareCacheService = _serviceProvider.GetRequiredService<StateAwareCacheService>();
        var key = "state-aware-test";
        var value = "test-value";

        // Act
        await stateAwareCacheService.SetAsync(key, value, CachePolicy.FileList);
        var result = await stateAwareCacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task StateAwareCacheService_StateValidation_WorksCorrectly()
    {
        // Arrange
        var stateAwareCacheService = _serviceProvider.GetRequiredService<StateAwareCacheService>();
        var key = "state-validation-test";
        var value = "test-value";

        await stateAwareCacheService.SetAsync(key, value, CachePolicy.FileList);

        // Act & Assert - Valid state
        var validResult = await stateAwareCacheService.GetWithStateValidationAsync<string>(
            key, 
            state => !state.IsLoading); // Valid when not loading
            
        validResult.Should().Be(value);

        // Act & Assert - Invalid state
        var invalidResult = await stateAwareCacheService.GetWithStateValidationAsync<string>(
            key, 
            state => state.IsLoading); // Invalid when loading (current state is not loading)
            
        invalidResult.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_ConcurrentAccess_HandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "concurrent-test";

        // Act - Multiple concurrent cache operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"{keyPrefix}-{index}";
                var value = $"value-{index}";
                
                await _cacheService.SetAsync(key, value, CachePolicy.FileList);
                var result = await _cacheService.GetAsync<string>(key);
                
                result.Should().Be(value);
            }));
        }

        // Assert - All operations complete successfully
        await Task.WhenAll(tasks);
        
        var stats = await _cacheService.GetStatisticsAsync();
        stats.TotalItems.Should().BeGreaterOrEqualTo(10);
    }

    [Fact]
    public async Task CacheService_LargeData_HandledCorrectly()
    {
        // Arrange
        var largeData = Enumerable.Range(0, 1000)
            .Select(i => new FilesDetailDto { Id = i, Name = $"File{i}.txt" })
            .ToList();

        var key = "large-data-test";

        // Act
        await _cacheService.SetAsync(key, largeData, CachePolicy.FileList);
        var result = await _cacheService.GetAsync<List<FilesDetailDto>>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1000);
        result!.First().Name.Should().Be("File0.txt");
        result.Last().Name.Should().Be("File999.txt");
    }

    [Fact]
    public async Task CacheService_ExpirationPolicy_WorksCorrectly()
    {
        // Arrange
        var key = "expiration-test";
        var value = "test-value";
        var shortExpirationPolicy = new CachePolicy
        {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100),
            Tags = new List<string> { "test" }
        };

        // Act
        await _cacheService.SetAsync(key, value, shortExpirationPolicy);
        
        // Immediately should be available
        var immediateResult = await _cacheService.GetAsync<string>(key);
        immediateResult.Should().Be(value);
        
        // Wait for expiration
        await Task.Delay(150);
        
        // Should be expired
        var expiredResult = await _cacheService.GetAsync<string>(key);
        
        // Assert
        expiredResult.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_ClearAll_RemovesAllEntries()
    {
        // Arrange
        var keys = new[] { "clear-test-1", "clear-test-2", "clear-test-3" };
        var value = "test-value";

        foreach (var key in keys)
        {
            await _cacheService.SetAsync(key, value, CachePolicy.FileList);
        }

        // Verify all items are cached
        foreach (var key in keys)
        {
            (await _cacheService.GetAsync<string>(key)).Should().Be(value);
        }

        // Act
        await _cacheService.ClearAllAsync();

        // Assert
        foreach (var key in keys)
        {
            (await _cacheService.GetAsync<string>(key)).Should().BeNull();
        }

        var stats = await _cacheService.GetStatisticsAsync();
        stats.TotalItems.Should().Be(0);
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }
}