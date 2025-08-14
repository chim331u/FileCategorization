using System.Collections.Immutable;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Interfaces;
using Fluxor;

namespace FileCategorization_Web.Features.FileManagement.Effects;

public class FileEffects
{
    private readonly IFileCategorizationService _fileService;
    private readonly ILogger<FileEffects> _logger;

    public FileEffects(IFileCategorizationService fileService, ILogger<FileEffects> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    // Load Files Effect
    [EffectMethod]
    public async Task HandleLoadFilesAction(LoadFilesAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Loading files with search parameter: {SearchParameter}", action.SearchParameter);
            
            var result = await _fileService.GetFileListAsync(action.SearchParameter);
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new LoadFilesSuccessAction(result.Value?.ToImmutableList() ?? ImmutableList<Data.DTOs.FileCategorizationDTOs.FilesDetailDto>.Empty));
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
            _logger.LogInformation("Loading categories");
            
            var result = await _fileService.GetCategoryListAsync();
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new LoadCategoriesSuccessAction(result.Value?.ToImmutableList() ?? ImmutableList<string>.Empty));
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
            _logger.LogInformation("Loading configurations");
            
            var result = await _fileService.GetConfigListAsync();
            
            if (result.IsSuccess)
            {
                dispatcher.Dispatch(new LoadConfigurationsSuccessAction(result.Value?.ToImmutableList() ?? ImmutableList<Data.DTOs.FileCategorizationDTOs.ConfigsDto>.Empty));
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
}