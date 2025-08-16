using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using FileCategorization_Web.Interfaces;

namespace FileCategorization_Web.Services;

public class LegacyServiceAdapter : IFileCategorizationService
{
    private readonly ILegacyFileCategorizationService _legacyService;

    public LegacyServiceAdapter(ILegacyFileCategorizationService legacyService)
    {
        _legacyService = legacyService;
    }

    public async Task<Result<string>> RefreshCategoryAsync()
    {
        try
        {
            var result = await _legacyService.RefreshCategory();
            return result != null ? Result.Success(result) : Result.Failure<string>("Failed to refresh category");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(ex);
        }
    }

    public async Task<Result<List<string>>> GetCategoryListAsync()
    {
        try
        {
            var result = await _legacyService.GetCategoryList();
            return result != null ? Result.Success(result) : Result.Failure<List<string>>("Failed to get category list");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<string>>(ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync()
    {
        try
        {
            var result = await _legacyService.GetLastFilesList();
            return result != null ? Result.Success(result) : Result.Failure<List<FilesDetailDto>>("Failed to get last files list");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<FilesDetailDto>>(ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory)
    {
        try
        {
            var result = await _legacyService.GetAllFiles(fileCategory);
            return result != null ? Result.Success(result) : Result.Failure<List<FilesDetailDto>>("Failed to get all files");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<FilesDetailDto>>(ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileToMoveAsync()
    {
        try
        {
            var result = await _legacyService.GetFileToMove();
            return result != null ? Result.Success(result) : Result.Failure<List<FilesDetailDto>>("Failed to get files to move");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<FilesDetailDto>>(ex);
        }
    }

    public async Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item)
    {
        try
        {
            var result = await _legacyService.UpdateFileDetail(item);
            return result != null ? Result.Success(result) : Result.Failure<FilesDetailDto>("Failed to update file detail");
        }
        catch (Exception ex)
        {
            return Result.Failure<FilesDetailDto>(ex);
        }
    }

    public async Task<Result<string>> TrainModelAsync()
    {
        try
        {
            var result = await _legacyService.TrainModel();
            return result != null ? Result.Success(result) : Result.Failure<string>("Failed to train model");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(ex);
        }
    }

    public async Task<Result<string>> ForceCategoryAsync()
    {
        try
        {
            var result = await _legacyService.ForceCategory();
            return result != null ? Result.Success(result) : Result.Failure<string>("Failed to force category");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(ex);
        }
    }

    public async Task<Result<string>> MoveFileAsync(int id, string category)
    {
        try
        {
            var result = await _legacyService.MoveFile(id, category);
            return result != null ? Result.Success(result) : Result.Failure<string>("Failed to move file");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(ex);
        }
    }

    public async Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove)
    {
        try
        {
            var result = await _legacyService.MoveFiles(filesToMove);
            return result != null ? Result.Success(result) : Result.Failure<string>("Failed to move files");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(ex);
        }
    }

    public async Task<Result<List<ConfigsDto>>> GetConfigListAsync()
    {
        try
        {
            var result = await _legacyService.GetConfigList();
            return result != null ? Result.Success(result) : Result.Failure<List<ConfigsDto>>("Failed to get config list");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<ConfigsDto>>(ex);
        }
    }

    public async Task<Result<ConfigsDto>> GetConfigAsync(int id)
    {
        try
        {
            var result = await _legacyService.GetConfig(id);
            return result != null ? Result.Success(result) : Result.Failure<ConfigsDto>("Failed to get config");
        }
        catch (Exception ex)
        {
            return Result.Failure<ConfigsDto>(ex);
        }
    }

    public async Task<Result<ConfigsDto>> UpdateConfigAsync(ConfigsDto item)
    {
        try
        {
            var result = await _legacyService.UpdateConfig(item);
            return result != null ? Result.Success(result) : Result.Failure<ConfigsDto>("Failed to update config");
        }
        catch (Exception ex)
        {
            return Result.Failure<ConfigsDto>(ex);
        }
    }

    public async Task<Result<ConfigsDto>> AddConfigAsync(ConfigsDto item)
    {
        try
        {
            var result = await _legacyService.AddConfig(item);
            return result != null ? Result.Success(result) : Result.Failure<ConfigsDto>("Failed to add config");
        }
        catch (Exception ex)
        {
            return Result.Failure<ConfigsDto>(ex);
        }
    }

    public async Task<Result<ConfigsDto>> DeleteConfigAsync(ConfigsDto item)
    {
        try
        {
            var result = await _legacyService.DeleteConfig(item);
            return result != null ? Result.Success(result) : Result.Failure<ConfigsDto>("Failed to delete config");
        }
        catch (Exception ex)
        {
            return Result.Failure<ConfigsDto>(ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileListAsync(int searchPar)
    {
        try
        {
            var result = await _legacyService.GetFileListDto(searchPar);
            return result != null ? Result.Success(result) : Result.Failure<List<FilesDetailDto>>("Failed to get file list");
        }
        catch (Exception ex)
        {
            return Result.Failure<List<FilesDetailDto>>(ex);
        }
    }

    public string GetRestUrl()
    {
        return _legacyService.GetRestUrl();
    }
}