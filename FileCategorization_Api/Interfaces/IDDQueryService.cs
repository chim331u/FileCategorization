using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.DD;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Query service interface for DD operations with Result Pattern and DTOs
/// </summary>
public interface IDDQueryService
{
    /// <summary>
    /// Processes a thread by URL, creating/updating thread and extracting links
    /// </summary>
    /// <param name="threadUrl">The URL of the thread to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result with thread information</returns>
    Task<Result<ThreadProcessingResultDto>> ProcessThreadAsync(string threadUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes links for an existing thread by ID
    /// </summary>
    /// <param name="threadId">The ID of the thread to refresh</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result with updated thread information</returns>
    Task<Result<ThreadProcessingResultDto>> RefreshThreadLinksAsync(int threadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active threads with link statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active threads</returns>
    Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves links for a specific thread
    /// </summary>
    /// <param name="threadId">The ID of the thread</param>
    /// <param name="includeUsed">Whether to include used links</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of links for the thread</returns>
    Task<Result<List<LinkDto>>> GetThreadLinksAsync(int threadId, bool includeUsed = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a link as used and returns the link content
    /// </summary>
    /// <param name="linkId">The ID of the link to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ED2K link content</returns>
    Task<Result<LinkUsageResultDto>> UseLink(int linkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a thread and all its associated links
    /// </summary>
    /// <param name="threadId">The ID of the thread to deactivate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> DeactivateThreadAsync(int threadId, CancellationToken cancellationToken = default);
}