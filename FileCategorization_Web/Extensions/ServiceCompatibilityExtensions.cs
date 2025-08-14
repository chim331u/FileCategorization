using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;
using FileCategorization_Web.Interfaces;

namespace FileCategorization_Web.Extensions;

public static class ServiceCompatibilityExtensions
{
    public static async Task<string?> RefreshCategory(this IFileCategorizationService service)
    {
        var result = await service.RefreshCategoryAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<List<string>?> GetCategoryList(this IFileCategorizationService service)
    {
        var result = await service.GetCategoryListAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<List<FilesDetailDto>?> GetFileListDto(this IFileCategorizationService service, int searchPar)
    {
        var result = await service.GetFileListAsync(searchPar);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<FilesDetailDto?> UpdateFileDetail(this IFileCategorizationService service, FilesDetailDto item)
    {
        var result = await service.UpdateFileDetailAsync(item);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<string?> TrainModel(this IFileCategorizationService service)
    {
        var result = await service.TrainModelAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<string?> ForceCategory(this IFileCategorizationService service)
    {
        var result = await service.ForceCategoryAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<string?> MoveFiles(this IFileCategorizationService service, List<FilesDetailDto> filesToMove)
    {
        var result = await service.MoveFilesAsync(filesToMove);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<List<ConfigsDto>?> GetConfigList(this IFileCategorizationService service)
    {
        var result = await service.GetConfigListAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<ConfigsDto?> GetConfig(this IFileCategorizationService service, int id)
    {
        var result = await service.GetConfigAsync(id);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<ConfigsDto?> UpdateConfig(this IFileCategorizationService service, ConfigsDto item)
    {
        var result = await service.UpdateConfigAsync(item);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<ConfigsDto?> AddConfig(this IFileCategorizationService service, ConfigsDto item)
    {
        var result = await service.AddConfigAsync(item);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<ConfigsDto?> DeleteConfig(this IFileCategorizationService service, ConfigsDto item)
    {
        var result = await service.DeleteConfigAsync(item);
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<List<FilesDetailDto>?> GetLastFilesList(this IFileCategorizationService service)
    {
        var result = await service.GetLastFilesListAsync();
        return result.IsSuccess ? result.Value : null;
    }

    public static async Task<List<FilesDetailDto>?> GetAllFiles(this IFileCategorizationService service, string fileCategory)
    {
        var result = await service.GetAllFilesAsync(fileCategory);
        return result.IsSuccess ? result.Value : null;
    }

    public static string GetRestUrl(this IFileCategorizationService service)
    {
        // For the modern service, we'll need to get this from configuration
        // For now, return a placeholder that will work with existing infrastructure
        return "http://192.168.1.5:30119/";
    }
}