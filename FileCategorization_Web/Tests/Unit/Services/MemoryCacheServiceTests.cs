using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Data.Caching;

namespace FileCategorization_Web.Tests.Unit.Services;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly Mock<ILogger<MemoryCacheService>> _loggerMock;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions 
        { 
            SizeLimit = 1000 
        });
        _loggerMock = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_AndGetAsync_StoresAndRetrievesData()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var policy = CachePolicy.FileList;

        // Act
        await _cacheService.SetAsync(key, value, policy);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithNullValue_DoesNotCache()
    {
        // Arrange
        var key = "test-key";
        string? value = null;

        // Act
        await _cacheService.SetAsync(key, value, CachePolicy.FileList);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesFromCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value, CachePolicy.FileList);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateByTagAsync_RemovesTaggedItems()
    {
        // Arrange
        var key1 = "files-key-1";
        var key2 = "files-key-2";
        var key3 = "categories-key";
        
        await _cacheService.SetAsync(key1, "value1", CachePolicy.FileList);
        await _cacheService.SetAsync(key2, "value2", CachePolicy.FileList);
        await _cacheService.SetAsync(key3, "value3", CachePolicy.Categories);

        // Act
        await _cacheService.InvalidateByTagAsync("files");

        // Assert
        (await _cacheService.GetAsync<string>(key1)).Should().BeNull();
        (await _cacheService.GetAsync<string>(key2)).Should().BeNull();
        (await _cacheService.GetAsync<string>(key3)).Should().NotBeNull(); // Different tag
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsValidStatistics()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1", CachePolicy.FileList);
        await _cacheService.SetAsync("key2", "value2", CachePolicy.Categories);

        // Act
        var stats = await _cacheService.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalItems.Should().BeGreaterThan(0);
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllCacheEntries()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1", CachePolicy.FileList);
        await _cacheService.SetAsync("key2", "value2", CachePolicy.Categories);

        // Act
        await _cacheService.ClearAllAsync();

        // Assert
        (await _cacheService.GetAsync<string>("key1")).Should().BeNull();
        (await _cacheService.GetAsync<string>("key2")).Should().BeNull();
        
        var stats = await _cacheService.GetStatisticsAsync();
        stats.TotalItems.Should().Be(0);
    }

    [Theory]
    [InlineData("files*", "files-123")]
    [InlineData("cat*", "categories")]
    [InlineData("*config*", "app-config-1")]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys(string pattern, string keyToSet)
    {
        // Arrange
        await _cacheService.SetAsync(keyToSet, "test-value", CachePolicy.FileList);
        await _cacheService.SetAsync("other-key", "other-value", CachePolicy.FileList);

        // Act
        await _cacheService.RemoveByPatternAsync(pattern);

        // Assert
        (await _cacheService.GetAsync<string>(keyToSet)).Should().BeNull();
        (await _cacheService.GetAsync<string>("other-key")).Should().NotBeNull();
    }

    [Fact]
    public async Task CacheService_SetAndRemove_WorksCorrectly()
    {
        // Arrange
        var key = "event-test-key";
        var value = "event-test-value";
        
        // Act
        await _cacheService.SetAsync(key, value, CachePolicy.FileList);
        var retrievedValue = await _cacheService.GetAsync<string>(key);
        
        await _cacheService.RemoveAsync(key);
        var removedValue = await _cacheService.GetAsync<string>(key);

        // Assert
        retrievedValue.Should().Be(value);
        removedValue.Should().BeNull();
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        _cacheService?.Dispose();
    }
}