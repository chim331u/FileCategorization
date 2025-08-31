using FileCategorization_App.Components.Interface;
using FileCategorization_App.Data;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.FileManagement;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Cached wrapper for ServiceApi providing intelligent caching for frequently accessed data
/// </summary>
public class CachedServiceApiWrapper : IServiceApi
{
    private readonly IServiceApi _serviceApi;
    private readonly ICacheService _cacheService;
    private readonly IConnectivityService _connectivityService;
    private readonly ILogger<CachedServiceApiWrapper> _logger;

    public CachedServiceApiWrapper(
        IServiceApi serviceApi, 
        ICacheService cacheService,
        IConnectivityService connectivityService,
        ILogger<CachedServiceApiWrapper> logger)
    {
        _serviceApi = serviceApi;
        _cacheService = cacheService;
        _connectivityService = connectivityService;
        _logger = logger;
    }

    #region Cached Methods (High Performance Impact)

    /// <summary>
    /// GetCategories with 10-minute cache (categories rarely change)
    /// </summary>
    public async Task<List<string>> GetCategories()
    {
        var result = await GetCategoriesAsync();
        return result.IsSuccess ? result.Value ?? new List<string>() : new List<string>();
    }

    public async Task<Result<List<string>>> GetCategoriesAsync()
    {
        return await _connectivityService.ExecuteWithConnectivityCheckAsync(
            async () => await _cacheService.GetOrSetAsync(
                "categories:all:result",
                () => _serviceApi.GetCategoriesAsync(),
                TimeSpan.FromMinutes(10)),
            "GetCategories");
    }

    /// <summary>
    /// GetFiles with 2-minute cache (file lists change moderately)
    /// </summary>
    public async Task<List<FilesDetailDto>> GetFiles()
    {
        var result = await GetFilesAsync();
        return result.IsSuccess ? result.Value ?? new List<FilesDetailDto>() : new List<FilesDetailDto>();
    }

    public async Task<Result<List<FilesDetailDto>>> GetFilesAsync()
    {
        return await _connectivityService.ExecuteWithConnectivityCheckAsync(
            async () => await _cacheService.GetOrSetAsync(
                "files:filtered:3:result",
                () => _serviceApi.GetFilesAsync(),
                TimeSpan.FromMinutes(2)),
            "GetFiles");
    }

    /// <summary>
    /// GetLastFilesList with 1-minute cache (last view changes frequently)
    /// </summary>
    public async Task<List<FilesDetailDto>> GetLastFilesList()
    {
        var result = await GetLastFilesListAsync();
        return result.IsSuccess ? result.Value ?? new List<FilesDetailDto>() : new List<FilesDetailDto>();
    }

    public async Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync()
    {
        return await _connectivityService.ExecuteWithConnectivityCheckAsync(
            async () => await _cacheService.GetOrSetAsync(
                "files:lastview:result",
                () => _serviceApi.GetLastFilesListAsync(),
                TimeSpan.FromMinutes(1)),
            "GetLastFilesList");
    }

    /// <summary>
    /// GetAllFiles with category-specific cache (2-minute cache per category)
    /// </summary>
    public async Task<List<FilesDetailDto>> GetAllFiles(string fileCategory)
    {
        var result = await GetAllFilesAsync(fileCategory);
        return result.IsSuccess ? result.Value ?? new List<FilesDetailDto>() : new List<FilesDetailDto>();
    }

    public async Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory)
    {
        return await _connectivityService.ExecuteWithConnectivityCheckAsync(
            async () => await _cacheService.GetOrSetAsync(
                $"files:category:{fileCategory}:result",
                () => _serviceApi.GetAllFilesAsync(fileCategory),
                TimeSpan.FromMinutes(2)),
            "GetAllFiles");
    }

    #endregion

    #region Cache-Invalidating Methods (Mutating Operations)

    /// <summary>
    /// RefreshCategory invalidates file caches
    /// </summary>
    public async Task<string> RefreshCategory()
    {
        var result = await RefreshCategoryAsync();
        return result.IsSuccess ? result.Value ?? "Refresh completed" : "Refresh failed";
    }

    public async Task<Result<string>> RefreshCategoryAsync()
    {
        var result = await _connectivityService.ExecuteWithConnectivityCheckAsync(
            () => _serviceApi.RefreshCategoryAsync(),
            "RefreshCategory");
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation("Invalidated file caches after RefreshCategoryAsync");
        }
        
        return result;
    }

    /// <summary>
    /// MoveFile invalidates file caches
    /// </summary>
    public async Task<string> MoveFile(FilesDetailDto fileDetail)
    {
        var result = await MoveFileAsync(fileDetail);
        return result.IsSuccess ? result.Value ?? "File moved successfully" : "File move failed";
    }

    public async Task<Result<string>> MoveFileAsync(FilesDetailDto fileDetail)
    {
        var result = await _connectivityService.ExecuteWithConnectivityCheckAsync(
            () => _serviceApi.MoveFileAsync(fileDetail),
            "MoveFile");
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation($"Invalidated file caches after moving file: {fileDetail.Name}");
        }
        
        return result;
    }

    /// <summary>
    /// MoveFiles invalidates file caches
    /// </summary>
    public async Task<string> MoveFiles(List<FilesDetailDto> filesToMove)
    {
        var result = await MoveFilesAsync(filesToMove);
        return result.IsSuccess ? result.Value ?? "Files moved successfully" : "Files move failed";
    }

    public async Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove)
    {
        var result = await _connectivityService.ExecuteWithConnectivityCheckAsync(
            () => _serviceApi.MoveFilesAsync(filesToMove),
            "MoveFiles");
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation($"Invalidated file caches after moving {filesToMove.Count} files");
        }
        
        return result;
    }

    /// <summary>
    /// Sets file as "not to show again" and invalidates related caches
    /// </summary>
    public async Task<Result<FilesDetailDto>> SetFileNotShowAgainAsync(int fileId)
    {
        var result = await _connectivityService.ExecuteWithConnectivityCheckAsync(
            () => _serviceApi.SetFileNotShowAgainAsync(fileId),
            "SetFileNotShowAgain");
        
        if (result.IsSuccess)
        {
            await _cacheService.RemoveAsync($"file:{fileId}");
            await InvalidateFileCaches();
            _logger.LogInformation($"Invalidated caches after marking file {fileId} as not to show again");
        }
        
        return result;
    }

    /// <summary>
    /// TrainModel may affect categorization, invalidate file caches
    /// </summary>
    public async Task<string> TrainModel()
    {
        var result = await TrainModelAsync();
        return result.IsSuccess ? result.Value ?? "Model trained successfully" : "Model training failed";
    }

    public async Task<Result<string>> TrainModelAsync()
    {
        var result = await _connectivityService.ExecuteWithConnectivityCheckAsync(
            () => _serviceApi.TrainModelAsync(),
            "TrainModel");
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation("Invalidated file caches after TrainModelAsync");
        }
        
        return result;
    }

    #endregion

    #region Non-Cached Methods (Single Item or Low Volume)


    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Invalidates all file-related caches
    /// </summary>
    private async Task InvalidateFileCaches()
    {
        await Task.WhenAll(
            _cacheService.RemovePatternAsync("files:*"),
            _cacheService.RemovePatternAsync("file:*")
        );
    }

    #endregion
}