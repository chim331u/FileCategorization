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
    private readonly ILogger<CachedServiceApiWrapper> _logger;

    public CachedServiceApiWrapper(
        IServiceApi serviceApi, 
        ICacheService cacheService, 
        ILogger<CachedServiceApiWrapper> logger)
    {
        _serviceApi = serviceApi;
        _cacheService = cacheService;
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
        return await _cacheService.GetOrSetAsync(
            "categories:all:result",
            () => _serviceApi.GetCategoriesAsync(),
            TimeSpan.FromMinutes(10));
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
        return await _cacheService.GetOrSetAsync(
            "files:filtered:3:result",
            () => _serviceApi.GetFilesAsync(),
            TimeSpan.FromMinutes(2));
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
        return await _cacheService.GetOrSetAsync(
            "files:lastview:result",
            () => _serviceApi.GetLastFilesListAsync(),
            TimeSpan.FromMinutes(1));
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
        return await _cacheService.GetOrSetAsync(
            $"files:category:{fileCategory}:result",
            () => _serviceApi.GetAllFilesAsync(fileCategory),
            TimeSpan.FromMinutes(2));
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
        var result = await _serviceApi.RefreshCategoryAsync();
        
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
        var result = await _serviceApi.MoveFileAsync(fileDetail);
        
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
        var result = await _serviceApi.MoveFilesAsync(filesToMove);
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation($"Invalidated file caches after moving {filesToMove.Count} files");
        }
        
        return result;
    }

    /// <summary>
    /// UpdateFileDetail invalidates specific file and file list caches
    /// </summary>
    public async Task<FilesDetailDto> UpdateFileDetail(FilesDetailDto item)
    {
        var result = await UpdateFileDetailAsync(item);
        return result.IsSuccess ? result.Value! : item;
    }

    public async Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item)
    {
        var result = await _serviceApi.UpdateFileDetailAsync(item);
        
        if (result.IsSuccess)
        {
            await _cacheService.RemoveAsync($"file:{item.Id}");
            await InvalidateFileCaches();
            _logger.LogInformation($"Invalidated caches after updating file: {item.Name}");
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
        var result = await _serviceApi.TrainModelAsync();
        
        if (result.IsSuccess)
        {
            await InvalidateFileCaches();
            _logger.LogInformation("Invalidated file caches after TrainModelAsync");
        }
        
        return result;
    }

    #endregion

    #region Non-Cached Methods (Single Item or Low Volume)

    /// <summary>
    /// GetFile with short cache (individual files may change)
    /// </summary>
    public async Task<FilesDetailDto> GetFile(int id)
    {
        var result = await GetFileAsync(id);
        return result.IsSuccess ? result.Value! : new FilesDetailDto();
    }

    public async Task<Result<FilesDetailDto>> GetFileAsync(int id)
    {
        return await _cacheService.GetOrSetAsync(
            $"file:{id}:result",
            () => _serviceApi.GetFileAsync(id),
            TimeSpan.FromMinutes(1));
    }

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