using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.DD;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Interface for DownloadDaemon API operations with Result Pattern support
/// </summary>
public interface IDDwebService
{
    /// <summary>
    /// Gets list of active download daemon threads
    /// </summary>
    Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync();
    
    /// <summary>
    /// Gets ED2K links for specific thread
    /// </summary>
    Task<Result<List<LinkDto>>> GetEd2kLinksAsync(int threadId);
    
    /// <summary>
    /// Marks link as used
    /// </summary>
    Task<Result<string>> UseLinkAsync(int linkId);

    /// <summary>
    /// Renews/refreshes thread links
    /// </summary>
    Task<Result<bool>> RenewThreadAsync(int threadId);
    
    /// <summary>
    /// Checks URL validity (deprecated in v2)
    /// </summary>
    [Obsolete("This method is deprecated in v2 API and will be removed")]
    Task<Result<bool>> CheckUrlAsync(string urlToCheck);
    
    #region Legacy Methods (for backward compatibility during migration)
    [Obsolete("Use GetActiveThreadsAsync instead. This method will be removed in Phase 2")]
    Task<List<ThreadSummaryDto>> GetActiveThreads();
    
    [Obsolete("Use GetEd2kLinksAsync instead. This method will be removed in Phase 2")]
    Task<List<LinkDto>> GetEd2kLinks(int threadId);
    
    [Obsolete("Use UseLinkAsync instead. This method will be removed in Phase 2")]
    Task<string> UseLink(int linkId);
    
    [Obsolete("Use RenewThreadAsync instead. This method will be removed in Phase 2")]
    Task<bool> RenewThread(int threadId);
    
    [Obsolete("Use CheckUrlAsync instead. This method will be removed in Phase 2")]
    Task<bool> CheckUrl(string urlToCheck);
    #endregion
}