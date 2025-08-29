using FileCategorization_App.Components.Interface;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.DD;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Cached wrapper for DDwebService providing intelligent caching for DownloadDaemon data
/// </summary>
public class CachedDDwebServiceWrapper : IDDwebService
{
    private readonly IDDwebService _ddwebService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedDDwebServiceWrapper> _logger;

    public CachedDDwebServiceWrapper(
        IDDwebService ddwebService, 
        ICacheService cacheService, 
        ILogger<CachedDDwebServiceWrapper> logger)
    {
        _ddwebService = ddwebService;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Cached Methods (Read Operations)

    /// <summary>
    /// GetActiveThreads with 1-minute cache (DD threads change frequently)
    /// </summary>
    public async Task<List<ThreadSummaryDto>> GetActiveThreads()
    {
        var result = await GetActiveThreadsAsync();
        return result.IsSuccess ? result.Value ?? new List<ThreadSummaryDto>() : new List<ThreadSummaryDto>();
    }

    public async Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync()
    {
        return await _cacheService.GetOrSetAsync(
            "dd:threads:active:result",
            () => _ddwebService.GetActiveThreadsAsync(),
            TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// GetEd2kLinks with 30-second cache (links change very frequently)
    /// </summary>
    public async Task<List<LinkDto>> GetEd2kLinks(int threadId)
    {
        var result = await GetEd2kLinksAsync(threadId);
        return result.IsSuccess ? result.Value ?? new List<LinkDto>() : new List<LinkDto>();
    }

    public async Task<Result<List<LinkDto>>> GetEd2kLinksAsync(int threadId)
    {
        return await _cacheService.GetOrSetAsync(
            $"dd:links:thread:{threadId}:result",
            () => _ddwebService.GetEd2kLinksAsync(threadId),
            TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Cache-Invalidating Methods (Mutating Operations)

    /// <summary>
    /// UseLink invalidates thread-specific link caches
    /// </summary>
    public async Task<string> UseLink(int linkId)
    {
        var result = await UseLinkAsync(linkId);
        return result.IsSuccess ? result.Value ?? "Link used successfully" : "Link use failed";
    }

    public async Task<Result<string>> UseLinkAsync(int linkId)
    {
        var result = await _ddwebService.UseLinkAsync(linkId);
        
        if (result.IsSuccess)
        {
            await _cacheService.RemovePatternAsync("dd:links:*");
            _logger.LogInformation($"Invalidated DD link caches after using link: {linkId}");
        }
        
        return result;
    }

    /// <summary>
    /// RenewThread invalidates thread and link caches
    /// </summary>
    public async Task<bool> RenewThread(int threadId)
    {
        var result = await RenewThreadAsync(threadId);
        return result.IsSuccess && result.Value;
    }

    public async Task<Result<bool>> RenewThreadAsync(int threadId)
    {
        var result = await _ddwebService.RenewThreadAsync(threadId);
        
        if (result.IsSuccess && result.Value)
        {
            await InvalidateThreadCaches(threadId);
            _logger.LogInformation($"Invalidated DD caches after renewing thread: {threadId}");
        }
        
        return result;
    }

    /// <summary>
    /// CheckUrl - legacy method, no caching due to deprecation
    /// </summary>
    [Obsolete("This method uses v1 API and will be removed in future versions")]
    public async Task<bool> CheckUrl(string urlToCheck)
    {
        var result = await CheckUrlAsync(urlToCheck);
        return result.IsSuccess && result.Value;
    }

    [Obsolete("This method is deprecated in v2 API and will be removed")]
    public async Task<Result<bool>> CheckUrlAsync(string urlToCheck)
    {
        var result = await _ddwebService.CheckUrlAsync(urlToCheck);
        
        if (result.IsSuccess && result.Value)
        {
            await _cacheService.RemovePatternAsync("dd:threads:*");
            _logger.LogInformation("Invalidated DD thread caches after CheckUrlAsync");
        }
        
        return result;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Invalidates caches for specific thread and all threads list
    /// </summary>
    private async Task InvalidateThreadCaches(int threadId)
    {
        await Task.WhenAll(
            _cacheService.RemovePatternAsync("dd:threads:*"), // All thread lists
            _cacheService.RemovePatternAsync($"dd:links:thread:{threadId}*") // Specific thread links
        );
    }

    #endregion
}