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

    // Last View Files Reducers
    [ReducerMethod]
    public static FileState ReduceLoadLastViewFilesAction(FileState state, LoadLastViewFilesAction action) =>
        state with { IsLoading = true, Error = null };

    [ReducerMethod]
    public static FileState ReduceLoadLastViewFilesSuccessAction(FileState state, LoadLastViewFilesSuccessAction action) =>
        state with { Files = action.Files, IsLoading = false, Error = null };

    [ReducerMethod]
    public static FileState ReduceLoadLastViewFilesFailureAction(FileState state, LoadLastViewFilesFailureAction action) =>
        state with { IsLoading = false, Error = action.Error };

    // Load Files by Category Reducers
    [ReducerMethod]
    public static FileState ReduceLoadFilesByCategoryAction(FileState state, LoadFilesByCategoryAction action) =>
        state with { IsLoading = true, Error = null, ExpandedCategory = action.Category };

    [ReducerMethod]
    public static FileState ReduceLoadFilesByCategorySuccessAction(FileState state, LoadFilesByCategorySuccessAction action) =>
        state with { ExpandedCategoryFiles = action.Files, ExpandedCategory = action.Category, IsLoading = false, Error = null };

    [ReducerMethod]
    public static FileState ReduceLoadFilesByCategoryFailureAction(FileState state, LoadFilesByCategoryFailureAction action) =>
        state with { IsLoading = false, Error = action.Error, ExpandedCategoryFiles = ImmutableList<FilesDetailDto>.Empty, ExpandedCategory = null };

    // Refresh Data Reducers
    [ReducerMethod]
    public static FileState ReduceRefreshDataAction(FileState state, RefreshDataAction action) =>
        state with { IsRefreshing = true, Error = null, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - Start refresh categories...") };

    [ReducerMethod]
    public static FileState ReduceRefreshDataSuccessAction(FileState state, RefreshDataSuccessAction action)
    {
        var formattedMessage = FormatRefreshMessage(action.Message);
        return state with { IsRefreshing = false, Error = null, ConsoleMessages = state.ConsoleMessages.Add(formattedMessage) };
    }

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
        // Remove the file completely from Files collection as it should no longer be visible
        var updatedFiles = state.Files.Where(f => f.Id != action.File.Id).ToImmutableList();
        
        // Also remove from ExpandedCategoryFiles if the file belongs to the currently expanded category
        var updatedExpandedFiles = state.ExpandedCategoryFiles;
        if (state.ExpandedCategory == action.File.FileCategory)
        {
            // Remove the file from expanded list since it should no longer be visible
            updatedExpandedFiles = state.ExpandedCategoryFiles.Where(f => f.Id != action.File.Id).ToImmutableList();
        }
        
        return state with { Files = updatedFiles, ExpandedCategoryFiles = updatedExpandedFiles };
    }

    // ML Model Reducers
    [ReducerMethod]
    public static FileState ReduceTrainModelAction(FileState state, TrainModelAction action) =>
        state with { IsTraining = true, Error = null };

    [ReducerMethod]
    public static FileState ReduceTrainModelSuccessAction(FileState state, TrainModelSuccessAction action)
    {
        var formattedMessage = FormatTrainModelMessage(action.Message);
        
        // Only reset IsTraining if this is an actual completion, not just a queue message
        var isActualCompletion = IsTrainingActualCompletion(action.Message);
        
        return state with 
        { 
            IsTraining = isActualCompletion ? false : state.IsTraining, // Keep current state if just queued
            ConsoleMessages = state.ConsoleMessages.Add(formattedMessage) 
        };
    }
    
    private static bool IsTrainingActualCompletion(string message)
    {
        // Check if this is actual training completion vs just queue response
        if (message.Contains("queued") || message.Contains("In Progress"))
            return false;
            
        // Check for completion indicators
        return message.Contains("completed") || 
               message.Contains("failed") || 
               message.Contains("cancelled") ||
               (message.Contains("trainingDuration") && !message.Contains("00:00:00"));
    }

    private static string FormatTrainModelMessage(string jsonMessage)
    {
        try
        {
            // Parse the JSON response to extract key information
            var json = System.Text.Json.JsonDocument.Parse(jsonMessage);
            var root = json.RootElement;
            
            var success = root.GetProperty("success").GetBoolean();
            var message = root.GetProperty("message").GetString() ?? "Training completed";
            var trainingDuration = root.TryGetProperty("trainingDuration", out var durationProp) ? durationProp.GetString() : "Unknown";
            var modelVersion = root.TryGetProperty("modelVersion", out var versionProp) ? versionProp.GetString() : "Unknown";
            
            // Check if this is a queued job (TrainingDuration = 00:00:00) vs completed job
            var isQueued = trainingDuration == "00:00:00" || message.Contains("queued");
            
            string status;
            if (!success)
            {
                status = "Error";
            }
            else if (isQueued)
            {
                status = "In Progress";
                // Modify message for queued jobs to remove duration and version info
                return $"{DateTime.Now:G} - {status} - Model training job has been queued and will execute in background";
            }
            else
            {
                status = "Success";
            }
            
            return $"{DateTime.Now:G} - {status} - {message}. Training Duration: {trainingDuration} - Model Version: {modelVersion}";
        }
        catch
        {
            // Fallback to original message if JSON parsing fails
            return $"{DateTime.Now:G} - {jsonMessage}";
        }
    }

    private static string FormatJobCompletionMessage(string message)
    {
        // DEBUG: Log all incoming messages to trace TrainModel completion
        Console.WriteLine($"🔍 DEBUG: FormatJobCompletionMessage received: {message}");
        
        // Try to detect if this is a JSON message from TrainModel or ForceCategorize
        if (message.TrimStart().StartsWith("{") && message.TrimEnd().EndsWith("}"))
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(message);
                var root = json.RootElement;
                
                // Check if this is a TrainModel completion message
                if (root.TryGetProperty("trainingDuration", out _) && root.TryGetProperty("modelVersion", out _))
                {
                    Console.WriteLine($"🎯 DEBUG: TrainModel completion message detected, formatting...");
                    return FormatTrainModelMessage(message);
                }
                
                // Check if this is a ForceCategorize completion message
                if (root.TryGetProperty("totalFiles", out _) && root.TryGetProperty("categorizedFiles", out _))
                {
                    return FormatForceCategoryMessage(message);
                }
                
                // Generic JSON job completion
                if (root.TryGetProperty("success", out var successProp) && root.TryGetProperty("message", out var messageProp))
                {
                    var success = successProp.GetBoolean();
                    var jobMessage = messageProp.GetString() ?? "Job completed";
                    var status = success ? "Success" : "Error";
                    
                    return $"{DateTime.Now:G} - {status} - {jobMessage}";
                }
            }
            catch
            {
                // Fall through to simple message formatting
            }
        }
        
        // Simple message - just add timestamp
        return $"{DateTime.Now:G} - {message}";
    }

    private static string FormatForceCategoryMessage(string jsonMessage)
    {
        try
        {
            // Parse the JSON response to extract job information
            var json = System.Text.Json.JsonDocument.Parse(jsonMessage);
            var root = json.RootElement;
            
            // Check if this is a job completion message (has totalFiles/categorizedFiles)
            if (root.TryGetProperty("totalFiles", out var totalFilesProp) && root.TryGetProperty("categorizedFiles", out var categorizedFilesProp))
            {
                var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                var message = root.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : "Force categorization completed";
                var totalFiles = totalFilesProp.GetInt32();
                var categorizedFiles = categorizedFilesProp.GetInt32();
                var errorFiles = root.TryGetProperty("errorFiles", out var errorProp) ? errorProp.GetInt32() : 0;
                var duration = root.TryGetProperty("duration", out var durationProp) ? durationProp.GetString() : "Unknown";
                
                var status = success ? "Success" : "Error";
                return $"{DateTime.Now:G} - {status} - {message}. Processed: {totalFiles} files, Success: {categorizedFiles}, Errors: {errorFiles}, Duration: {duration}";
            }
            
            // Fallback: try to extract jobId and status (for API response messages)
            var jobId = root.TryGetProperty("jobId", out var jobProp) ? jobProp.GetString() : "Unknown";
            var jobStatus = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "Running";
            
            return $"{DateTime.Now:G} - JobId: {jobId} - Status: {jobStatus}";
        }
        catch
        {
            // Fallback to original message if JSON parsing fails
            return $"{DateTime.Now:G} - {jsonMessage}";
        }
    }

    [ReducerMethod]
    public static FileState ReduceTrainModelFailureAction(FileState state, TrainModelFailureAction action) =>
        state with { IsTraining = false, Error = action.Error, ConsoleMessages = state.ConsoleMessages.Add($"{DateTime.Now:G} - ERROR: {action.Error}") };

    [ReducerMethod]
    public static FileState ReduceForceCategoryAction(FileState state, ForceCategoryAction action) =>
        state with { IsCategorizing = true, Error = null };

    [ReducerMethod]
    public static FileState ReduceForceCategorySuccessAction(FileState state, ForceCategorySuccessAction action) =>
        state with { IsCategorizing = false, ConsoleMessages = state.ConsoleMessages.Add(FormatForceCategoryMessage(action.Message)) };

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
    public static FileState ReduceSignalRJobCompletedAction(FileState state, SignalRJobCompletedAction action)
    {
        // DEBUG: Check if SignalR messages reach reducer
        Console.WriteLine($"🔄 DEBUG: Reducer received SignalR message: {action.ResultText}");
        
        var formattedMessage = FormatJobCompletionMessage(action.ResultText);
        var newState = state with { ConsoleMessages = state.ConsoleMessages.Add(formattedMessage) };
        
        // Reset operation states when jobs are ACTUALLY completed (not just queued)
        if (IsActualJobCompletionMessage(action.ResultText))
        {
            // Check if this is a training completion (has actual completion indicators)
            if (action.ResultText.Contains("trainingDuration") || 
                (action.ResultText.Contains("training") && (action.ResultText.Contains("completed") || action.ResultText.Contains("failed"))))
            {
                newState = newState with { IsTraining = false };
            }
            
            // Check if this is a force categorization completion  
            if (action.ResultText.Contains("totalFiles") || 
                (action.ResultText.Contains("categorization") && (action.ResultText.Contains("completed") || action.ResultText.Contains("failed"))))
            {
                newState = newState with { IsCategorizing = false };
            }
        }
        
        return newState;
    }
    
    private static bool IsActualJobCompletionMessage(string message)
    {
        // Check if this is an ACTUAL job completion (not just queue/progress)
        // Must be either a JSON completion message with specific completion fields,
        // or contain explicit completion/failure/cancellation words
        
        if (message.TrimStart().StartsWith("{") && message.TrimEnd().EndsWith("}"))
        {
            // JSON messages with completion-specific fields (trainingDuration, totalFiles, etc.)
            return message.Contains("trainingDuration") || 
                   message.Contains("totalFiles") ||
                   message.Contains("completedAt");
        }
        
        // Text messages with explicit completion status
        return message.Contains("completed") ||
               message.Contains("failed") ||
               message.Contains("cancelled");
    }

    // Cache Reducers
    [ReducerMethod]
    public static FileState ReduceCacheWarmupAction(FileState state, CacheWarmupAction action) =>
        state with { IsCacheWarming = true };

    [ReducerMethod]
    public static FileState ReduceCacheWarmupSuccessAction(FileState state, CacheWarmupSuccessAction action) =>
        state with { IsCacheWarming = false, LastCacheUpdate = DateTime.UtcNow };

    [ReducerMethod]
    public static FileState ReduceCacheWarmupFailureAction(FileState state, CacheWarmupFailureAction action) =>
        state with { IsCacheWarming = false };

    [ReducerMethod]
    public static FileState ReduceCacheClearAction(FileState state, CacheClearAction action) =>
        state; // Silently start cache clear

    [ReducerMethod]
    public static FileState ReduceCacheClearSuccessAction(FileState state, CacheClearSuccessAction action) =>
        state with { CacheStatistics = null, LastCacheUpdate = DateTime.UtcNow };

    [ReducerMethod]
    public static FileState ReduceCacheInvalidateSuccessAction(FileState state, CacheInvalidateSuccessAction action) =>
        state with { LastCacheUpdate = DateTime.UtcNow };

    [ReducerMethod]
    public static FileState ReduceCacheStatsUpdateAction(FileState state, CacheStatsUpdateAction action) =>
        state with { CacheStatistics = action.Statistics, LastCacheUpdate = DateTime.UtcNow };

    [ReducerMethod]
    public static FileState ReduceCacheHitAction(FileState state, CacheHitAction action) =>
        state; // Silently track cache hits without console output

    [ReducerMethod]
    public static FileState ReduceCacheMissAction(FileState state, CacheMissAction action) =>
        state; // Silently track cache misses without console output

    private static string FormatRefreshMessage(string jsonMessage)
    {
        try
        {
            // Try to parse as JSON first
            if (jsonMessage.TrimStart().StartsWith("{") && jsonMessage.TrimEnd().EndsWith("}"))
            {
                var json = System.Text.Json.JsonDocument.Parse(jsonMessage);
                var root = json.RootElement;
                
                // Check if this is a refresh job status message (queue response)
                if (root.TryGetProperty("jobId", out var jobIdProp) && 
                    root.TryGetProperty("status", out var statusProp))
                {
                    var jobId = jobIdProp.GetString() ?? "Unknown";
                    var status = statusProp.GetString() ?? "Unknown";
                    
                    // Check if it's a queued job (like train model formatting)
                    if (status.Equals("Queued", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{DateTime.Now:G} - In Progress - Refresh categories job has been queued and will execute in background";
                    }
                    
                    // Other status formats
                    return $"{DateTime.Now:G} - JobId: {jobId} - Status: {status}";
                }
                
                // Check if this is a generic success/message JSON
                if (root.TryGetProperty("success", out var successProp) && 
                    root.TryGetProperty("message", out var messageProp))
                {
                    var success = successProp.GetBoolean();
                    var message = messageProp.GetString() ?? "Refresh completed";
                    var status = success ? "Success" : "Error";
                    
                    return $"{DateTime.Now:G} - {status} - {message}";
                }
            }
        }
        catch
        {
            // Fall through to simple message formatting
        }
        
        // Simple message - just add timestamp
        return $"{DateTime.Now:G} - {jsonMessage}";
    }
}