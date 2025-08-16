using FileCategorization_Api.Domain.Enums;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using AutoMapper;
using FileCategorization_Api.Services;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.Enums;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for FilesQueryService to demonstrate improved testability with Repository Pattern.
/// </summary>
public class FilesQueryServiceTests
{
    private readonly Mock<IFilesDetailRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<FilesQueryService>> _mockLogger;
    private readonly FilesQueryService _service;

    public FilesQueryServiceTests()
    {
        _mockRepository = new Mock<IFilesDetailRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<FilesQueryService>>();
        
        _service = new FilesQueryService(_mockRepository.Object, _mockLogger.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnSuccess_WhenRepositoryReturnsCategories()
    {
        // Arrange
        var expectedCategories = new List<string> { "Movies", "Music", "Documents" };
        var repositoryResult = Result<IEnumerable<string>>.Success(expectedCategories);

        _mockRepository
            .Setup(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCategories, result.Value);
        _mockRepository.Verify(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnFailure_WhenRepositoryFails()
    {
        // Arrange
        var errorMessage = "Database connection failed";
        var repositoryResult = Result<IEnumerable<string>>.Failure(errorMessage);

        _mockRepository
            .Setup(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _mockRepository.Verify(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredFilesAsync_ShouldReturnMappedFiles_WhenRepositoryReturnsFiles()
    {
        // Arrange
        var filterType = FileFilterType.ToCategorize;
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "test1.txt", FileCategory = "Documents" },
            new() { Id = 2, Name = "test2.txt", FileCategory = "Documents" }
        };
        var expectedResponses = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "test1.txt", FileCategory = "Documents" },
            new() { Id = 2, Name = "test2.txt", FileCategory = "Documents" }
        };

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(files);

        _mockRepository
            .Setup(r => r.GetFilteredFilesAsync((int)filterType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(files))
            .Returns(expectedResponses);

        // Act
        var result = await _service.GetFilteredFilesAsync(filterType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponses, result.Value);
        _mockRepository.Verify(r => r.GetFilteredFilesAsync((int)filterType, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(files), Times.Once);
    }

    [Fact]
    public async Task GetFilteredFilesAsync_ShouldReturnFailure_WhenRepositoryFails()
    {
        // Arrange
        var filterType = FileFilterType.All;
        var errorMessage = "Repository error";
        var repositoryResult = Result<IEnumerable<FilesDetail>>.Failure(errorMessage);

        _mockRepository
            .Setup(r => r.GetFilteredFilesAsync((int)filterType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.GetFilteredFilesAsync(filterType);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _mockRepository.Verify(r => r.GetFilteredFilesAsync((int)filterType, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(It.IsAny<IEnumerable<FilesDetail>>()), Times.Never);
    }

    [Fact]
    public async Task SearchFilesByNameAsync_ShouldReturnFailure_WhenPatternIsEmpty()
    {
        // Arrange
        var emptyPattern = "";

        // Act
        var result = await _service.SearchFilesByNameAsync(emptyPattern);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Search pattern cannot be empty", result.Error);
        _mockRepository.Verify(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchFilesByNameAsync_ShouldReturnSuccess_WhenPatternIsValid()
    {
        // Arrange
        var pattern = "test";
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "test1.txt" },
            new() { Id = 2, Name = "test2.pdf" }
        };
        var expectedResponses = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "test1.txt" },
            new() { Id = 2, Name = "test2.pdf" }
        };

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(files);

        _mockRepository
            .Setup(r => r.SearchByNameAsync(pattern, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(files))
            .Returns(expectedResponses);

        // Act
        var result = await _service.SearchFilesByNameAsync(pattern);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponses, result.Value);
        _mockRepository.Verify(r => r.SearchByNameAsync(pattern, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(files), Times.Once);
    }

    [Fact]
    public async Task GetLastViewListAsync_ShouldReturnMappedFiles_WhenRepositoryReturnsFiles()
    {
        // Arrange
        var latestFiles = new List<FilesDetail>
        {
            new() { Id = 1, Name = "latest_movie.mkv", FileCategory = "Movies" },
            new() { Id = 2, Name = "latest_song.mp3", FileCategory = "Music" }
        };
        var expectedResponses = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "latest_movie.mkv", FileCategory = "Movies" },
            new() { Id = 2, Name = "latest_song.mp3", FileCategory = "Music" }
        };

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(latestFiles);

        _mockRepository
            .Setup(r => r.GetLatestFilesByCategoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(latestFiles))
            .Returns(expectedResponses);

        // Act
        var result = await _service.GetLastViewListAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResponses, result.Value);
        _mockRepository.Verify(r => r.GetLatestFilesByCategoryAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(latestFiles), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnFailure_WhenCategoryIsNull()
    {
        // Arrange
        string? nullCategory = null;

        // Act
        var result = await _service.GetFilesByCategoryAsync(nullCategory!);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Category cannot be null or empty", result.Error);
        _mockRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(It.IsAny<IEnumerable<FilesDetail>>()), Times.Never);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnFailure_WhenCategoryIsEmpty()
    {
        // Arrange
        var emptyCategory = "";

        // Act
        var result = await _service.GetFilesByCategoryAsync(emptyCategory);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Category cannot be null or empty", result.Error);
        _mockRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(It.IsAny<IEnumerable<FilesDetail>>()), Times.Never);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnFailure_WhenCategoryIsWhitespace()
    {
        // Arrange
        var whitespaceCategory = "   ";

        // Act
        var result = await _service.GetFilesByCategoryAsync(whitespaceCategory);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Category cannot be null or empty", result.Error);
        _mockRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(It.IsAny<IEnumerable<FilesDetail>>()), Times.Never);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnSuccess_WhenRepositoryReturnsFiles()
    {
        // Arrange
        var category = "Movies";
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = "movie1.mkv", FileCategory = "Movies", FileSize = 1024 },
            new() { Id = 2, Name = "movie2.mp4", FileCategory = "Movies", FileSize = 2048 },
            new() { Id = 3, Name = "movie3.avi", FileCategory = "Movies", FileSize = 1536 }
        };
        var expectedResponses = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "movie1.mkv", FileCategory = "Movies", FileSize = 1024 },
            new() { Id = 2, Name = "movie2.mp4", FileCategory = "Movies", FileSize = 2048 },
            new() { Id = 3, Name = "movie3.avi", FileCategory = "Movies", FileSize = 1536 }
        };

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(files);

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(files))
            .Returns(expectedResponses);

        // Act
        var result = await _service.GetFilesByCategoryAsync(category);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Count());
        Assert.Equal(expectedResponses, result.Value);
        _mockRepository.Verify(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(files), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnEmptyList_WhenNoCategoryFilesExist()
    {
        // Arrange
        var category = "EmptyCategory";
        var emptyFiles = new List<FilesDetail>();
        var emptyResponses = new List<FilesDetailResponse>();

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(emptyFiles);

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(emptyFiles))
            .Returns(emptyResponses);

        // Act
        var result = await _service.GetFilesByCategoryAsync(category);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
        _mockRepository.Verify(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(emptyFiles), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategoryAsync_ShouldReturnFailure_WhenRepositoryFails()
    {
        // Arrange
        var category = "Movies";
        var errorMessage = "Database connection timeout";
        var repositoryResult = Result<IEnumerable<FilesDetail>>.Failure(errorMessage);

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.GetFilesByCategoryAsync(category);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _mockRepository.Verify(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<FilesDetailResponse>>(It.IsAny<IEnumerable<FilesDetail>>()), Times.Never);
    }

    [Theory]
    [InlineData("Movies")]
    [InlineData("Music")]
    [InlineData("Documents")]
    [InlineData("Software")]
    [InlineData("Unknown")]
    public async Task GetFilesByCategoryAsync_ShouldHandleDifferentCategories_Successfully(string category)
    {
        // Arrange
        var files = new List<FilesDetail>
        {
            new() { Id = 1, Name = $"file1.{category.ToLower()}", FileCategory = category }
        };
        var expectedResponses = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = $"file1.{category.ToLower()}", FileCategory = category }
        };

        var repositoryResult = Result<IEnumerable<FilesDetail>>.Success(files);

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResult);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<FilesDetailResponse>>(files))
            .Returns(expectedResponses);

        // Act
        var result = await _service.GetFilesByCategoryAsync(category);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(category, result.Value!.First().FileCategory);
        _mockRepository.Verify(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }
}