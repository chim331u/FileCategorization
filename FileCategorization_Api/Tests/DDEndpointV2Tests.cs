using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Moq;
using FileCategorization_Api.Interfaces;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.DD;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FileCategorization_Api.Tests;

public class DDEndpointV2Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IDDQueryService> _mockDDService;

    public DDEndpointV2Tests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockDDService = new Mock<IDDQueryService>();
    }

    private HttpClient CreateClientWithMockedService()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDDQueryService));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add mock
                services.AddSingleton(_mockDDService.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task ProcessThread_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var request = new ProcessThreadRequestDto
        {
            ThreadUrl = "https://example.com/thread/123"
        };

        var expectedResult = new ThreadProcessingResultDto
        {
            ThreadId = 1,
            Title = "Test Thread",
            Url = request.ThreadUrl,
            IsNewThread = true,
            NewLinksCount = 5,
            UpdatedLinksCount = 0,
            TotalLinksCount = 5,
            ProcessedAt = DateTime.Now
        };

        _mockDDService
            .Setup(x => x.ProcessThreadAsync(request.ThreadUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ThreadProcessingResultDto>.Success(expectedResult));

        // Act
        var response = await client.PostAsJsonAsync("/api/v2/dd/threads/process", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ThreadProcessingResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(expectedResult.ThreadId, result.ThreadId);
        Assert.Equal(expectedResult.Title, result.Title);
        Assert.Equal(expectedResult.NewLinksCount, result.NewLinksCount);
        Assert.True(result.IsNewThread);

        _mockDDService.Verify(x => x.ProcessThreadAsync(request.ThreadUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessThread_InvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var request = new ProcessThreadRequestDto
        {
            ThreadUrl = "invalid-url"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2/dd/threads/process", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessThread_ServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var request = new ProcessThreadRequestDto
        {
            ThreadUrl = "https://example.com/thread/123"
        };

        _mockDDService
            .Setup(x => x.ProcessThreadAsync(request.ThreadUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ThreadProcessingResultDto>.Failure("Failed to process thread"));

        // Act
        var response = await client.PostAsJsonAsync("/api/v2/dd/threads/process", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshThreadLinks_ValidThreadId_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 1;

        var expectedResult = new ThreadProcessingResultDto
        {
            ThreadId = threadId,
            Title = "Refreshed Thread",
            Url = "https://example.com/thread/1",
            IsNewThread = false,
            NewLinksCount = 3,
            UpdatedLinksCount = 2,
            TotalLinksCount = 10,
            ProcessedAt = DateTime.Now
        };

        _mockDDService
            .Setup(x => x.RefreshThreadLinksAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ThreadProcessingResultDto>.Success(expectedResult));

        // Act
        var response = await client.PostAsync($"/api/v2/dd/threads/{threadId}/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ThreadProcessingResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(expectedResult.ThreadId, result.ThreadId);
        Assert.Equal(expectedResult.NewLinksCount, result.NewLinksCount);
        Assert.False(result.IsNewThread);
    }

    [Fact]
    public async Task RefreshThreadLinks_ThreadNotFound_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 999;

        _mockDDService
            .Setup(x => x.RefreshThreadLinksAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ThreadProcessingResultDto>.Failure("Thread not found"));

        // Act
        var response = await client.PostAsync($"/api/v2/dd/threads/{threadId}/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveThreads_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        var expectedThreads = new List<ThreadSummaryDto>
        {
            new()
            {
                Id = 1,
                MainTitle = "Thread 1",
                LinksCount = 10,
                NewLinksCount = 2,
                HasNewLinks = true,
                CreatedDate = DateTime.Now.AddDays(-1)
            },
            new()
            {
                Id = 2,
                MainTitle = "Thread 2", 
                LinksCount = 5,
                NewLinksCount = 0,
                HasNewLinks = false,
                CreatedDate = DateTime.Now.AddDays(-2)
            }
        };

        _mockDDService
            .Setup(x => x.GetActiveThreadsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<ThreadSummaryDto>>.Success(expectedThreads));

        // Act
        var response = await client.GetAsync("/api/v2/dd/threads");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ThreadSummaryDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Thread 1", result[0].MainTitle);
        Assert.Equal(10, result[0].LinksCount);
    }

    [Fact]
    public async Task GetThreadLinks_ValidThreadId_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 1;
        var expectedLinks = new List<LinkDto>
        {
            new()
            {
                Id = 1,
                Title = "File1.txt",
                Ed2kLink = "ed2k://file1",
                IsNew = true,
                IsUsed = false,
                ThreadId = threadId
            },
            new()
            {
                Id = 2,
                Title = "File2.txt",
                Ed2kLink = "ed2k://file2",
                IsNew = false,
                IsUsed = true,
                ThreadId = threadId
            }
        };

        _mockDDService
            .Setup(x => x.GetThreadLinksAsync(threadId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<LinkDto>>.Success(expectedLinks));

        // Act
        var response = await client.GetAsync($"/api/v2/dd/threads/{threadId}/links");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<LinkDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("File1.txt", result[0].Title);
        Assert.True(result[0].IsNew);
    }

    [Fact]
    public async Task GetThreadLinks_ExcludeUsed_FiltersCorrectly()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 1;
        var expectedLinks = new List<LinkDto>
        {
            new()
            {
                Id = 1,
                Title = "File1.txt",
                Ed2kLink = "ed2k://file1",
                IsNew = true,
                IsUsed = false,
                ThreadId = threadId
            }
        };

        _mockDDService
            .Setup(x => x.GetThreadLinksAsync(threadId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<LinkDto>>.Success(expectedLinks));

        // Act
        var response = await client.GetAsync($"/api/v2/dd/threads/{threadId}/links?includeUsed=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<LinkDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.False(result[0].IsUsed);
    }

    [Fact]
    public async Task UseLink_ValidLinkId_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int linkId = 1;
        var expectedResult = new LinkUsageResultDto
        {
            LinkId = linkId,
            Title = "UsedFile.txt",
            Ed2kLink = "ed2k://usedfile",
            ThreadId = 1,
            UsedAt = DateTime.Now
        };

        _mockDDService
            .Setup(x => x.UseLink(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LinkUsageResultDto>.Success(expectedResult));

        // Act
        var response = await client.PostAsync($"/api/v2/dd/links/{linkId}/use", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LinkUsageResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(expectedResult.LinkId, result.LinkId);
        Assert.Equal(expectedResult.Title, result.Title);
        Assert.Equal(expectedResult.Ed2kLink, result.Ed2kLink);
    }

    [Fact]
    public async Task UseLink_LinkNotFound_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int linkId = 999;

        _mockDDService
            .Setup(x => x.UseLink(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LinkUsageResultDto>.Failure("Link not found"));

        // Act
        var response = await client.PostAsync($"/api/v2/dd/links/{linkId}/use", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateThread_ValidThreadId_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 1;

        _mockDDService
            .Setup(x => x.DeactivateThreadAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var response = await client.DeleteAsync($"/api/v2/dd/threads/{threadId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<bool>(content);

        Assert.True(result);
        _mockDDService.Verify(x => x.DeactivateThreadAsync(threadId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateThread_ThreadNotFound_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClientWithMockedService();
        const int threadId = 999;

        _mockDDService
            .Setup(x => x.DeactivateThreadAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Thread not found"));

        // Act
        var response = await client.DeleteAsync($"/api/v2/dd/threads/{threadId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}