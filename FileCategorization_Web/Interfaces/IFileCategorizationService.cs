using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;

namespace FileCategorization_Web.Interfaces;

public interface IFileCategorizationService
{
    Task<string> RefreshCategory();
    Task<List<string>> GetCategoryList();

    Task<List<FilesDetailDto>> GetLastFilesList();
    Task<List<FilesDetailDto>> GetAllFiles(string fileCategory);
    Task<List<FilesDetailDto>> GetFileToMove();
    Task<FilesDetailDto> UpdateFileDetail(FilesDetailDto item);
    Task<string> TrainModel();        
    Task<string> ForceCategory();
    Task<string> MoveFile(int id, string category);
    Task<string> MoveFiles(List<FilesDetailDto> filesToMove);
    Task<List<ConfigsDto>> GetConfigList();
    Task<ConfigsDto> GetConfig(int id);
    Task<ConfigsDto> UpdateConfig(ConfigsDto item);
    Task<ConfigsDto> AddConfig(ConfigsDto item);
    Task<ConfigsDto> DeleteConfig(ConfigsDto item);
    string GetRestUrl();

    //v1
    Task<List<FilesDetailDto>> GetFileListDto(int searchPar);
}