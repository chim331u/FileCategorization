using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Fluxor;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Features.FileManagement.Store;
using FileCategorization_Web.Data.Caching;

namespace FileCategorization_Web.Tests.Unit.Services;

public class StateAwareCacheServiceTests : IDisposable
{
    private readonly Mock<MemoryCacheService> _baseCacheServiceMock;
    private readonly Mock<IState<FileState>> _stateMock;
    private readonly Mock<ILogger<StateAwareCacheService>> _loggerMock;
    private readonly StateAwareCacheService _stateAwareCacheService;

    public StateAwareCacheServiceTests()
    {
        _baseCacheServiceMock = new Mock<MemoryCacheService>();
        _stateMock = new Mock<IState<FileState>>();
        _loggerMock = new Mock<ILogger<StateAwareCacheService>>();

        // Setup default state
        _stateMock.Setup(x => x.Value).Returns(FileState.InitialState);

        _stateAwareCacheService = new StateAwareCacheService(
            _baseCacheServiceMock.Object,
            _stateMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        _baseCacheServiceMock.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _stateAwareCacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(expectedValue);
        _baseCacheServiceMock.Verify(x => x.GetAsync<string>(key), Times.Once);
    }

    [Fact]
    public async Task SetAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var policy = CachePolicy.FileList;

        _baseCacheServiceMock.Setup(x => x.SetAsync(key, value, policy))
            .Returns(Task.CompletedTask);

        // Act
        await _stateAwareCacheService.SetAsync(key, value, policy);

        // Assert
        _baseCacheServiceMock.Verify(x => x.SetAsync(key, value, policy), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        var key = "test-key";
        _baseCacheServiceMock.Setup(x => x.RemoveAsync(key))
            .Returns(Task.CompletedTask);

        // Act
        await _stateAwareCacheService.RemoveAsync(key);

        // Assert
        _baseCacheServiceMock.Verify(x => x.RemoveAsync(key), Times.Once);
    }

    [Fact]
    public async Task InvalidateByTagAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        var tag = "files";
        _baseCacheServiceMock.Setup(x => x.InvalidateByTagAsync(tag))
            .Returns(Task.CompletedTask);

        // Act
        await _stateAwareCacheService.InvalidateByTagAsync(tag);

        // Assert
        _baseCacheServiceMock.Verify(x => x.InvalidateByTagAsync(tag), Times.Once);
    }

    [Fact]
    public async Task ClearAllAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        _baseCacheServiceMock.Setup(x => x.ClearAllAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _stateAwareCacheService.ClearAllAsync();

        // Assert
        _baseCacheServiceMock.Verify(x => x.ClearAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToBaseCacheService()
    {
        // Arrange
        var expectedStats = new CacheStatistics 
        { 
            TotalItems = 10,
            HitCount = 15,
            MissCount = 5,
            LastUpdated = DateTime.UtcNow
        };
        
        _baseCacheServiceMock.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _stateAwareCacheService.GetStatisticsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedStats);
        _baseCacheServiceMock.Verify(x => x.GetStatisticsAsync(), Times.Once);
    }

    [Fact]
    public async Task WarmupCacheAsync_CompletesSuccessfully()
    {
        // Act
        var act = () => _stateAwareCacheService.WarmupCacheAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateOnStateChangeAsync_WithDifferentStates_CompletesSuccessfully()
    {
        // Arrange
        var previousState = FileState.InitialState;
        var currentState = FileState.InitialState with { IsLoading = true };

        // Act
        var act = () => _stateAwareCacheService.InvalidateOnStateChangeAsync(previousState, currentState);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetWithStateValidationAsync_WithValidState_ReturnsData()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        var validator = (FileState state) => true; // Always valid

        _baseCacheServiceMock.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _stateAwareCacheService.GetWithStateValidationAsync<string>(key, validator);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetWithStateValidationAsync_WithInvalidState_ReturnsNull()
    {
        // Arrange
        var key = "test-key";
        var validator = (FileState state) => false; // Always invalid

        // Act
        var result = await _stateAwareCacheService.GetWithStateValidationAsync<string>(key, validator);

        // Assert
        result.Should().BeNull();
        _baseCacheServiceMock.Verify(x => x.GetAsync<string>(key), Times.Never);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act - constructor is called in setup

        // Assert
        _stateAwareCacheService.Should().NotBeNull();
        
        // Verify that the logger was called during initialization
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("StateAwareCacheService initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EventForwarding_ForwardsEventsFromBaseCacheService()
    {
        // Arrange
        string? lastSetKey = null;
        object? lastSetValue = null;
        string? lastRemovedKey = null;
        CacheInvalidationStrategy? lastInvalidatedStrategy = null;

        _stateAwareCacheService.CacheItemSet += (key, value) => 
        {
            lastSetKey = key;
            lastSetValue = value;
        };

        _stateAwareCacheService.CacheItemRemoved += key => 
        {
            lastRemovedKey = key;
        };

        _stateAwareCacheService.CacheInvalidated += strategy => 
        {
            lastInvalidatedStrategy = strategy;
        };

        // Act - Trigger events from base service
        _baseCacheServiceMock.Raise(x => x.CacheItemSet += null, "test-key", "test-value");
        _baseCacheServiceMock.Raise(x => x.CacheItemRemoved += null, "test-key");
        _baseCacheServiceMock.Raise(x => x.CacheInvalidated += null, CacheInvalidationStrategy.FileData);

        // Assert
        lastSetKey.Should().Be("test-key");
        lastSetValue.Should().Be("test-value");
        lastRemovedKey.Should().Be("test-key");
        lastInvalidatedStrategy.Should().Be(CacheInvalidationStrategy.FileData);
    }

    public void Dispose()
    {
        _stateAwareCacheService?.Dispose();
    }
}