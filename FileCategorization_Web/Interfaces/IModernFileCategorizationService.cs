using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Shared.DTOs.Configuration;

namespace FileCategorization_Web.Interfaces;

public interface IModernFileCategorizationService
{
    Task<Result<List<FilesDetailDto>>> GetFileListAsync(int searchPar);
    Task<Result<List<string>>> GetCategoryListAsync();
    Task<Result<string>> RefreshCategoryAsync();
    Task<Result<List<ConfigsDto>>> GetConfigListAsync();
    Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item);
    Task<Result<ConfigsDto>> UpdateConfigAsync(ConfigsDto config);
    Task<Result<ConfigsDto>> AddConfigAsync(ConfigsDto config);
    Task<Result<ConfigsDto>> DeleteConfigAsync(ConfigsDto config);
    Task<Result<ConfigsDto>> GetConfigAsync(int id);
    Task<Result<string>> TrainModelAsync();
    Task<Result<string>> ForceCategoryAsync();
    Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove);
    Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync();
    Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory);
    Task<Result<List<FilesDetailDto>>> GetFileToMoveAsync();
    Task<Result<string>> MoveFileAsync(int id, string category);

}