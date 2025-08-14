using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Infrastructure.Data.Repositories;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Tests;

public class DDRepositoryTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly Mock<ILogger<DDRepository>> _mockLogger;
    private readonly DDRepository _repository;

    public DDRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationContext(options);
        _mockLogger = new Mock<ILogger<DDRepository>>();
        _repository = new DDRepository(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var threads = new List<DD_Threads>
        {
            new()
            {
                Id = 1,
                MainTitle = "Test Thread 1",
                Type = "Movie",
                Url = "https://example.com/thread/1",
                CreatedDate = DateTime.Now.AddDays(-2),
                IsActive = true
            },
            new()
            {
                Id = 2,
                MainTitle = "Test Thread 2", 
                Type = "TV",
                Url = "https://example.com/thread/2",
                CreatedDate = DateTime.Now.AddDays(-1),
                IsActive = true
            },
            new()
            {
                Id = 3,
                MainTitle = "Inactive Thread",
                Type = "Movie",
                Url = "https://example.com/thread/3",
                CreatedDate = DateTime.Now.AddDays(-3),
                IsActive = false
            }
        };

        var links = new List<DD_LinkEd2k>
        {
            new()
            {
                Id = 1,
                Title = "Movie File 1",
                Ed2kLink = "ed2k://|file|movie1.avi|123|ABC|/",
                Threads = threads[0],
                IsNew = true,
                IsUsed = false,
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-1)
            },
            new()
            {
                Id = 2,
                Title = "Movie File 2",
                Ed2kLink = "ed2k://|file|movie2.mkv|456|DEF|/",
                Threads = threads[0],
                IsNew = false,
                IsUsed = true,
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-1)
            },
            new()
            {
                Id = 3,
                Title = "TV File 1",
                Ed2kLink = "ed2k://|file|episode1.mp4|789|GHI|/",
                Threads = threads[1],
                IsNew = true,
                IsUsed = false,
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new()
            {
                Id = 4,
                Title = "Inactive File",
                Ed2kLink = "ed2k://|file|inactive.avi|999|XYZ|/",
                Threads = threads[2],
                IsNew = false,
                IsUsed = false,
                IsActive = false,
                CreatedDate = DateTime.Now.AddDays(-2)
            }
        };

        _context.DDThreads.AddRange(threads);
        _context.DDLinkEd2.AddRange(links);
        _context.SaveChanges();
    }

    #region Thread Operations Tests

    [Fact]
    public async Task GetThreadByUrlAsync_ValidUrl_ReturnsThread()
    {
        // Arrange
        const string url = "https://example.com/thread/1";

        // Act
        var result = await _repository.GetThreadByUrlAsync(url);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal("Test Thread 1", result.Data.MainTitle);
        Assert.Equal(url, result.Data.Url);
    }

    [Fact]
    public async Task GetThreadByUrlAsync_NonExistentUrl_ReturnsNull()
    {
        // Arrange
        const string url = "https://example.com/thread/nonexistent";

        // Act
        var result = await _repository.GetThreadByUrlAsync(url);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetThreadByUrlAsync_InvalidUrl_ReturnsFailure(string? invalidUrl)
    {
        // Act
        var result = await _repository.GetThreadByUrlAsync(invalidUrl!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("URL cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetThreadByUrlAsync_NullUrl_ReturnsFailure()
    {
        // Act
        var result = await _repository.GetThreadByUrlAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("URL cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task GetThreadByIdAsync_ValidId_ReturnsActiveThread()
    {
        // Act
        var result = await _repository.GetThreadByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal("Test Thread 1", result.Data.MainTitle);
    }

    [Fact]
    public async Task GetThreadByIdAsync_InactiveThread_ReturnsNull()
    {
        // Act
        var result = await _repository.GetThreadByIdAsync(3);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetThreadByIdAsync_InvalidId_ReturnsFailure(int invalidId)
    {
        // Act
        var result = await _repository.GetThreadByIdAsync(invalidId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread ID must be greater than zero", result.ErrorMessage);
    }

    [Fact]
    public async Task GetActiveThreadsAsync_ReturnsOnlyActiveThreads()
    {
        // Act
        var result = await _repository.GetActiveThreadsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, thread => Assert.True(thread.IsActive));
        Assert.Contains(result.Data, t => t.Id == 1);
        Assert.Contains(result.Data, t => t.Id == 2);
        Assert.DoesNotContain(result.Data, t => t.Id == 3);
    }

    [Fact]
    public async Task CreateThreadAsync_ValidThread_CreatesSuccessfully()
    {
        // Arrange
        var newThread = new DD_Threads
        {
            MainTitle = "New Test Thread",
            Type = "Documentary",
            Url = "https://example.com/thread/new"
        };

        // Act
        var result = await _repository.CreateThreadAsync(newThread);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Id > 0);
        Assert.Equal("New Test Thread", result.Data.MainTitle);
        Assert.True(result.Data.IsActive);
        Assert.True(result.Data.CreatedDate > DateTime.MinValue);

        // Verify it was actually saved to database
        var savedThread = await _context.DDThreads.FindAsync(result.Data.Id);
        Assert.NotNull(savedThread);
        Assert.Equal(newThread.MainTitle, savedThread.MainTitle);
    }

    [Fact]
    public async Task CreateThreadAsync_NullThread_ReturnsFailure()
    {
        // Act
        var result = await _repository.CreateThreadAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread cannot be null", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateThreadAsync_ValidThread_UpdatesSuccessfully()
    {
        // Arrange
        var thread = await _context.DDThreads.FindAsync(1);
        thread.MainTitle = "Updated Title";
        thread.Type = "Updated Type";

        // Act
        var result = await _repository.UpdateThreadAsync(thread);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Title", result.Data.MainTitle);
        Assert.Equal("Updated Type", result.Data.Type);
        Assert.True(result.Data.LastUpdatedDate > DateTime.MinValue);

        // Verify it was actually updated in database
        var updatedThread = await _context.DDThreads.FindAsync(1);
        Assert.Equal("Updated Title", updatedThread.MainTitle);
        Assert.Equal("Updated Type", updatedThread.Type);
        Assert.True(updatedThread.LastUpdatedDate > DateTime.MinValue);
    }

    [Fact]
    public async Task UpdateThreadAsync_NullThread_ReturnsFailure()
    {
        // Act
        var result = await _repository.UpdateThreadAsync(null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread cannot be null", result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateThreadAsync_ValidId_DeactivatesSuccessfully()
    {
        // Act
        var result = await _repository.DeactivateThreadAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        // Verify thread was deactivated
        var thread = await _context.DDThreads.FindAsync(1);
        Assert.False(thread.IsActive);
        Assert.True(thread.LastUpdatedDate > DateTime.MinValue);
    }

    [Fact]
    public async Task DeactivateThreadAsync_NonExistentId_ReturnsFailure()
    {
        // Act
        var result = await _repository.DeactivateThreadAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread not found", result.ErrorMessage);
    }

    #endregion

    #region Link Operations Tests

    [Fact]
    public async Task GetLinksByThreadIdAsync_ValidThreadId_ReturnsActiveLinks()
    {
        // Act
        var result = await _repository.GetLinksByThreadIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, link => Assert.True(link.IsActive));
        Assert.All(result.Data, link => Assert.Equal(1, link.Threads.Id));
    }

    [Fact]
    public async Task GetLinksByThreadIdAsync_ThreadWithNoLinks_ReturnsEmptyList()
    {
        // Arrange - Create a thread with no links
        var newThread = new DD_Threads
        {
            MainTitle = "Thread with no links",
            Type = "Test",
            Url = "https://example.com/empty",
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context.DDThreads.Add(newThread);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLinksByThreadIdAsync(newThread.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLinksByThreadIdAsync_InvalidThreadId_ReturnsFailure(int invalidId)
    {
        // Act
        var result = await _repository.GetLinksByThreadIdAsync(invalidId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Thread ID must be greater than zero", result.ErrorMessage);
    }

    [Fact]
    public async Task GetLinkByIdAsync_ValidId_ReturnsLink()
    {
        // Act
        var result = await _repository.GetLinkByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal("Movie File 1", result.Data.Title);
    }

    [Fact]
    public async Task GetLinkByIdAsync_InactiveLink_ReturnsNull()
    {
        // Act
        var result = await _repository.GetLinkByIdAsync(4);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetExistingLinksAsync_WithExistingLinks_ReturnsMatchingLinks()
    {
        // Arrange
        var ed2kLinks = new List<string>
        {
            "ed2k://|file|movie1.avi|123|ABC|/",
            "ed2k://|file|nonexistent.avi|999|ZZZ|/"
        };

        // Act
        var result = await _repository.GetExistingLinksAsync(ed2kLinks, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("ed2k://|file|movie1.avi|123|ABC|/", result.Data[0].Ed2kLink);
    }

    [Fact]
    public async Task GetExistingLinksAsync_EmptyList_ReturnsEmptyResult()
    {
        // Act
        var result = await _repository.GetExistingLinksAsync(new List<string>(), 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task CreateLinksAsync_ValidLinks_CreatesSuccessfully()
    {
        // Arrange
        var thread = await _context.DDThreads.FindAsync(1);
        var newLinks = new List<DD_LinkEd2k>
        {
            new()
            {
                Title = "New File 1",
                Ed2kLink = "ed2k://|file|newfile1.avi|111|AAA|/",
                Threads = thread,
                IsNew = true,
                IsUsed = false
            },
            new()
            {
                Title = "New File 2",
                Ed2kLink = "ed2k://|file|newfile2.mkv|222|BBB|/",
                Threads = thread,
                IsNew = true,
                IsUsed = false
            }
        };

        // Act
        var result = await _repository.CreateLinksAsync(newLinks);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data);

        // Verify links were created
        var createdLinks = await _context.DDLinkEd2
            .Where(x => x.Ed2kLink.Contains("newfile"))
            .ToListAsync();
        Assert.Equal(2, createdLinks.Count);
        Assert.All(createdLinks, link => Assert.True(link.IsActive));
    }

    [Fact]
    public async Task CreateLinksAsync_EmptyList_ReturnsZero()
    {
        // Act
        var result = await _repository.CreateLinksAsync(new List<DD_LinkEd2k>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data);
    }

    [Fact]
    public async Task UpdateLinksAsync_ValidLinks_UpdatesSuccessfully()
    {
        // Arrange
        var links = await _context.DDLinkEd2
            .Where(x => x.Threads.Id == 1)
            .ToListAsync();
        
        foreach (var link in links)
        {
            link.Title = "Updated " + link.Title;
        }

        // Act
        var result = await _repository.UpdateLinksAsync(links);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(links.Count, result.Data);

        // Verify updates
        var updatedLinks = await _context.DDLinkEd2
            .Where(x => x.Threads.Id == 1)
            .ToListAsync();
        Assert.All(updatedLinks, link => Assert.StartsWith("Updated", link.Title));
        Assert.All(updatedLinks, link => Assert.True(link.LastUpdatedDate > DateTime.MinValue));
    }

    [Fact]
    public async Task MarkLinkAsUsedAsync_ValidLinkId_MarksAsUsed()
    {
        // Act
        var result = await _repository.MarkLinkAsUsedAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsUsed);
        Assert.False(result.Data.IsNew);
        Assert.True(result.Data.LastUpdatedDate > DateTime.MinValue);

        // Verify in database
        var link = await _context.DDLinkEd2.FindAsync(1);
        Assert.True(link.IsUsed);
        Assert.False(link.IsNew);
    }

    [Fact]
    public async Task MarkLinkAsUsedAsync_NonExistentLink_ReturnsFailure()
    {
        // Act
        var result = await _repository.MarkLinkAsUsedAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Link not found", result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateLinksAsync_ValidLinkIds_DeactivatesSuccessfully()
    {
        // Arrange
        var linkIds = new List<int> { 1, 2 };

        // Act
        var result = await _repository.DeactivateLinksAsync(linkIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        // Verify deactivation
        var links = await _context.DDLinkEd2
            .Where(x => linkIds.Contains(x.Id))
            .ToListAsync();
        Assert.All(links, link => Assert.False(link.IsActive));
        Assert.All(links, link => Assert.True(link.LastUpdatedDate > DateTime.MinValue));
    }

    [Fact]
    public async Task DeactivateLinksAsync_EmptyList_ReturnsSuccess()
    {
        // Act
        var result = await _repository.DeactivateLinksAsync(new List<int>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetNewLinksCountAsync_ValidThreadId_ReturnsCorrectCount()
    {
        // Act
        var result = await _repository.GetNewLinksCountAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data); // Only one new link for thread 1
    }

    [Fact]
    public async Task GetNewLinksCountAsync_ThreadWithNoNewLinks_ReturnsZero()
    {
        // Arrange - Mark all links as not new
        var links = await _context.DDLinkEd2.Where(x => x.Threads.Id == 1).ToListAsync();
        foreach (var link in links)
        {
            link.IsNew = false;
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetNewLinksCountAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data);
    }

    [Fact]
    public async Task GetThreadsLinkCountsAsync_ValidThreadIds_ReturnsCorrectCounts()
    {
        // Arrange
        var threadIds = new List<int> { 1, 2 };

        // Act
        var result = await _repository.GetThreadsLinkCountsAsync(threadIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Data[1]); // Thread 1 has 2 active links
        Assert.Equal(1, result.Data[2]); // Thread 2 has 1 active link
    }

    [Fact]
    public async Task GetThreadsLinkCountsAsync_ThreadWithNoLinks_IncludesZeroCount()
    {
        // Arrange - Create thread with no links
        var newThread = new DD_Threads
        {
            MainTitle = "Empty Thread",
            Type = "Test",
            Url = "https://example.com/empty2",
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context.DDThreads.Add(newThread);
        await _context.SaveChangesAsync();

        var threadIds = new List<int> { 1, newThread.Id };

        // Act
        var result = await _repository.GetThreadsLinkCountsAsync(threadIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Data[1]);
        Assert.Equal(0, result.Data[newThread.Id]);
    }

    [Fact]
    public async Task GetThreadsLinkCountsAsync_EmptyList_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _repository.GetThreadsLinkCountsAsync(new List<int>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}