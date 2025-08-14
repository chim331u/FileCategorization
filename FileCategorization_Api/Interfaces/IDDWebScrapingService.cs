using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Service interface for DD web scraping operations
/// </summary>
public interface IDDWebScrapingService
{
    /// <summary>
    /// Logs into DD website and retrieves page content
    /// </summary>
    /// <param name="threadUrl">The URL of the thread to access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTML content of the page</returns>
    Task<Result<string>> GetPageContentAsync(string threadUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses thread information from HTML content
    /// </summary>
    /// <param name="htmlContent">The HTML content to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed thread information</returns>
    Task<Result<DD_Threads>> ParseThreadInfoAsync(string htmlContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts ED2K links from HTML content
    /// </summary>
    /// <param name="htmlContent">The HTML content to parse</param>
    /// <param name="thread">The thread to associate links with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of extracted ED2K links</returns>
    Task<Result<List<DD_LinkEd2k>>> ParseEd2kLinksAsync(string htmlContent, DD_Threads thread, CancellationToken cancellationToken = default);
}