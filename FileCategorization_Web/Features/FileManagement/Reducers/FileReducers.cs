using System.Collections.Immutable;
using FileCategorization_Web.Features.FileManagement.Actions;
using FileCategorization_Web.Features.FileManagement.Store;
using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;
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
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Starting Categorization...") };

    [ReducerMethod]
    public static FileState ReduceForceCategorySuccessAction(FileState state, ForceCategorySuccessAction action) =>
        state with { ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - {action.Message}") };

    [ReducerMethod]
    public static FileState ReduceForceCategoryFailureAction(FileState state, ForceCategoryFailureAction action) =>
        state with { Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

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
}