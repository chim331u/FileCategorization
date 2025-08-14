using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FileCategorization_Api.Services;
using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Contracts.DD;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Tests;

public class DDQueryServiceTests
{
    private readonly Mock<IDDRepository> _mockRepository;
    private readonly Mock<IDDWebScrapingService> _mockWebScrapingService;
    private readonly Mock<ILogger<DDQueryService>> _mockLogger;
    private readonly IMapper _mapper;
    private readonly DDQueryService _service;

    public DDQueryServiceTests()
    {
        _mockRepository = new Mock<IDDRepository>();
        _mockWebScrapingService = new Mock<IDDWebScrapingService>();
        _mockLogger = new Mock<ILogger<DDQueryService>>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DDMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _service = new DDQueryService(
            _mockRepository.Object,
            _mockWebScrapingService.Object,
            _mapper,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessThreadAsync_WithNewThread_CreatesThreadSuccessfully()
    {
        // Arrange
        const string testUrl = "http://example.com/thread/1";
        const string pageContent = "<html>Test content</html>";
        
        var parsedThread = new DD_Threads
        {
            MainTitle = "Test Thread",
            Type = "Movie",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        var createdThread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Test Thread",
            Type = "Movie",
            Url = testUrl,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        var parsedLinks = new List<DD_LinkEd2k>
        {
            new()
            {
                Title = "Movie File",
                Ed2kLink = "ed2k://movie",
                Threads = createdThread,
                IsNew = true,
                CreatedDate = DateTime.Now,
                IsActive = true
            }
        };

        // Setup mocks
        _mockWebScrapingService.Setup(x => x.GetPageContentAsync(testUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(pageContent));

        _mockWebScrapingService.Setup(x => x.ParseThreadInfoAsync(pageContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(parsedThread));

        _mockRepository.Setup(x => x.GetThreadByUrlAsync(testUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads?>.Success((DD_Threads?)null));

        _mockRepository.Setup(x => x.CreateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(createdThread));

        _mockWebScrapingService.Setup(x => x.ParseEd2kLinksAsync(pageContent, It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(parsedLinks));

        _mockRepository.Setup(x => x.GetExistingLinksAsync(It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>()));

        _mockRepository.Setup(x => x.CreateLinksAsync(It.IsAny<List<DD_LinkEd2k>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _mockRepository.Setup(x => x.GetLinksByThreadIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(parsedLinks));

        // Act
        var result = await _service.ProcessThreadAsync(testUrl);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.ThreadId);
        Assert.Equal("Test Thread", result.Data.Title);
        Assert.Equal(testUrl, result.Data.Url);
        Assert.True(result.Data.IsNewThread);
        Assert.Equal(1, result.Data.NewLinksCount);
        Assert.Equal(1, result.Data.TotalLinksCount);

        // Verify repository calls
        _mockRepository.Verify(x => x.CreateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.CreateLinksAsync(It.IsAny<List<DD_LinkEd2k>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessThreadAsync_WithExistingThread_UpdatesThreadSuccessfully()
    {
        // Arrange
        const string testUrl = "http://example.com/thread/1";
        const string pageContent = "<html>Updated content</html>";
        
        var existingThread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Old Title",
            Type = "Movie",
            Url = testUrl,
            CreatedDate = DateTime.Now.AddDays(-1),
            IsActive = true
        };

        var parsedThread = new DD_Threads
        {
            MainTitle = "Updated Title",
            Type = "Movie",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        var updatedThread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Updated Title",
            Type = "Movie",
            Url = testUrl,
            CreatedDate = DateTime.Now.AddDays(-1),
            LastUpdatedDate = DateTime.Now,
            IsActive = true
        };

        // Setup mocks
        _mockWebScrapingService.Setup(x => x.GetPageContentAsync(testUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(pageContent));

        _mockWebScrapingService.Setup(x => x.ParseThreadInfoAsync(pageContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(parsedThread));

        _mockRepository.Setup(x => x.GetThreadByUrlAsync(testUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads?>.Success(existingThread));

        _mockRepository.Setup(x => x.UpdateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(updatedThread));

        _mockWebScrapingService.Setup(x => x.ParseEd2kLinksAsync(pageContent, It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>()));

        _mockRepository.Setup(x => x.GetLinksByThreadIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>()));

        // Act
        var result = await _service.ProcessThreadAsync(testUrl);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.ThreadId);
        Assert.Equal("Updated Title", result.Data.Title);
        Assert.False(result.Data.IsNewThread);

        // Verify repository calls
        _mockRepository.Verify(x => x.UpdateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.CreateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessThreadAsync_WebScrapingFails_ReturnsFailure()
    {
        // Arrange
        const string testUrl = "http://example.com/thread/1";

        _mockWebScrapingService.Setup(x => x.GetPageContentAsync(testUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Failed to fetch page"));

        // Act
        var result = await _service.ProcessThreadAsync(testUrl);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to fetch page content", result.ErrorMessage);
    }

    [Fact]
    public async Task GetActiveThreadsAsync_WithThreads_ReturnsThreadsWithStats()
    {
        // Arrange
        var threads = new List<DD_Threads>
        {
            new()
            {
                Id = 1,
                MainTitle = "Thread 1",
                Type = "Movie",
                Url = "http://example.com/thread1",
                CreatedDate = DateTime.Now.AddDays(-2),
                IsActive = true
            },
            new()
            {
                Id = 2,
                MainTitle = "Thread 2",
                Type = "TV",
                Url = "http://example.com/thread2",
                CreatedDate = DateTime.Now.AddDays(-1),
                IsActive = true
            }
        };

        var linkCounts = new Dictionary<int, int>
        {
            { 1, 5 },
            { 2, 3 }
        };

        // Setup mocks
        _mockRepository.Setup(x => x.GetActiveThreadsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_Threads>>.Success(threads));

        _mockRepository.Setup(x => x.GetThreadsLinkCountsAsync(It.Is<List<int>>(ids => ids.Contains(1) && ids.Contains(2)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Dictionary<int, int>>.Success(linkCounts));

        _mockRepository.Setup(x => x.GetNewLinksCountAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(2));

        _mockRepository.Setup(x => x.GetNewLinksCountAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        // Act
        var result = await _service.GetActiveThreadsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        
        var thread1 = result.Data.First(t => t.Id == 1);
        Assert.Equal("Thread 1", thread1.MainTitle);
        Assert.Equal(5, thread1.LinksCount);
        Assert.Equal(2, thread1.NewLinksCount);
        Assert.Equal(3, thread1.UsedLinksCount);
        Assert.True(thread1.HasNewLinks);

        var thread2 = result.Data.First(t => t.Id == 2);
        Assert.Equal("Thread 2", thread2.MainTitle);
        Assert.Equal(3, thread2.LinksCount);
        Assert.Equal(1, thread2.NewLinksCount);
        Assert.Equal(2, thread2.UsedLinksCount);
        Assert.True(thread2.HasNewLinks);
    }

    [Fact]
    public async Task UseLink_WithValidLink_MarksLinkAsUsedSuccessfully()
    {
        // Arrange
        const int linkId = 1;
        var thread = new DD_Threads
        {
            Id = 1,
            MainTitle = "Test Thread",
            Url = "http://example.com/thread",
            IsActive = true
        };

        var usedLink = new DD_LinkEd2k
        {
            Id = linkId,
            Title = "Test File",
            Ed2kLink = "ed2k://testfile",
            Threads = thread,
            IsUsed = true,
            IsNew = false,
            LastUpdatedDate = DateTime.Now
        };

        // Setup mock
        _mockRepository.Setup(x => x.MarkLinkAsUsedAsync(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_LinkEd2k>.Success(usedLink));

        // Act
        var result = await _service.UseLink(linkId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(linkId, result.Data.LinkId);
        Assert.Equal("Test File", result.Data.Title);
        Assert.Equal("ed2k://testfile", result.Data.Ed2kLink);
        Assert.Equal(1, result.Data.ThreadId);
    }

    [Fact]
    public async Task RefreshThreadLinksAsync_WithValidThreadId_RefreshesSuccessfully()
    {
        // Arrange
        const int threadId = 1;
        const string threadUrl = "http://example.com/thread/1";
        
        var existingThread = new DD_Threads
        {
            Id = threadId,
            MainTitle = "Existing Thread",
            Url = threadUrl,
            IsActive = true,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Setup mocks
        _mockRepository.Setup(x => x.GetThreadByIdAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads?>.Success(existingThread));

        // Setup the full processing chain
        _mockWebScrapingService.Setup(x => x.GetPageContentAsync(threadUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("<html>Content</html>"));

        _mockWebScrapingService.Setup(x => x.ParseThreadInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(new DD_Threads { MainTitle = "Updated Thread" }));

        _mockRepository.Setup(x => x.GetThreadByUrlAsync(threadUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads?>.Success(existingThread));

        _mockRepository.Setup(x => x.UpdateThreadAsync(It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads>.Success(existingThread));

        _mockWebScrapingService.Setup(x => x.ParseEd2kLinksAsync(It.IsAny<string>(), It.IsAny<DD_Threads>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>()));

        _mockRepository.Setup(x => x.GetLinksByThreadIdAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>()));

        // Act
        var result = await _service.RefreshThreadLinksAsync(threadId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(threadId, result.Data.ThreadId);
    }

    [Fact]
    public async Task RefreshThreadLinksAsync_WithInvalidThreadId_ReturnsFailure()
    {
        // Arrange
        const int threadId = 999;

        // Setup mock to return null (thread not found)
        _mockRepository.Setup(x => x.GetThreadByIdAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DD_Threads?>.Success((DD_Threads?)null));

        // Act
        var result = await _service.RefreshThreadLinksAsync(threadId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task GetThreadLinksAsync_WithIncludeUsedFalse_FiltersOutUsedLinks()
    {
        // Arrange
        const int threadId = 1;
        var thread = new DD_Threads { Id = threadId };
        
        var allLinks = new List<DD_LinkEd2k>
        {
            new()
            {
                Id = 1,
                Title = "New File",
                Ed2kLink = "ed2k://newfile",
                Threads = thread,
                IsUsed = false,
                IsNew = true
            },
            new()
            {
                Id = 2,
                Title = "Used File",
                Ed2kLink = "ed2k://usedfile", 
                Threads = thread,
                IsUsed = true,
                IsNew = false
            }
        };

        // Setup mock
        _mockRepository.Setup(x => x.GetLinksByThreadIdAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DD_LinkEd2k>>.Success(allLinks));

        // Act
        var result = await _service.GetThreadLinksAsync(threadId, includeUsed: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("New File", result.Data.First().Title);
        Assert.False(result.Data.First().IsUsed);
    }

    [Fact]
    public async Task DeactivateThreadAsync_WithValidThreadId_DeactivatesSuccessfully()
    {
        // Arrange
        const int threadId = 1;

        // Setup mock
        _mockRepository.Setup(x => x.DeactivateThreadAsync(threadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _service.DeactivateThreadAsync(threadId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        // Verify repository call
        _mockRepository.Verify(x => x.DeactivateThreadAsync(threadId, It.IsAny<CancellationToken>()), Times.Once);
    }
}