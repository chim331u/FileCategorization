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
    /// Checks URL validity (deprecated in v2, uses v1 API)
    /// </summary>
    [Obsolete("This method uses v1 API and will be removed in future versions")]
    Task<Result<bool>> CheckUrlAsync(string urlToCheck);
    
    #region Legacy Sync Methods (for backward compatibility)
    
    /// <summary>
    /// Legacy sync method for GetActiveThreads
    /// </summary>
    Task<List<ThreadSummaryDto>> GetActiveThreads();
    
    /// <summary>
    /// Legacy sync method for GetEd2kLinks
    /// </summary>
    Task<List<LinkDto>> GetEd2kLinks(int threadId);
    
    /// <summary>
    /// Legacy sync method for UseLink
    /// </summary>
    Task<string> UseLink(int linkId);
    
    /// <summary>
    /// Legacy sync method for RenewThread
    /// </summary>
    Task<bool> RenewThread(int threadId);
    
    /// <summary>
    /// Legacy sync method for CheckUrl
    /// </summary>
    [Obsolete("This method uses v1 API and will be removed in future versions")]
    Task<bool> CheckUrl(string urlToCheck);
    
    #endregion
}