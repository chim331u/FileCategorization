using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Domain.Entities.FileCategorization;

namespace FileCategorization_Api.Interfaces;

public interface IFilesDetailService
{
    Task<List<string?>> GetDbCategoryList();
    Task<IEnumerable<FilesDetailResponse>> GetFileList();
    Task<FilesDetailResponse?> GetFilesDetailById(int id);
    Task<IEnumerable<FilesDetailResponse>> GetFileListToCategorize();
    Task<FilesDetailResponse?> AddFileDetailAsync(FilesDetailRequest filesDetailRequest);
    Task<int> AddFileDetailList(List<FilesDetail> fileDetail);
    Task<FilesDetailResponse?> UpdateFilesDetailAsync(int id, FilesDetailUpdateRequest filesDetail);
    Task<FilesDetail?> UpdateFilesDetail(FilesDetail filesDetail);
    Task<bool> DeleteFilesDetailAsync(int id);
    
    Task<IEnumerable<FilesDetailResponse?>> GetAllFiles(string fileCategory);
    Task<IEnumerable<FilesDetailResponse?>> GetLastViewList();

    Task<string?> ForceCategory();
    Task<bool> FileNameIsPresent(string fileName);

}