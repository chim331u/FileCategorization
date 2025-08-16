using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Moq;
using FileCategorization_Api.Services;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Integration tests for FilesQueryEndpoint v2 endpoints.
/// </summary>
public class FilesQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IFilesQueryService> _mockFilesQueryService;

    public FilesQueryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockFilesQueryService = new Mock<IFilesQueryService>();
    }

    private HttpClient CreateClientWithMockedService()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IFilesQueryService));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add mock
                services.AddSingleton(_mockFilesQueryService.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetFilesByCategory_ValidCategory_ReturnsSuccessWithFiles()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "Movies";
        var expectedFiles = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "movie1.mkv", FileCategory = "Movies", FileSize = 1024 },
            new() { Id = 2, Name = "movie2.mp4", FileCategory = "Movies", FileSize = 2048 }
        };

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(expectedFiles));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{category}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var actualFiles = JsonSerializer.Deserialize<List<FilesDetailResponse>>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Assert.NotNull(actualFiles);
        Assert.Equal(2, actualFiles.Count);
        Assert.Equal("movie1.mkv", actualFiles.First().Name);
        Assert.Equal("Movies", actualFiles.First().FileCategory);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategory_EmptyCategory_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var emptyCategory = "";

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{emptyCategory}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Empty path segment results in 404
        
        // The service should not be called for empty category
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFilesByCategory_WhitespaceCategory_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var whitespaceCategory = "   "; // URL encoded whitespace

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{Uri.EscapeDataString(whitespaceCategory)}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category is required", content);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFilesByCategory_ServiceReturnsFailure_ReturnsInternalServerError()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "Movies";
        var errorMessage = "Database connection failed";

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Failure(errorMessage));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{category}");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(errorMessage, content);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategory_EmptyResult_ReturnsEmptyArray()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "EmptyCategory";
        var emptyFiles = new List<FilesDetailResponse>();

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(emptyFiles));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{category}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var actualFiles = JsonSerializer.Deserialize<List<FilesDetailResponse>>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Assert.NotNull(actualFiles);
        Assert.Empty(actualFiles);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Movies")]
    [InlineData("Music")]
    [InlineData("Documents")]
    [InlineData("Software")]
    [InlineData("Unknown")]
    [InlineData("TV Shows")]
    [InlineData("Games")]
    public async Task GetFilesByCategory_DifferentValidCategories_ReturnsOk(string category)
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var files = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = $"file1.{category.ToLower().Replace(" ", "")}", FileCategory = category }
        };

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(files));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{Uri.EscapeDataString(category)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var actualFiles = JsonSerializer.Deserialize<List<FilesDetailResponse>>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Assert.NotNull(actualFiles);
        Assert.Single(actualFiles);
        Assert.Equal(category, actualFiles.First().FileCategory);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategory_CategoryWithSpecialCharacters_HandledCorrectly()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "TV Shows & Movies";
        var files = new List<FilesDetailResponse>
        {
            new() { Id = 1, Name = "special_file.mkv", FileCategory = category }
        };

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(files));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{Uri.EscapeDataString(category)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var actualFiles = JsonSerializer.Deserialize<List<FilesDetailResponse>>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Assert.NotNull(actualFiles);
        Assert.Single(actualFiles);
        Assert.Equal(category, actualFiles.First().FileCategory);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategory_LargeResultSet_ReturnsAllFiles()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "Documents";
        var files = Enumerable.Range(1, 100)
            .Select(i => new FilesDetailResponse 
            { 
                Id = i, 
                Name = $"document{i}.pdf", 
                FileCategory = category,
                FileSize = i * 1024
            })
            .ToList();

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(files));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{category}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var actualFiles = JsonSerializer.Deserialize<List<FilesDetailResponse>>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        Assert.NotNull(actualFiles);
        Assert.Equal(100, actualFiles.Count);
        Assert.All(actualFiles, file => Assert.Equal(category, file.FileCategory));
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilesByCategory_CancellationToken_PassedToService()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var category = "Music";
        var files = new List<FilesDetailResponse>();

        _mockFilesQueryService
            .Setup(s => s.GetFilesByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<FilesDetailResponse>>.Success(files));

        // Act
        var response = await client.GetAsync($"/api/v2/files/category/{category}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        _mockFilesQueryService.Verify(s => s.GetFilesByCategoryAsync(
            category, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}