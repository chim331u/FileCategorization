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
        /// Gets specific file details by ID
        /// </summary>
        Task<Result<FilesDetailDto>> GetFileAsync(int id);

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
        /// Updates file details
        /// </summary>
        Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item);

        /// <summary>
        /// Moves multiple files in batch operation
        /// </summary>
        Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove);
        
        #region Legacy Methods (for backward compatibility during migration)
        [Obsolete("Use GetFilesAsync instead. This method will be removed in Phase 2")]
        Task<List<FilesDetailDto>> GetFiles();
        
        [Obsolete("Use RefreshCategoryAsync instead. This method will be removed in Phase 2")]
        Task<string> RefreshCategory();
        
        [Obsolete("Use GetFileAsync instead. This method will be removed in Phase 2")]
        Task<FilesDetailDto> GetFile(int id);
        
        [Obsolete("Use GetCategoriesAsync instead. This method will be removed in Phase 2")]
        Task<List<string>> GetCategories();
        
        [Obsolete("Use MoveFileAsync instead. This method will be removed in Phase 2")]
        Task<string> MoveFile(FilesDetailDto fileDetail);
        
        [Obsolete("Use TrainModelAsync instead. This method will be removed in Phase 2")]
        Task<string> TrainModel();
        
        [Obsolete("Use GetLastFilesListAsync instead. This method will be removed in Phase 2")]
        Task<List<FilesDetailDto>> GetLastFilesList();
        
        [Obsolete("Use GetAllFilesAsync instead. This method will be removed in Phase 2")]
        Task<List<FilesDetailDto>> GetAllFiles(string fileCategory);
        
        [Obsolete("Use UpdateFileDetailAsync instead. This method will be removed in Phase 2")]
        Task<FilesDetailDto> UpdateFileDetail(FilesDetailDto item);
        
        [Obsolete("Use MoveFilesAsync instead. This method will be removed in Phase 2")]
        Task<string> MoveFiles(List<FilesDetailDto> filesToMove);
        #endregion
    }
}
