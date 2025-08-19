using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Infrastructure.Data.Repositories;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Tests for NotShowAgain functionality in FilesDetailRepository.
/// </summary>
public class NotShowAgainTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly Mock<ILogger<Repository<FilesDetail>>> _loggerMock;
    private readonly FilesDetailRepository _repository;

    public NotShowAgainTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationContext(options);
        _loggerMock = new Mock<ILogger<Repository<FilesDetail>>>();
        _repository = new FilesDetailRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateNotShowAgainAsync_WithValidId_UpdatesFileSuccessfully()
    {
        // Arrange
        var testFile = new FilesDetail
        {
            Id = 1,
            Name = "TestFile.txt",
            Path = "/test/path",
            FileSize = 1024,
            LastUpdateFile = DateTime.Now.AddDays(-1),
            IsNotToMove = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        await _context.FilesDetail.AddAsync(testFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UpdateNotShowAgainAsync(testFile.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        // Verify the file was updated correctly
        var updatedFile = await _context.FilesDetail.FindAsync(testFile.Id);
        Assert.NotNull(updatedFile);
        Assert.False(updatedFile.IsNotToMove);
        Assert.True(updatedFile.LastUpdatedDate > testFile.LastUpdatedDate);
    }

    [Fact]
    public async Task UpdateNotShowAgainAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _repository.UpdateNotShowAgainAsync(nonExistentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task UpdateNotShowAgainAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var invalidId = -1;

        // Act
        var result = await _repository.UpdateNotShowAgainAsync(invalidId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid file ID", result.Error);
    }

    [Fact]
    public async Task UpdateNotShowAgainAsync_WithZeroId_ReturnsFailure()
    {
        // Arrange
        var zeroId = 0;

        // Act
        var result = await _repository.UpdateNotShowAgainAsync(zeroId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid file ID", result.Error);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}