using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.FileManagement;

namespace FileCategorization_App.Components.Interface
{
    /// <summary>
    /// Interface for File Management API operations with Result Pattern support
    /// </summary>
    public interface IServiceApi
    {
        /// <summary>
        /// Gets list of files to be processed/moved
        /// </summary>
        Task<Result<List<FilesDetailDto>>> GetFilesAsync();

        /// <summary>
        /// Refreshes file categorization from origin directory
        /// </summary>
        Task<Result<string>> RefreshCategoryAsync();


        /// <summary>
        /// Gets list of available categories
        /// </summary>
        Task<Result<List<string>>> GetCategoriesAsync();

        /// <summary>
        /// Moves single file to specified category
        /// </summary>
        Task<Result<string>> MoveFileAsync(FilesDetailDto fileDetail);

        /// <summary>
        /// Trains the machine learning model
        /// </summary>
        Task<Result<string>> TrainModelAsync();

        /// <summary>
        /// Gets recently viewed/processed files
        /// </summary>
        Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync();

        /// <summary>
        /// Gets all files filtered by category
        /// </summary>
        Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory);

        /// <summary>
        /// Marks file as "not to show again" (uses v2 API)
        /// </summary>
        Task<Result<FilesDetailDto>> SetFileNotShowAgainAsync(int fileId);

        /// <summary>
        /// Moves multiple files in batch operation
        /// </summary>
        Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove);

        #region Legacy Sync Methods (for backward compatibility)
        
        /// <summary>
        /// Legacy sync method for GetCategories
        /// </summary>
        Task<List<string>> GetCategories();
        
        /// <summary>
        /// Legacy sync method for GetFiles
        /// </summary>
        Task<List<FilesDetailDto>> GetFiles();
        
        
        /// <summary>
        /// Legacy sync method for GetLastFilesList
        /// </summary>
        Task<List<FilesDetailDto>> GetLastFilesList();
        
        /// <summary>
        /// Legacy sync method for GetAllFiles
        /// </summary>
        Task<List<FilesDetailDto>> GetAllFiles(string fileCategory);
        
        /// <summary>
        /// Legacy sync method for RefreshCategory
        /// </summary>
        Task<string> RefreshCategory();
        
        /// <summary>
        /// Legacy sync method for MoveFile
        /// </summary>
        Task<string> MoveFile(FilesDetailDto fileDetail);
        
        /// <summary>
        /// Legacy sync method for MoveFiles
        /// </summary>
        Task<string> MoveFiles(List<FilesDetailDto> filesToMove);
        
        
        /// <summary>
        /// Legacy sync method for TrainModel
        /// </summary>
        Task<string> TrainModel();
        
        #endregion
    }
}
