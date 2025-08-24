using System.Collections.Immutable;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;

namespace FileCategorization_Web.Features.FileManagement.Actions;

// Base Action
public abstract record FileAction;

// Loading Actions
public record SetLoadingAction(bool IsLoading) : FileAction;
public record SetRefreshingAction(bool IsRefreshing) : FileAction;
public record SetTrainingAction(bool IsTraining) : FileAction;
public record SetErrorAction(string? Error) : FileAction;

// File Data Actions
public record LoadFilesAction(int SearchParameter) : FileAction;
public record LoadFilesSuccessAction(ImmutableList<FilesDetailDto> Files) : FileAction;
public record LoadFilesFailureAction(string Error) : FileAction;

public record LoadLastViewFilesAction : FileAction;
public record LoadLastViewFilesSuccessAction(ImmutableList<FilesDetailDto> Files) : FileAction;
public record LoadLastViewFilesFailureAction(string Error) : FileAction;

public record LoadFilesByCategoryAction(string Category) : FileAction;
public record LoadFilesByCategorySuccessAction(ImmutableList<FilesDetailDto> Files, string Category) : FileAction;
public record LoadFilesByCategoryFailureAction(string Error) : FileAction;

public record RefreshDataAction : FileAction;
public record RefreshDataSuccessAction(string Message) : FileAction;
public record RefreshDataFailureAction(string Error) : FileAction;

// Category Actions
public record LoadCategoriesAction : FileAction;
public record LoadCategoriesSuccessAction(ImmutableList<string> Categories) : FileAction;
public record LoadCategoriesFailureAction(string Error) : FileAction;

// Configuration Actions  
public record LoadConfigurationsAction : FileAction;
public record LoadConfigurationsSuccessAction(ImmutableList<ConfigsDto> Configurations) : FileAction;
public record LoadConfigurationsFailureAction(string Error) : FileAction;

public record UpdateConfigurationAction(ConfigsDto Configuration) : FileAction;
public record UpdateConfigurationSuccessAction(ConfigsDto UpdatedConfiguration) : FileAction;
public record UpdateConfigurationFailureAction(string Error) : FileAction;

public record CreateConfigurationAction(ConfigsDto Configuration) : FileAction;
public record CreateConfigurationSuccessAction(ConfigsDto CreatedConfiguration) : FileAction;
public record CreateConfigurationFailureAction(string Error) : FileAction;

public record DeleteConfigurationAction(ConfigsDto Configuration) : FileAction;
public record DeleteConfigurationSuccessAction(ConfigsDto DeletedConfiguration) : FileAction;
public record DeleteConfigurationFailureAction(string Error) : FileAction;

// File Management Actions
public record UpdateFileDetailAction(FilesDetailDto File) : FileAction;
public record UpdateFileDetailSuccessAction(FilesDetailDto UpdatedFile) : FileAction;
public record UpdateFileDetailFailureAction(string Error) : FileAction;

public record ScheduleFileAction(int FileId) : FileAction;
public record RevertFileAction(int FileId) : FileAction;
public record NotShowAgainFileAction(FilesDetailDto File) : FileAction;

// ML Actions
public record TrainModelAction : FileAction;
public record TrainModelSuccessAction(string Message) : FileAction;
public record TrainModelFailureAction(string Error) : FileAction;

public record ForceCategoryAction : FileAction;
public record ForceCategorySuccessAction(string Message) : FileAction;
public record ForceCategoryFailureAction(string Error) : FileAction;

// File Movement Actions
public record MoveFilesAction(ImmutableList<FilesDetailDto> FilesToMove) : FileAction;
public record MoveFilesSuccessAction(string JobId) : FileAction;
public record MoveFilesFailureAction(string Error) : FileAction;

// Search and Filter Actions
public record SetSearchParameterAction(int SearchParameter) : FileAction;
public record SetSelectedCategoryAction(string? Category) : FileAction;

// Console Actions
public record AddConsoleMessageAction(string Message) : FileAction;
public record ClearConsoleAction : FileAction;

// Category Management Actions
public record AddNewCategoryAction(string Category) : FileAction;

// SignalR Actions
public record SignalRConnectedAction(string ConnectionId) : FileAction;
public record SignalRDisconnectedAction : FileAction;

public record SignalRFileMovedAction(int FileId, string ResultText, MoveFilesResults Result) : FileAction
{
    // Extended constructor for SignalR service compatibility (ignores extra parameters)
    public SignalRFileMovedAction(int FileId, string FileName, string DestinationPath, string ResultText, MoveFilesResults Result) 
        : this(FileId, ResultText, Result) { }
}

public record SignalRJobCompletedAction(string ResultText, MoveFilesResults Result) : FileAction
{
    // Extended constructor for SignalR service compatibility (ignores extra parameters)  
    public SignalRJobCompletedAction(string ResultText, MoveFilesResults Result, int TotalFiles, int SuccessfulFiles, int FailedFiles)
        : this(ResultText, Result) { }
}