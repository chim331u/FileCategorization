using System.Net;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using FileCategorization_Api.Services;
using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Tests;

public class DDWebScrapingServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<DDWebScrapingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly HttpClient _httpClient;
    private readonly DDWebScrapingService _service;

    public DDWebScrapingServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<DDWebScrapingService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        // Setup environment and configuration
        _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
        _mockConfiguration.Setup(x => x["DD_USERNAME"]).Returns("testuser");
        _mockConfiguration.Setup(x => x["DD_PSW"]).Returns("testpass");

        _service = new DDWebScrapingService(_mockLogger.Object, _mockConfiguration.Object, _mockEnvironment.Object, _httpClient);
    }

    [Fact]
    public async Task GetPageContentAsync_ValidUrl_ReturnsContent()
    {
        // Arrange
        const string url = "https://example.com/thread/123";
        const string expectedContent = "<html><body>Test content</body></html>";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedContent)
            });

        // Act
        var result = await _service.GetPageContentAsync(url);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedContent, result.Data);
    }

    [Fact]
    public async Task GetPageContentAsync_HttpError_ReturnsFailure()
    {
        // Arrange
        const string url = "https://example.com/thread/404";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                ReasonPhrase = "Not Found"
            });

        // Act
        var result = await _service.GetPageContentAsync(url);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to fetch page", result.ErrorMessage);
        Assert.Contains("NotFound", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPageContentAsync_NetworkException_ReturnsFailure()
    {
        // Arrange
        const string url = "https://example.com/thread/timeout";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetPageContentAsync(url);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Network error occurred", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseThreadInfoAsync_ValidHtml_ParsesThreadInfo()
    {
        // Arrange
        const string html = @"
            <html>
                <head><title>Test Thread - Movie</title></head>
                <body>
                    <h3 class='first'>
                        <a>Test Movie Thread</a>
                    </h3>
                </body>
            </html>";

        // Act
        var result = await _service.ParseThreadInfoAsync(html);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Movie Thread", result.Data.MainTitle);
        Assert.True(result.Data.IsActive);
        Assert.True(result.Data.CreatedDate > DateTime.MinValue);
    }

    [Fact]
    public async Task ParseThreadInfoAsync_EmptyHtml_ReturnsFailure()
    {
        // Arrange
        const string html = "<html></html>";

        // Act
        var result = await _service.ParseThreadInfoAsync(html);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unable to find thread title", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseThreadInfoAsync_InvalidHtml_ReturnsFailure()
    {
        // Arrange
        const string html = "<invalid-html><unclosed-tag";

        // Act
        var result = await _service.ParseThreadInfoAsync(html);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unable to find thread title", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseEd2kLinksAsync_ValidHtml_ParsesLinks()
    {
        // Arrange
        var thread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Test Thread",
            IsActive = true
        };

        const string html = @"
            <html>
                <body>
                    Some text with links:
                    ed2k://|file|movie1.avi|123456789|ABCDEF123456789|/
                    ed2k://|file|movie2.mkv|987654321|123456789ABCDEF|/
                    http://regular-link.com
                </body>
            </html>";

        // Act
        var result = await _service.ParseEd2kLinksAsync(html, thread);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        var firstLink = result.Data.First();
        Assert.Equal("movie1.avi", firstLink.Title);
        Assert.Contains("ed2k://", firstLink.Ed2kLink);
        Assert.Equal(thread, firstLink.Threads);
        Assert.False(firstLink.IsUsed);
        Assert.True(firstLink.IsActive);
    }

    [Fact]
    public async Task ParseEd2kLinksAsync_NoLinks_ReturnsEmptyList()
    {
        // Arrange
        var thread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Test Thread",
            IsActive = true
        };

        const string html = @"
            <html>
                <body>
                    <div class='content'>
                        <p>No links available</p>
                    </div>
                </body>
            </html>";

        // Act
        var result = await _service.ParseEd2kLinksAsync(html, thread);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task ParseEd2kLinksAsync_MixedLinks_FiltersOnlyEd2k()
    {
        // Arrange
        var thread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Test Thread",
            IsActive = true
        };

        const string html = @"
            <html>
                <body>
                    ed2k://|file|valid.avi|123|ABC|/
                    http://example.com
                    magnet:?xt=urn:btih:123
                    ed2k://|file|another.mkv|456|DEF|/
                </body>
            </html>";

        // Act
        var result = await _service.ParseEd2kLinksAsync(html, thread);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, link => Assert.StartsWith("ed2k://", link.Ed2kLink));
    }

    // LoginAsync is private, no need to test it directly

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPageContentAsync_InvalidUrl_ReturnsFailure(string? invalidUrl)
    {
        // Act
        var result = await _service.GetPageContentAsync(invalidUrl!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread URL cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPageContentAsync_NullUrl_ReturnsFailure()
    {
        // Act
        var result = await _service.GetPageContentAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread URL cannot be null or empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseThreadInfoAsync_NullOrEmptyHtml_ReturnsFailure(string? invalidHtml)
    {
        // Act
        var result = await _service.ParseThreadInfoAsync(invalidHtml!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("HTML content cannot be null or empty", result.ErrorMessage);
    }

    [Fact] 
    public async Task ParseThreadInfoAsync_NullHtml_ReturnsFailure()
    {
        // Act
        var result = await _service.ParseThreadInfoAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("HTML content cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ParseEd2kLinksAsync_NullThread_ReturnsFailure()
    {
        // Arrange
        const string html = "<html><body></body></html>";

        // Act
        var result = await _service.ParseEd2kLinksAsync(html, null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread cannot be null", result.ErrorMessage);
    }

    // Service doesn't implement IDisposable
}