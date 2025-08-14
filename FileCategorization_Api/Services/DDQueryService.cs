using AutoMapper;
using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Contracts.DD;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Services;

/// <summary>
/// Query service for DD operations with modern architecture patterns
/// </summary>
public class DDQueryService : IDDQueryService
{
    private readonly IDDRepository _repository;
    private readonly IDDWebScrapingService _webScrapingService;
    private readonly IMapper _mapper;
    private readonly ILogger<DDQueryService> _logger;

    public DDQueryService(
        IDDRepository repository,
        IDDWebScrapingService webScrapingService,
        IMapper mapper,
        ILogger<DDQueryService> logger)
    {
        _repository = repository;
        _webScrapingService = webScrapingService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ThreadProcessingResultDto>> ProcessThreadAsync(string threadUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing thread: {Url}", threadUrl);

            // Get page content
            var pageContentResult = await _webScrapingService.GetPageContentAsync(threadUrl, cancellationToken);
            if (!pageContentResult.IsSuccess)
                return Result<ThreadProcessingResultDto>.Failure($"Failed to fetch page content: {pageContentResult.ErrorMessage}");

            // Parse thread info
            var threadInfoResult = await _webScrapingService.ParseThreadInfoAsync(pageContentResult.Data, cancellationToken);
            if (!threadInfoResult.IsSuccess)
                return Result<ThreadProcessingResultDto>.Failure($"Failed to parse thread info: {threadInfoResult.ErrorMessage}");

            var parsedThread = threadInfoResult.Data;
            parsedThread.Url = threadUrl;

            // Check if thread already exists
            var existingThreadResult = await _repository.GetThreadByUrlAsync(threadUrl, cancellationToken);
            if (!existingThreadResult.IsSuccess)
                return Result<ThreadProcessingResultDto>.Failure($"Failed to check existing thread: {existingThreadResult.ErrorMessage}");

            DD_Threads thread;
            bool isNewThread;

            if (existingThreadResult.Data != null)
            {
                // Update existing thread
                var existingThread = existingThreadResult.Data;
                existingThread.MainTitle = parsedThread.MainTitle;
                existingThread.Type = parsedThread.Type;
                existingThread.Note = parsedThread.Note;

                var updateResult = await _repository.UpdateThreadAsync(existingThread, cancellationToken);
                if (!updateResult.IsSuccess)
                    return Result<ThreadProcessingResultDto>.Failure($"Failed to update thread: {updateResult.ErrorMessage}");

                thread = updateResult.Data;
                isNewThread = false;
                _logger.LogInformation("Updated existing thread: {ThreadId}", thread.Id);
            }
            else
            {
                // Create new thread
                var createResult = await _repository.CreateThreadAsync(parsedThread, cancellationToken);
                if (!createResult.IsSuccess)
                    return Result<ThreadProcessingResultDto>.Failure($"Failed to create thread: {createResult.ErrorMessage}");

                thread = createResult.Data;
                isNewThread = true;
                _logger.LogInformation("Created new thread: {ThreadId}", thread.Id);
            }

            // Parse and process links
            var processLinksResult = await ProcessThreadLinks(pageContentResult.Data, thread, cancellationToken);
            if (!processLinksResult.IsSuccess)
                return Result<ThreadProcessingResultDto>.Failure($"Failed to process links: {processLinksResult.ErrorMessage}");

            var linkStats = processLinksResult.Data;

            // Get total links count
            var totalLinksResult = await _repository.GetLinksByThreadIdAsync(thread.Id, cancellationToken);
            var totalLinksCount = totalLinksResult.IsSuccess ? totalLinksResult.Data.Count : 0;

            var result = new ThreadProcessingResultDto
            {
                ThreadId = thread.Id,
                Title = thread.MainTitle ?? string.Empty,
                Url = thread.Url ?? string.Empty,
                IsNewThread = isNewThread,
                NewLinksCount = linkStats.NewLinksCount,
                UpdatedLinksCount = linkStats.UpdatedLinksCount,
                TotalLinksCount = totalLinksCount,
                ProcessedAt = DateTime.Now
            };

            _logger.LogInformation("Successfully processed thread {ThreadId}: {NewLinks} new, {UpdatedLinks} updated, {TotalLinks} total",
                thread.Id, linkStats.NewLinksCount, linkStats.UpdatedLinksCount, totalLinksCount);

            return Result<ThreadProcessingResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing thread: {Url}", threadUrl);
            return Result<ThreadProcessingResultDto>.Failure($"Unexpected error processing thread: {ex.Message}");
        }
    }

    public async Task<Result<ThreadProcessingResultDto>> RefreshThreadLinksAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refreshing thread links: {ThreadId}", threadId);

            // Get existing thread
            var threadResult = await _repository.GetThreadByIdAsync(threadId, cancellationToken);
            if (!threadResult.IsSuccess)
                return Result<ThreadProcessingResultDto>.Failure($"Failed to get thread: {threadResult.ErrorMessage}");

            if (threadResult.Data == null)
                return Result<ThreadProcessingResultDto>.Failure("Thread not found");

            var thread = threadResult.Data;
            if (string.IsNullOrWhiteSpace(thread.Url))
                return Result<ThreadProcessingResultDto>.Failure("Thread URL is not available");

            // Process thread using existing URL
            return await ProcessThreadAsync(thread.Url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing thread links: {ThreadId}", threadId);
            return Result<ThreadProcessingResultDto>.Failure($"Unexpected error refreshing thread: {ex.Message}");
        }
    }

    public async Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var threadsResult = await _repository.GetActiveThreadsAsync(cancellationToken);
            if (!threadsResult.IsSuccess)
                return Result<List<ThreadSummaryDto>>.Failure($"Failed to get threads: {threadsResult.ErrorMessage}");

            var threads = threadsResult.Data;
            if (!threads.Any())
                return Result<List<ThreadSummaryDto>>.Success(new List<ThreadSummaryDto>());

            // Get link counts for all threads
            var threadIds = threads.Select(t => t.Id).ToList();
            var linkCountsResult = await _repository.GetThreadsLinkCountsAsync(threadIds, cancellationToken);
            var linkCounts = linkCountsResult.IsSuccess ? linkCountsResult.Data : new Dictionary<int, int>();

            // Map to DTOs with link statistics
            var threadSummaries = new List<ThreadSummaryDto>();
            foreach (var thread in threads)
            {
                var dto = _mapper.Map<ThreadSummaryDto>(thread);
                dto.LinksCount = linkCounts.GetValueOrDefault(thread.Id, 0);

                // Get new links count
                var newLinksCountResult = await _repository.GetNewLinksCountAsync(thread.Id, cancellationToken);
                dto.NewLinksCount = newLinksCountResult.IsSuccess ? newLinksCountResult.Data : 0;
                dto.HasNewLinks = dto.NewLinksCount > 0;

                // Calculate used links count (approximate)
                dto.UsedLinksCount = Math.Max(0, dto.LinksCount - dto.NewLinksCount);

                threadSummaries.Add(dto);
            }

            return Result<List<ThreadSummaryDto>>.Success(threadSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active threads");
            return Result<List<ThreadSummaryDto>>.Failure($"Failed to get active threads: {ex.Message}");
        }
    }

    public async Task<Result<List<LinkDto>>> GetThreadLinksAsync(int threadId, bool includeUsed = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var linksResult = await _repository.GetLinksByThreadIdAsync(threadId, cancellationToken);
            if (!linksResult.IsSuccess)
                return Result<List<LinkDto>>.Failure($"Failed to get thread links: {linksResult.ErrorMessage}");

            var links = linksResult.Data;
            if (!includeUsed)
            {
                links = links.Where(l => !l.IsUsed).ToList();
            }

            var linkDtos = _mapper.Map<List<LinkDto>>(links);
            return Result<List<LinkDto>>.Success(linkDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thread links: {ThreadId}", threadId);
            return Result<List<LinkDto>>.Failure($"Failed to get thread links: {ex.Message}");
        }
    }

    public async Task<Result<LinkUsageResultDto>> UseLink(int linkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var linkResult = await _repository.MarkLinkAsUsedAsync(linkId, cancellationToken);
            if (!linkResult.IsSuccess)
                return Result<LinkUsageResultDto>.Failure($"Failed to mark link as used: {linkResult.ErrorMessage}");

            var usageResult = _mapper.Map<LinkUsageResultDto>(linkResult.Data);
            
            _logger.LogInformation("Link {LinkId} marked as used", linkId);
            return Result<LinkUsageResultDto>.Success(usageResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using link: {LinkId}", linkId);
            return Result<LinkUsageResultDto>.Failure($"Failed to use link: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeactivateThreadAsync(int threadId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _repository.DeactivateThreadAsync(threadId, cancellationToken);
            if (!result.IsSuccess)
                return Result<bool>.Failure($"Failed to deactivate thread: {result.ErrorMessage}");

            _logger.LogInformation("Thread {ThreadId} deactivated", threadId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating thread: {ThreadId}", threadId);
            return Result<bool>.Failure($"Failed to deactivate thread: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<Result<(int NewLinksCount, int UpdatedLinksCount)>> ProcessThreadLinks(
        string pageContent, 
        DD_Threads thread, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse links from page content
            var parseLinksResult = await _webScrapingService.ParseEd2kLinksAsync(pageContent, thread, cancellationToken);
            if (!parseLinksResult.IsSuccess)
                return Result<(int, int)>.Failure($"Failed to parse links: {parseLinksResult.ErrorMessage}");

            var newLinks = parseLinksResult.Data;
            if (!newLinks.Any())
            {
                _logger.LogInformation("No links found in page content for thread {ThreadId}", thread.Id);
                return Result<(int, int)>.Success((0, 0));
            }

            // Get existing links for comparison
            var ed2kLinks = newLinks.Select(l => l.Ed2kLink).ToList();
            var existingLinksResult = await _repository.GetExistingLinksAsync(ed2kLinks, thread.Id, cancellationToken);
            if (!existingLinksResult.IsSuccess)
                return Result<(int, int)>.Failure($"Failed to check existing links: {existingLinksResult.ErrorMessage}");

            var existingLinks = existingLinksResult.Data;
            var existingLinkUrls = existingLinks.Select(l => l.Ed2kLink).ToHashSet();

            // Separate new and existing links
            var linksToAdd = newLinks.Where(l => !existingLinkUrls.Contains(l.Ed2kLink)).ToList();
            var linksToUpdate = existingLinks.Where(l => l.IsNew).ToList();

            int newLinksCount = 0;
            int updatedLinksCount = 0;

            // Add new links
            if (linksToAdd.Any())
            {
                foreach (var link in linksToAdd)
                {
                    link.IsNew = true;
                }

                var addResult = await _repository.CreateLinksAsync(linksToAdd, cancellationToken);
                if (addResult.IsSuccess)
                {
                    newLinksCount = addResult.Data;
                    _logger.LogInformation("Added {Count} new links for thread {ThreadId}", newLinksCount, thread.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to add new links: {Error}", addResult.ErrorMessage);
                }
            }

            // Update existing links (mark as not new)
            if (linksToUpdate.Any())
            {
                foreach (var link in linksToUpdate)
                {
                    link.IsNew = false;
                }

                var updateResult = await _repository.UpdateLinksAsync(linksToUpdate, cancellationToken);
                if (updateResult.IsSuccess)
                {
                    updatedLinksCount = updateResult.Data;
                    _logger.LogInformation("Updated {Count} existing links for thread {ThreadId}", updatedLinksCount, thread.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to update existing links: {Error}", updateResult.ErrorMessage);
                }
            }

            return Result<(int, int)>.Success((newLinksCount, updatedLinksCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing links for thread {ThreadId}", thread.Id);
            return Result<(int, int)>.Failure($"Failed to process links: {ex.Message}");
        }
    }

    #endregion
}