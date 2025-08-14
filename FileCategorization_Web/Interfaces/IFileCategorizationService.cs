using FileCategorization_Web.Data.Common;
using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;

namespace FileCategorization_Web.Interfaces;

public interface IFileCategorizationService
{
    Task<Result<string>> RefreshCategoryAsync();
    Task<Result<List<string>>> GetCategoryListAsync();

    Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync();
    Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory);
    Task<Result<List<FilesDetailDto>>> GetFileToMoveAsync();
    Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item);
    Task<Result<string>> TrainModelAsync();        
    Task<Result<string>> ForceCategoryAsync();
    Task<Result<string>> MoveFileAsync(int id, string category);
    Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove);
    Task<Result<List<ConfigsDto>>> GetConfigListAsync();
    Task<Result<ConfigsDto>> GetConfigAsync(int id);
    Task<Result<ConfigsDto>> UpdateConfigAsync(ConfigsDto item);
    Task<Result<ConfigsDto>> AddConfigAsync(ConfigsDto item);
    Task<Result<ConfigsDto>> DeleteConfigAsync(ConfigsDto item);

    Task<Result<List<FilesDetailDto>>> GetFileListAsync(int searchPar);
}