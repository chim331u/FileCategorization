using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Infrastructure.Data.Repositories;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for ConfigRepository.
/// </summary>
public class ConfigRepositoryTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly Mock<ILogger<ConfigRepository>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly ConfigRepository _repository;

    /// <summary>
    /// Initializes a new instance of the ConfigRepositoryTests class.
    /// </summary>
    public ConfigRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationContext(options);
        _mockLogger = new Mock<ILogger<ConfigRepository>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        _repository = new ConfigRepository(_context, _mockLogger.Object, _mockEnvironment.Object);
    }

    [Fact]
    public async Task GetByKeyAsync_WithValidKey_ReturnsConfig()
    {
        // Arrange
        var config = new Configs
        {
            Key = "TestKey",
            Value = "TestValue",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByKeyAsync("TestKey");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("TestKey", result.Value.Key);
        Assert.Equal("TestValue", result.Value.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_WithNonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByKeyAsync("NonExistentKey");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_WithEmptyKey_ReturnsFailure()
    {
        // Act
        var result = await _repository.GetByKeyAsync("");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be null or empty", result.Error);
    }

    [Fact]
    public async Task GetValueByKeyAsync_WithValidKey_ReturnsValue()
    {
        // Arrange
        var config = new Configs
        {
            Key = "TestKey",
            Value = "TestValue",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetValueByKeyAsync("TestKey");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TestValue", result.Value);
    }

    [Fact]
    public async Task KeyExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var config = new Configs
        {
            Key = "ExistingKey",
            Value = "Value",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.KeyExistsAsync("ExistingKey");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task KeyExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _repository.KeyExistsAsync("NonExistentKey");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task KeyExistsAsync_WithExcludeId_ExcludesSpecificRecord()
    {
        // Arrange
        var config = new Configs
        {
            Key = "TestKey",
            Value = "Value",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(config);
        await _context.SaveChangesAsync();

        // Act - exclude the same ID
        var result = await _repository.KeyExistsAsync("TestKey", config.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value); // Should return false because we excluded the matching record
    }

    [Fact]
    public async Task GetByEnvironmentAsync_WithDevEnvironment_ReturnsDevConfigs()
    {
        // Arrange
        var devConfig = new Configs
        {
            Key = "DevKey",
            Value = "DevValue",
            IsDev = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        var prodConfig = new Configs
        {
            Key = "ProdKey",
            Value = "ProdValue",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.AddRange(devConfig, prodConfig);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEnvironmentAsync(true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("DevKey", result.Value.First().Key);
    }

    [Fact]
    public async Task AddAsync_WithValidConfig_AddsSuccessfully()
    {
        // Arrange
        var config = new Configs
        {
            Key = "NewKey",
            Value = "NewValue",
            IsDev = false
        };

        // Act
        var result = await _repository.AddAsync(config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NewKey", result.Value!.Key);
        Assert.Equal("NewValue", result.Value.Value);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateKey_ReturnsFailure()
    {
        // Arrange
        var existingConfig = new Configs
        {
            Key = "DuplicateKey",
            Value = "Value1",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(existingConfig);
        await _context.SaveChangesAsync();

        var newConfig = new Configs
        {
            Key = "DuplicateKey",
            Value = "Value2",
            IsDev = false
        };

        // Act
        var result = await _repository.AddAsync(newConfig);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task UpdateEnvironmentAsync_WithValidId_UpdatesSuccessfully()
    {
        // Arrange
        var config = new Configs
        {
            Key = "TestKey",
            Value = "TestValue",
            IsDev = false,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Configuration.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UpdateEnvironmentAsync(config.Id, true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        // Verify the update in database
        var updatedConfig = await _context.Configuration.FindAsync(config.Id);
        Assert.True(updatedConfig?.IsDev);
    }

    [Fact]
    public async Task UpdateEnvironmentAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.UpdateEnvironmentAsync(999, true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    /// <summary>
    /// Disposes the test context.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}