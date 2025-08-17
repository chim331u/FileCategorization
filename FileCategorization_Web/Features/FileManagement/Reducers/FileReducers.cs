using System.Collections.Immutable;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Features.FileManagement.Store;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using Fluxor;

namespace FileCategorization_Web.Features.FileManagement.Reducers;

public static class FileReducers
{
    // Loading State Reducers
    [ReducerMethod]
    public static FileState ReduceSetLoadingAction(FileState state, SetLoadingAction action) =>
        state with { IsLoading = action.IsLoading, Error = action.IsLoading ? null : state.Error };

    [ReducerMethod]
    public static FileState ReduceSetRefreshingAction(FileState state, SetRefreshingAction action) =>
        state with { IsRefreshing = action.IsRefreshing };

    [ReducerMethod]
    public static FileState ReduceSetTrainingAction(FileState state, SetTrainingAction action) =>
        state with { IsTraining = action.IsTraining };

    [ReducerMethod]
    public static FileState ReduceSetErrorAction(FileState state, SetErrorAction action) =>
        state with { Error = action.Error, IsLoading = false, IsRefreshing = false, IsTraining = false };

    // File Data Reducers
    [ReducerMethod]
    public static FileState ReduceLoadFilesAction(FileState state, LoadFilesAction action) =>
        state with { IsLoading = true, Error = null, SearchParameter = action.SearchParameter };

    [ReducerMethod]
    public static FileState ReduceLoadFilesSuccessAction(FileState state, LoadFilesSuccessAction action) =>
        state with { Files = action.Files, IsLoading = false, Error = null };

    [ReducerMethod]
    public static FileState ReduceLoadFilesFailureAction(FileState state, LoadFilesFailureAction action) =>
        state with { IsLoading = false, Error = action.Error };

    // Refresh Data Reducers
    [ReducerMethod]
    public static FileState ReduceRefreshDataAction(FileState state, RefreshDataAction action) =>
        state with { IsRefreshing = true, Error = null };

    [ReducerMethod]
    public static FileState ReduceRefreshDataSuccessAction(FileState state, RefreshDataSuccessAction action) =>
        state with { IsRefreshing = false, Error = null, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.Message}") };

    [ReducerMethod]
    public static FileState ReduceRefreshDataFailureAction(FileState state, RefreshDataFailureAction action) =>
        state with { IsRefreshing = false, Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    // Category Reducers
    [ReducerMethod]
    public static FileState ReduceLoadCategoriesSuccessAction(FileState state, LoadCategoriesSuccessAction action) =>
        state with { Categories = action.Categories };

    [ReducerMethod]
    public static FileState ReduceAddNewCategoryAction(FileState state, AddNewCategoryAction action) =>
        state with { Categories = state.Categories.Contains(action.Category) ? state.Categories : state.Categories.Add(action.Category) };

    // Configuration Reducers
    [ReducerMethod]
    public static FileState ReduceLoadConfigurationsSuccessAction(FileState state, LoadConfigurationsSuccessAction action) =>
        state with { Configurations = action.Configurations };

    [ReducerMethod]
    public static FileState ReduceCreateConfigurationSuccessAction(FileState state, CreateConfigurationSuccessAction action) =>
        state with { Configurations = state.Configurations.Add(action.CreatedConfiguration) };

    [ReducerMethod]
    public static FileState ReduceUpdateConfigurationSuccessAction(FileState state, UpdateConfigurationSuccessAction action)
    {
        var updatedConfigurations = state.Configurations.Select(c => 
            c.Id == action.UpdatedConfiguration.Id ? action.UpdatedConfiguration : c).ToImmutableList();
        return state with { Configurations = updatedConfigurations };
    }

    [ReducerMethod]
    public static FileState ReduceDeleteConfigurationSuccessAction(FileState state, DeleteConfigurationSuccessAction action)
    {
        var filteredConfigurations = state.Configurations.Where(c => c.Id != action.DeletedConfiguration.Id).ToImmutableList();
        return state with { Configurations = filteredConfigurations };
    }

    [ReducerMethod]
    public static FileState ReduceCreateConfigurationFailureAction(FileState state, CreateConfigurationFailureAction action) =>
        state with { Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    [ReducerMethod]
    public static FileState ReduceUpdateConfigurationFailureAction(FileState state, UpdateConfigurationFailureAction action) =>
        state with { Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    [ReducerMethod]
    public static FileState ReduceDeleteConfigurationFailureAction(FileState state, DeleteConfigurationFailureAction action) =>
        state with { Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    // File Management Reducers
    [ReducerMethod]
    public static FileState ReduceUpdateFileDetailSuccessAction(FileState state, UpdateFileDetailSuccessAction action)
    {
        var updatedFiles = state.Files.Select(f => f.Id == action.UpdatedFile.Id ? action.UpdatedFile : f).ToImmutableList();
        return state with { Files = updatedFiles };
    }

    [ReducerMethod]
    public static FileState ReduceScheduleFileAction(FileState state, ScheduleFileAction action)
    {
        var updatedFiles = state.Files.Select(f => f.Id == action.FileId ? 
            new FilesDetailDto { Id = f.Id, Name = f.Name, FileSize = f.FileSize, FileCategory = f.FileCategory, IsToCategorize = f.IsToCategorize, IsNew = f.IsNew, IsNotToMove = true } : f).ToImmutableList();
        return state with { Files = updatedFiles };
    }

    [ReducerMethod]
    public static FileState ReduceRevertFileAction(FileState state, RevertFileAction action)
    {
        var updatedFiles = state.Files.Select(f => f.Id == action.FileId ? 
            new FilesDetailDto { Id = f.Id, Name = f.Name, FileSize = f.FileSize, FileCategory = f.FileCategory, IsToCategorize = f.IsToCategorize, IsNew = f.IsNew, IsNotToMove = false } : f).ToImmutableList();
        return state with { Files = updatedFiles };
    }

    [ReducerMethod]
    public static FileState ReduceNotShowAgainFileAction(FileState state, NotShowAgainFileAction action)
    {
        var updatedFiles = state.Files.Select(f => f.Id == action.FileId ? 
            new FilesDetailDto { Id = f.Id, Name = f.Name, FileSize = f.FileSize, FileCategory = f.FileCategory, IsToCategorize = false, IsNew = false, IsNotToMove = true } : f).ToImmutableList();
        return state with { Files = updatedFiles };
    }

    // ML Model Reducers
    [ReducerMethod]
    public static FileState ReduceTrainModelAction(FileState state, TrainModelAction action) =>
        state with { IsTraining = true, Error = null, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Start Model Train...") };

    [ReducerMethod]
    public static FileState ReduceTrainModelSuccessAction(FileState state, TrainModelSuccessAction action) =>
        state with { IsTraining = false, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.Message}") };

    [ReducerMethod]
    public static FileState ReduceTrainModelFailureAction(FileState state, TrainModelFailureAction action) =>
        state with { IsTraining = false, Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    [ReducerMethod]
    public static FileState ReduceForceCategoryAction(FileState state, ForceCategoryAction action) =>
        state with { IsCategorizing = true, Error = null, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Starting Categorization...") };

    [ReducerMethod]
    public static FileState ReduceForceCategorySuccessAction(FileState state, ForceCategorySuccessAction action) =>
        state with { IsCategorizing = false, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.Message}") };

    [ReducerMethod]
    public static FileState ReduceForceCategoryFailureAction(FileState state, ForceCategoryFailureAction action) =>
        state with { IsCategorizing = false, Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    // Move Files Reducers
    [ReducerMethod]
    public static FileState ReduceMoveFilesSuccessAction(FileState state, MoveFilesSuccessAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Scheduled job n {action.JobId}") };

    [ReducerMethod]
    public static FileState ReduceMoveFilesFailureAction(FileState state, MoveFilesFailureAction action) =>
        state with { Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    // Search and Filter Reducers
    [ReducerMethod]
    public static FileState ReduceSetSearchParameterAction(FileState state, SetSearchParameterAction action) =>
        state with { SearchParameter = action.SearchParameter };

    [ReducerMethod]
    public static FileState ReduceSetSelectedCategoryAction(FileState state, SetSelectedCategoryAction action) =>
        state with { SelectedCategory = action.Category };

    // Console Reducers
    [ReducerMethod]
    public static FileState ReduceAddConsoleMessageAction(FileState state, AddConsoleMessageAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.Message}") };

    [ReducerMethod]
    public static FileState ReduceClearConsoleAction(FileState state, ClearConsoleAction action) =>
        state with { ConsoleMessages = ImmutableList<string>.Empty };

    // SignalR Reducers
    [ReducerMethod]
    public static FileState ReduceSignalRConnectedAction(FileState state, SignalRConnectedAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - SignalR - Connection established. Connection ID: {action.ConnectionId}") };

    [ReducerMethod]
    public static FileState ReduceSignalRDisconnectedAction(FileState state, SignalRDisconnectedAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - SignalR - Connection lost") };

    [ReducerMethod]
    public static FileState ReduceSignalRFileMovedAction(FileState state, SignalRFileMovedAction action)
    {
        var message = $"{DateTime.Now:G} - File {action.ResultText}: {action.Result}";
        var updatedMessages = state.ConsoleMessages.Add(message);
        
        // Remove file from list if completed
        var updatedFiles = action.Result == MoveFilesResults.Completed 
            ? state.Files.Where(f => f.Id != action.FileId).ToImmutableList()
            : state.Files;
            
        return state with 
        { 
            Files = updatedFiles, 
            ConsoleMessages = updatedMessages 
        };
    }

    [ReducerMethod]
    public static FileState ReduceSignalRJobCompletedAction(FileState state, SignalRJobCompletedAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.ResultText}") };

    // Cache Reducers
    [ReducerMethod]
    public static FileState ReduceCacheWarmupAction(FileState state, CacheWarmupAction action) =>
        state with { IsCacheWarming = true, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Starting cache warmup...") };

    [ReducerMethod]
    public static FileState ReduceCacheWarmupSuccessAction(FileState state, CacheWarmupSuccessAction action) =>
        state with { IsCacheWarming = false, LastCacheUpdate = DateTime.UtcNow, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache warmup completed") };

    [ReducerMethod]
    public static FileState ReduceCacheWarmupFailureAction(FileState state, CacheWarmupFailureAction action) =>
        state with { IsCacheWarming = false, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache warmup failed: {action.Error}") };

    [ReducerMethod]
    public static FileState ReduceCacheClearAction(FileState state, CacheClearAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Clearing cache...") };

    [ReducerMethod]
    public static FileState ReduceCacheClearSuccessAction(FileState state, CacheClearSuccessAction action) =>
        state with { CacheStatistics = null, LastCacheUpdate = DateTime.UtcNow, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache cleared successfully") };

    [ReducerMethod]
    public static FileState ReduceCacheInvalidateSuccessAction(FileState state, CacheInvalidateSuccessAction action) =>
        state with { LastCacheUpdate = DateTime.UtcNow, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache invalidated: {action.Strategy}") };

    [ReducerMethod]
    public static FileState ReduceCacheStatsUpdateAction(FileState state, CacheStatsUpdateAction action) =>
        state with { CacheStatistics = action.Statistics, LastCacheUpdate = DateTime.UtcNow };

    [ReducerMethod]
    public static FileState ReduceCacheHitAction(FileState state, CacheHitAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache HIT: {action.Key} ({action.DataType})") };

    [ReducerMethod]
    public static FileState ReduceCacheMissAction(FileState state, CacheMissAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Cache MISS: {action.Key} ({action.DataType})") };
}