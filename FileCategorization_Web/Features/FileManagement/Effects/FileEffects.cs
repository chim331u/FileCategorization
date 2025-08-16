using System.Collections.Immutable;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Data.Caching;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Shared.DTOs.Configuration;
using FileCategorization_Shared.Enums;
using Fluxor;

namespace FileCategorization_Web.Features.FileManagement.Effects;

public class FileEffects
{
    private readonly IFileCategorizationService _fileService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<FileEffects> _logger;

    public FileEffects(IFileCategorizationService fileService, ICacheService cacheService, ILogger<FileEffects> logger)
    {
        _fileService = fileService;
        _cacheService = cacheService;
        _logger = logger;
    }

    // Load Files Effect
    [EffectMethod]
    public async Task HandleLoadFilesAction(LoadFilesAction action, IDispatcher dispatcher)
    {
        try
        {
            var cacheKey = $"files_{action.SearchParameter}";
            _logger.LogInformation("Loading files with search parameter: {SearchParameter}", action.SearchParameter);
            
            // Try to get from cache first
            var cachedFiles = await _cacheService.GetAsync<IEnumerable<FilesDetailDto>>(cacheKey);
            if (cachedFiles != null)
            {
                dispatcher.Dispatch(new CacheHitAction(cacheKey, "FilesDetailDto[]"));
                dispatcher.Dispatch(new LoadFilesSuccessAction(cachedFiles.ToImmutableList()));
                dispatcher.Dispatch(new AddConsoleMessageAction("File List loaded from cache"));
                return;
            }

            dispatcher.Dispatch(new CacheMissAction(cacheKey, "FilesDetailDto[]"));
            
            var result = await _fileService.GetFileListAsync(action.SearchParameter);
            
            if (result.IsSuccess)
            {
                var files = result.Value?.ToImmutableList() ?? ImmutableList<FilesDetailDto>.Empty;
                
                // Cache the result using predefined policy
                if (result.Value != null)
                {
                    await _cacheService.SetAsync(cacheKey, result.Value, CachePolicy.FileList);
                }
                dispatcher.Dispatch(new CacheSetAction(cacheKey, "FilesDetailDto[]", TimeSpan.FromMinutes(5)));
                
                dispatcher.Dispatch(new LoadFilesSuccessAction(files));
                dispatcher.Dispatch(new AddConsoleMessageAction("File List updated"));
            }
            else
            {
                dispatcher.Dispatch(new LoadFilesFailureAction(result.Error ?? "Failed to load files"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading files");
            dispatcher.Dispatch(new LoadFilesFailureAction($"Error loading files: {ex.Message}"));
        }
    }

    // Refresh Data Effect
    [EffectMethod]
    public async Task HandleRefreshDataAction(RefreshDataAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Refreshing category data");
            
            // Invalidate related caches before refreshing
            await _cacheService.InvalidateByTagAsync("categories");
            await _cacheService.InvalidateByTagAsync("files");
            
            var refreshResult = await _fileService.RefreshCategoryAsync();
            
            if (refreshResult.IsSuccess)
            {
                dispatcher.Dispatch(new RefreshDataSuccessAction(refreshResult.Value ?? "Data refreshed"));
                
                // Load categories after successful refresh
                dispatcher.Dispatch(new LoadCategoriesAction());
            }
            else
            {
                dispatcher.Dispatch(new RefreshDataFailureAction(refreshResult.Error ?? "Failed to refresh data"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing data");
            dispatcher.Dispatch(new RefreshDataFailureAction($"Error refreshing data: {ex.Message}"));
        }
    }

    // Load Categories Effect
    [EffectMethod]
    public async Task HandleLoadCategoriesAction(LoadCategoriesAction action, IDispatcher dispatcher)
    {
        try
        {
            var cacheKey = "categories";
            _logger.LogInformation("Loading categories");
            
            // Try to get from cache first
            var cachedCategories = await _cacheService.GetAsync<IEnumerable<string>>(cacheKey);
            if (cachedCategories != null)
            {
                dispatcher.Dispatch(new CacheHitAction(cacheKey, "string[]"));
                dispatcher.Dispatch(new LoadCategoriesSuccessAction(cachedCategories.ToImmutableList()));
                return;
            }

            dispatcher.Dispatch(new CacheMissAction(cacheKey, "string[]"));
            
            var result = await _fileService.GetCategoryListAsync();
            
            if (result.IsSuccess)
            {
                var categories = result.Value?.ToImmutableList() ?? ImmutableList<string>.Empty;
                
                // Cache the result using predefined policy
                if (result.Value != null)
                {
                    await _cacheService.SetAsync(cacheKey, result.Value, CachePolicy.Categories);
                }
                dispatcher.Dispatch(new CacheSetAction(cacheKey, "string[]", TimeSpan.FromMinutes(30)));
                
                dispatcher.Dispatch(new LoadCategoriesSuccessAction(categories));
            }
            else
            {
                dispatcher.Dispatch(new LoadCategoriesFailureAction(result.Error ?? "Failed to load categories"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            dispatcher.Dispatch(new LoadCategoriesFailureAction($"Error loading categories: {ex.Message}"));
        }
    }

    // Load Configurations Effect
    [EffectMethod]
    public async Task HandleLoadConfigurationsAction(LoadConfigurationsAction action, IDispatcher dispatcher)
    {
        try
        {
            var cacheKey = "configurations";
            _logger.LogInformation("Loading configurations");
            
            // Try to get from cache first
            var cachedConfigurations = await _cacheService.GetAsync<IEnumerable<ConfigsDto>>(cacheKey);
            if (cachedConfigurations != null)
            {
                dispatcher.Dispatch(new CacheHitAction(cacheKey, "ConfigsDto[]"));
                dispatcher.Dispatch(new LoadConfigurationsSuccessAction(cachedConfigurations.ToImmutableList()));
                return;
            }

            dispatcher.Dispatch(new CacheMissAction(cacheKey, "ConfigsDto[]"));
            
            var result = await _fileService.GetConfigListAsync();
            
            if (result.IsSuccess)
            {
                var configurations = result.Value?.ToImmutableList() ?? ImmutableList<ConfigsDto>.Empty;
                
                // Cache the result using predefined policy
                if (result.Value != null)
                {
                    await _cacheService.SetAsync(cacheKey, result.Value, CachePolicy.Configurations);
                }
                dispatcher.Dispatch(new CacheSetAction(cacheKey, "ConfigsDto[]", TimeSpan.FromHours(1)));
                
                dispatcher.Dispatch(new LoadConfigurationsSuccessAction(configurations));
            }
            else
            {
                dispatcher.Dispatch(new LoadConfigurationsFailureAction(result.Error ?? "Failed to load configurations"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configurations");
            dispatcher.Dispatch(new LoadConfigurationsFailureAction($"Error loading configurations: {ex.Message}"));
        }
    }

    // Update File Detail Effect
    [EffectMethod]
    public async Task HandleUpdateFileDetailAction(UpdateFileDetailAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Updating file detail for ID: {FileId}", action.File.Id);
            
            var result = await _fileService.UpdateFileDetailAsync(action.File);
            
            if (result.IsSuccess && result.Value != null)
            {
                // Invalidate files cache after update
                await _cacheService.InvalidateByTagAsync("files");
                
                dispatcher.Dispatch(new UpdateFileDetailSuccessAction(result.Value));
                dispatcher.Dispatch(new AddConsoleMessageAction($"File {action.File.Name} updated successfully"));
            }
            else
            {
                dispatcher.Dispatch(new UpdateFileDetailFailureAction(result.Error ?? "Failed to update file detail"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file detail");
            dispatcher.Dispatch(new UpdateFileDetailFailureAction($"Error updating file detail: {ex.Message}"));
        }
    }

    // Train Model Effect
    [EffectMethod]
    public async Task HandleTrainModelAction(TrainModelAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Starting model training");
            
            var result = await _fileService.TrainModelAsync();
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new TrainModelSuccessAction(result.Value ?? "Model training completed"));
            }
            else
            {
                dispatcher.Dispatch(new TrainModelFailureAction(result.Error ?? "Failed to train model"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model");
            dispatcher.Dispatch(new TrainModelFailureAction($"Error training model: {ex.Message}"));
        }
    }

    // Force Category Effect
    [EffectMethod]
    public async Task HandleForceCategoryAction(ForceCategoryAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Starting force categorization");
            
            var result = await _fileService.ForceCategoryAsync();
            
            if (result.IsSuccess)
            {
                // Invalidate files cache after categorization
                await _cacheService.InvalidateByTagAsync("files");
                
                dispatcher.Dispatch(new ForceCategorySuccessAction(result.Value ?? "Force categorization completed"));
                // Reload files after categorization
                dispatcher.Dispatch(new LoadFilesAction(3)); // Default to "To Categorize"
            }
            else
            {
                dispatcher.Dispatch(new ForceCategoryFailureAction(result.Error ?? "Failed to force categorization"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing categorization");
            dispatcher.Dispatch(new ForceCategoryFailureAction($"Error forcing categorization: {ex.Message}"));
        }
    }

    // Move Files Effect
    [EffectMethod]
    public async Task HandleMoveFilesAction(MoveFilesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Moving {Count} files", action.FilesToMove.Count);
            
            var result = await _fileService.MoveFilesAsync(action.FilesToMove.ToList());
            
            if (result.IsSuccess)
            {
                // Invalidate files cache after moving
                await _cacheService.InvalidateByTagAsync("files");
                
                dispatcher.Dispatch(new MoveFilesSuccessAction(result.Value ?? "Files moved successfully"));
                // Reload files after moving
                dispatcher.Dispatch(new LoadFilesAction(3)); // Default to "To Categorize"
            }
            else
            {
                dispatcher.Dispatch(new MoveFilesFailureAction(result.Error ?? "Failed to move files"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving files");
            dispatcher.Dispatch(new MoveFilesFailureAction($"Error moving files: {ex.Message}"));
        }
    }

    // Not Show Again Effect  
    [EffectMethod]
    public async Task HandleNotShowAgainFileAction(NotShowAgainFileAction action, IDispatcher dispatcher)
    {
        try
        {
            // This is handled by the reducer, but we need to update the backend
            // We'll need to find the file and update it
            _logger.LogInformation("Marking file {FileId} as not to show again", action.FileId);
            
            // For now, just add a console message
            dispatcher.Dispatch(new AddConsoleMessageAction($"File will not show again"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking file as not to show again");
            dispatcher.Dispatch(new SetErrorAction($"Error updating file: {ex.Message}"));
        }
    }

    // Cache Management Effects
    [EffectMethod]
    public async Task HandleCacheClearAction(CacheClearAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Clearing all cache");
            await _cacheService.ClearAllAsync();
            dispatcher.Dispatch(new CacheClearSuccessAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            dispatcher.Dispatch(new CacheClearFailureAction($"Failed to clear cache: {ex.Message}"));
        }
    }

    [EffectMethod]
    public async Task HandleCacheInvalidateAction(CacheInvalidateAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Invalidating cache with strategy: {Strategy}", action.Strategy);
            
            switch (action.Strategy)
            {
                case CacheInvalidationStrategy.FileData:
                    await _cacheService.InvalidateByTagAsync("files");
                    break;
                case CacheInvalidationStrategy.Categories:
                    await _cacheService.InvalidateByTagAsync("categories");
                    break;
                case CacheInvalidationStrategy.Configurations:
                    await _cacheService.InvalidateByTagAsync("configurations");
                    break;
                case CacheInvalidationStrategy.All:
                    await _cacheService.ClearAllAsync();
                    break;
            }
            
            dispatcher.Dispatch(new CacheInvalidateSuccessAction(action.Strategy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache");
            dispatcher.Dispatch(new CacheInvalidateFailureAction($"Failed to invalidate cache: {ex.Message}"));
        }
    }

    [EffectMethod]
    public async Task HandleCacheWarmupAction(CacheWarmupAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Starting cache warmup");
            
            // Warmup critical data
            dispatcher.Dispatch(new LoadCategoriesAction());
            dispatcher.Dispatch(new LoadConfigurationsAction());
            dispatcher.Dispatch(new LoadFilesAction(3)); // Default "To Categorize"
            
            // Update statistics
            var statistics = await _cacheService.GetStatisticsAsync();
            dispatcher.Dispatch(new CacheStatsUpdateAction(statistics));
            
            dispatcher.Dispatch(new CacheWarmupSuccessAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warmup");
            dispatcher.Dispatch(new CacheWarmupFailureAction($"Cache warmup failed: {ex.Message}"));
        }
    }
}