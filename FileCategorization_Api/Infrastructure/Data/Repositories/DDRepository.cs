using Microsoft.EntityFrameworkCore;
using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.DD;
using FileCategorization_Api.Domain.Entities.DD_Web;
using FileCategorization_Api.Infrastructure.Data;

namespace FileCategorization_Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for DD operations with optimized batch operations and proper error handling
/// </summary>
public class DDRepository : IDDRepository
{
    private readonly ApplicationContext _context;
    private readonly ILogger<DDRepository> _logger;

    public DDRepository(ApplicationContext context, ILogger<DDRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Thread Operations

    public async Task<Result<DD_Threads?>> GetThreadByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return Result<DD_Threads?>.Failure("URL cannot be null or empty");

            var thread = await _context.DDThreads
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Url == url, cancellationToken);

            return Result<DD_Threads?>.Success(thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thread by URL: {Url}", url);
            return Result<DD_Threads?>.Failure($"Failed to retrieve thread: {ex.Message}");
        }
    }

    public async Task<Result<DD_Threads?>> GetThreadByIdAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (threadId <= 0)
                return Result<DD_Threads?>.Failure("Thread ID must be greater than zero");

            var thread = await _context.DDThreads
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == threadId && x.IsActive, cancellationToken);

            return Result<DD_Threads?>.Success(thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thread by ID: {ThreadId}", threadId);
            return Result<DD_Threads?>.Failure($"Failed to retrieve thread: {ex.Message}");
        }
    }

    public async Task<Result<List<DD_Threads>>> GetActiveThreadsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var threads = await _context.DDThreads
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync(cancellationToken);

            return Result<List<DD_Threads>>.Success(threads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active threads");
            return Result<List<DD_Threads>>.Failure($"Failed to retrieve threads: {ex.Message}");
        }
    }

    public async Task<Result<DD_Threads>> CreateThreadAsync(DD_Threads thread, CancellationToken cancellationToken = default)
    {
        try
        {
            if (thread == null)
                return Result<DD_Threads>.Failure("Thread cannot be null");

            thread.CreatedDate = DateTime.Now;
            thread.IsActive = true;

            var entry = await _context.DDThreads.AddAsync(thread, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new thread with ID: {ThreadId}", entry.Entity.Id);
            return Result<DD_Threads>.Success(entry.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating thread: {Title}", thread?.MainTitle);
            return Result<DD_Threads>.Failure($"Failed to create thread: {ex.Message}");
        }
    }

    public async Task<Result<DD_Threads>> UpdateThreadAsync(DD_Threads thread, CancellationToken cancellationToken = default)
    {
        try
        {
            if (thread == null)
                return Result<DD_Threads>.Failure("Thread cannot be null");

            thread.LastUpdatedDate = DateTime.Now;
            
            var entry = _context.DDThreads.Update(thread);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated thread with ID: {ThreadId}", thread.Id);
            return Result<DD_Threads>.Success(entry.Entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating thread: {ThreadId}", thread?.Id);
            return Result<DD_Threads>.Failure($"Failed to update thread: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeactivateThreadAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (threadId <= 0)
                return Result<bool>.Failure("Thread ID must be greater than zero");

            var thread = await _context.DDThreads
                .FirstOrDefaultAsync(x => x.Id == threadId, cancellationToken);

            if (thread == null)
                return Result<bool>.Failure("Thread not found");

            thread.IsActive = false;
            thread.LastUpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deactivated thread with ID: {ThreadId}", threadId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating thread: {ThreadId}", threadId);
            return Result<bool>.Failure($"Failed to deactivate thread: {ex.Message}");
        }
    }

    #endregion

    #region Link Operations

    public async Task<Result<List<DD_LinkEd2k>>> GetLinksByThreadIdAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (threadId <= 0)
                return Result<List<DD_LinkEd2k>>.Failure("Thread ID must be greater than zero");

            var links = await _context.DDLinkEd2
                .AsNoTracking()
                .Where(x => x.Threads.Id == threadId && x.IsActive)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync(cancellationToken);

            return Result<List<DD_LinkEd2k>>.Success(links);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving links for thread: {ThreadId}", threadId);
            return Result<List<DD_LinkEd2k>>.Failure($"Failed to retrieve links: {ex.Message}");
        }
    }

    public async Task<Result<DD_LinkEd2k?>> GetLinkByIdAsync(int linkId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (linkId <= 0)
                return Result<DD_LinkEd2k?>.Failure("Link ID must be greater than zero");

            var link = await _context.DDLinkEd2
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == linkId && x.IsActive, cancellationToken);

            return Result<DD_LinkEd2k?>.Success(link);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving link by ID: {LinkId}", linkId);
            return Result<DD_LinkEd2k?>.Failure($"Failed to retrieve link: {ex.Message}");
        }
    }

    public async Task<Result<List<DD_LinkEd2k>>> GetExistingLinksAsync(List<string> ed2kLinks, int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (ed2kLinks == null || !ed2kLinks.Any())
                return Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>());

            if (threadId <= 0)
                return Result<List<DD_LinkEd2k>>.Failure("Thread ID must be greater than zero");

            var existingLinks = await _context.DDLinkEd2
                .AsNoTracking()
                .Where(x => ed2kLinks.Contains(x.Ed2kLink) && x.Threads.Id == threadId)
                .ToListAsync(cancellationToken);

            return Result<List<DD_LinkEd2k>>.Success(existingLinks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existing links for thread: {ThreadId}", threadId);
            return Result<List<DD_LinkEd2k>>.Failure($"Failed to check existing links: {ex.Message}");
        }
    }

    public async Task<Result<int>> CreateLinksAsync(List<DD_LinkEd2k> links, CancellationToken cancellationToken = default)
    {
        try
        {
            if (links == null || !links.Any())
                return Result<int>.Success(0);

            foreach (var link in links)
            {
                link.CreatedDate = DateTime.Now;
                link.IsActive = true;
            }

            await _context.DDLinkEd2.AddRangeAsync(links, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created {Count} new links", links.Count);
            return Result<int>.Success(links.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {Count} links", links?.Count ?? 0);
            return Result<int>.Failure($"Failed to create links: {ex.Message}");
        }
    }

    public async Task<Result<int>> UpdateLinksAsync(List<DD_LinkEd2k> links, CancellationToken cancellationToken = default)
    {
        try
        {
            if (links == null || !links.Any())
                return Result<int>.Success(0);

            foreach (var link in links)
            {
                link.LastUpdatedDate = DateTime.Now;
            }

            _context.DDLinkEd2.UpdateRange(links);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated {Count} links", links.Count);
            return Result<int>.Success(links.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {Count} links", links?.Count ?? 0);
            return Result<int>.Failure($"Failed to update links: {ex.Message}");
        }
    }

    public async Task<Result<DD_LinkEd2k>> MarkLinkAsUsedAsync(int linkId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (linkId <= 0)
                return Result<DD_LinkEd2k>.Failure("Link ID must be greater than zero");

            var link = await _context.DDLinkEd2
                .FirstOrDefaultAsync(x => x.Id == linkId && x.IsActive, cancellationToken);

            if (link == null)
                return Result<DD_LinkEd2k>.Failure("Link not found");

            link.IsUsed = true;
            link.IsNew = false;
            link.LastUpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked link as used: {LinkId} - {Title}", linkId, link.Title);
            return Result<DD_LinkEd2k>.Success(link);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking link as used: {LinkId}", linkId);
            return Result<DD_LinkEd2k>.Failure($"Failed to mark link as used: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeactivateLinksAsync(List<int> linkIds, CancellationToken cancellationToken = default)
    {
        try
        {
            if (linkIds == null || !linkIds.Any())
                return Result<bool>.Success(true);

            var links = await _context.DDLinkEd2
                .Where(x => linkIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (var link in links)
            {
                link.IsActive = false;
                link.LastUpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deactivated {Count} links", links.Count);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating {Count} links", linkIds?.Count ?? 0);
            return Result<bool>.Failure($"Failed to deactivate links: {ex.Message}");
        }
    }

    #endregion

    #region Statistics and Reporting

    public async Task<Result<int>> GetNewLinksCountAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (threadId <= 0)
                return Result<int>.Failure("Thread ID must be greater than zero");

            var count = await _context.DDLinkEd2
                .AsNoTracking()
                .CountAsync(x => x.Threads.Id == threadId && x.IsActive && x.IsNew, cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting new links for thread: {ThreadId}", threadId);
            return Result<int>.Failure($"Failed to count new links: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<int, int>>> GetThreadsLinkCountsAsync(List<int> threadIds, CancellationToken cancellationToken = default)
    {
        try
        {
            if (threadIds == null || !threadIds.Any())
                return Result<Dictionary<int, int>>.Success(new Dictionary<int, int>());

            var counts = await _context.DDLinkEd2
                .AsNoTracking()
                .Where(x => threadIds.Contains(x.Threads.Id) && x.IsActive)
                .GroupBy(x => x.Threads.Id)
                .Select(g => new { ThreadId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ThreadId, x => x.Count, cancellationToken);

            // Ensure all requested thread IDs are in the result, even with 0 count
            foreach (var threadId in threadIds.Where(id => !counts.ContainsKey(id)))
            {
                counts[threadId] = 0;
            }

            return Result<Dictionary<int, int>>.Success(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting links for {Count} threads", threadIds?.Count ?? 0);
            return Result<Dictionary<int, int>>.Failure($"Failed to count thread links: {ex.Message}");
        }
    }

    #endregion
}