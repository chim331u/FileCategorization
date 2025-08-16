using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Infrastructure.Data.Repositories;
using FileCategorization_Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for ActionsRepository.
/// </summary>
public class ActionsRepositoryTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ActionsRepository _repository;
    private readonly Mock<ILogger<ActionsRepository>> _mockLogger;

    /// <summary>
    /// Initializes a new instance of the ActionsRepositoryTests class.
    /// </summary>
    public ActionsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationContext(options);
        _mockLogger = new Mock<ILogger<ActionsRepository>>();
        _repository = new ActionsRepository(_context, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ActionsRepository(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ActionsRepository(_context, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_repository);
    }

    #endregion

    #region GetFilesByIdsAsync Tests

    [Fact]
    public async Task GetFilesByIdsAsync_WithNullFileIds_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _repository.GetFilesByIdsAsync(null!);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetFilesByIdsAsync_WithEmptyFileIds_ReturnsEmptyDictionary()
    {
        // Arrange
        var fileIds = new List<int>();

        // Act
        var result = await _repository.GetFilesByIdsAsync(fileIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetFilesByIdsAsync_WithExistingFiles_ReturnsFiles()
    {
        // Arrange
        var files = new[]
        {
            new FilesDetail { Id = 1, Name = "file1.txt", FileCategory = "Document" },
            new FilesDetail { Id = 2, Name = "file2.jpg", FileCategory = "Image" },
            new FilesDetail { Id = 3, Name = "file3.mp4", FileCategory = "Video" }
        };

        await _context.FilesDetail.AddRangeAsync(files);
        await _context.SaveChangesAsync();

        var fileIds = new List<int> { 1, 2 };

        // Act
        var result = await _repository.GetFilesByIdsAsync(fileIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(1, result.Value.Keys);
        Assert.Contains(2, result.Value.Keys);
        Assert.DoesNotContain(3, result.Value.Keys);
    }

    [Fact]
    public async Task GetFilesByIdsAsync_WithNonExistentFiles_ReturnsEmptyForMissing()
    {
        // Arrange
        var fileIds = new List<int> { 999, 1000 };

        // Act
        var result = await _repository.GetFilesByIdsAsync(fileIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetFilesByIdsAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var fileIds = new List<int> { 1, 2, 3 };
        var cts = new CancellationTokenSource();
        // Don't cancel immediately - test that the token is passed through

        // Act
        var result = await _repository.GetFilesByIdsAsync(fileIds, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!); // No files exist with those IDs
    }

    #endregion

    #region BatchUpdateFilesAsync Tests

    [Fact]
    public async Task BatchUpdateFilesAsync_WithNullFiles_ReturnsZero()
    {
        // Act
        var result = await _repository.BatchUpdateFilesAsync(null!);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task BatchUpdateFilesAsync_WithEmptyFiles_ReturnsZero()
    {
        // Arrange
        var files = new List<FilesDetail>();

        // Act
        var result = await _repository.BatchUpdateFilesAsync(files);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task BatchUpdateFilesAsync_WithValidFiles_UpdatesFiles()
    {
        // Arrange
        var originalFiles = new[]
        {
            new FilesDetail { Id = 1, Name = "file1.txt", FileCategory = "Unknown" },
            new FilesDetail { Id = 2, Name = "file2.jpg", FileCategory = "Unknown" }
        };

        await _context.FilesDetail.AddRangeAsync(originalFiles);
        await _context.SaveChangesAsync();

        // Detach entities to avoid tracking conflicts
        _context.ChangeTracker.Clear();

        var updatedFiles = new List<FilesDetail>
        {
            new() { Id = 1, Name = "file1.txt", FileCategory = "Document" },
            new() { Id = 2, Name = "file2.jpg", FileCategory = "Image" }
        };

        // Act
        var result = await _repository.BatchUpdateFilesAsync(updatedFiles);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        // Verify updates in database
        var dbFile1 = await _context.FilesDetail.FindAsync(1);
        var dbFile2 = await _context.FilesDetail.FindAsync(2);
        
        Assert.Equal("Document", dbFile1!.FileCategory);
        Assert.Equal("Image", dbFile2!.FileCategory);
    }

    [Fact]
    public async Task BatchUpdateFilesAsync_WithNonExistentFiles_SkipsMissing()
    {
        // Arrange
        var files = new List<FilesDetail>
        {
            new() { Id = 999, Name = "nonexistent.txt", FileCategory = "Document" }
        };

        // Act
        var result = await _repository.BatchUpdateFilesAsync(files);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value); // No files updated
    }

    [Fact]
    public async Task BatchUpdateFilesAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "file1.txt", FileCategory = "Document" }
        };

        var cts = new CancellationTokenSource();
        // Don't cancel immediately - test that the token is passed through

        // Act
        var result = await _repository.BatchUpdateFilesAsync(files, cts.Token);

        // Assert - Will succeed but update 0 files since they don't exist
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    #endregion

    #region GetExistingFileNamesAsync Tests

    [Fact]
    public async Task GetExistingFileNamesAsync_WithNoFiles_ReturnsEmptySet()
    {
        // Arrange
        var fileNames = new List<string> { "file1.txt", "file2.jpg" };

        // Act
        var result = await _repository.GetExistingFileNamesAsync(fileNames);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetExistingFileNamesAsync_WithFiles_ReturnsFileNames()
    {
        // Arrange
        var files = new[]
        {
            new FilesDetail { Id = 1, Name = "file1.txt" },
            new FilesDetail { Id = 2, Name = "file2.jpg" },
            new FilesDetail { Id = 3, Name = "file3.mp4" }
        };

        await _context.FilesDetail.AddRangeAsync(files);
        await _context.SaveChangesAsync();

        var searchNames = new List<string> { "file1.txt", "file2.jpg", "nonexistent.pdf" };

        // Act
        var result = await _repository.GetExistingFileNamesAsync(searchNames);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains("file1.txt", result.Value);
        Assert.Contains("file2.jpg", result.Value);
        Assert.DoesNotContain("file3.mp4", result.Value);
        Assert.DoesNotContain("nonexistent.pdf", result.Value);
    }

    [Fact]
    public async Task GetExistingFileNamesAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var fileNames = new List<string> { "file1.txt" };
        var cts = new CancellationTokenSource();
        // Don't cancel immediately - test that the token is passed through

        // Act
        var result = await _repository.GetExistingFileNamesAsync(fileNames, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!); // No files exist
    }

    #endregion

    #region BatchAddFilesAsync Tests

    [Fact]
    public async Task BatchAddFilesAsync_WithValidFiles_AddsFiles()
    {
        // Arrange
        var files = new List<FilesDetail>
        {
            new() { Name = "file1.txt", FileCategory = "Document" },
            new() { Name = "file2.jpg", FileCategory = "Image" }
        };

        // Act
        var result = await _repository.BatchAddFilesAsync(files);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        // Verify files were added to database
        var dbFiles = await _context.FilesDetail.ToListAsync();
        Assert.Equal(2, dbFiles.Count);
        Assert.Contains(dbFiles, f => f.Name == "file1.txt");
        Assert.Contains(dbFiles, f => f.Name == "file2.jpg");
    }

    [Fact]
    public async Task BatchAddFilesAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var files = new List<FilesDetail>();

        // Act
        var result = await _repository.BatchAddFilesAsync(files);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    #endregion

    #region BatchAppendTrainingDataAsync Tests

    [Fact]
    public async Task BatchAppendTrainingDataAsync_WithValidData_AppendsToFile()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        var trainingEntries = new List<string>
        {
            "1;Document;file1.txt",
            "2;Image;file2.jpg"
        };

        try
        {
            // Act
            var result = await _repository.BatchAppendTrainingDataAsync(trainingEntries, tempFilePath);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value);

            // Verify file content
            var content = await File.ReadAllTextAsync(tempFilePath);
            Assert.Contains("file1.txt", content);
            Assert.Contains("file2.jpg", content);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task BatchAppendTrainingDataAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        var trainingEntries = new List<string>();

        try
        {
            // Act
            var result = await _repository.BatchAppendTrainingDataAsync(trainingEntries, tempFilePath);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task BatchUpdateFilesAsync_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange - Create a large number of files
        var files = new List<FilesDetail>();
        for (int i = 1; i <= 1000; i++)
        {
            files.Add(new FilesDetail { Id = i, Name = $"file{i}.txt", FileCategory = "Unknown" });
        }

        await _context.FilesDetail.AddRangeAsync(files);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Update all files
        var updatedFiles = files.Select(f => new FilesDetail 
        { 
            Id = f.Id, 
            Name = f.Name, 
            FileCategory = "Document" 
        }).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.BatchUpdateFilesAsync(updatedFiles);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value);
        
        // Performance assertion - should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Batch update took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetFilesByIdsAsync_WithDatabaseError_ReturnsFailure()
    {
        // Arrange - Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act
        var result = await _repository.GetFilesByIdsAsync(new List<int> { 1 });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot access a disposed context", result.Error);
    }

    [Fact]
    public async Task BatchUpdateFilesAsync_WithDatabaseError_ReturnsFailure()
    {
        // Arrange - Dispose context to simulate database error
        await _context.DisposeAsync();

        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "file1.txt", FileCategory = "Document" }
        };

        // Act
        var result = await _repository.BatchUpdateFilesAsync(files);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot access a disposed context", result.Error);
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _context?.Dispose();
    }

    #endregion
}