using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Data.Configuration;
using Microsoft.Extensions.Options;

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

    public static string GetRestUrl(this IFileCategorizationService service, IConfiguration configuration)
    {
        // Try to get URL from modern configuration first
        var apiOptions = configuration.GetSection(FileCategorizationApiOptions.SectionName).Get<FileCategorizationApiOptions>();
        if (apiOptions != null && !string.IsNullOrEmpty(apiOptions.BaseUrl))
        {
            return apiOptions.BaseUrl.TrimEnd('/') + "/";
        }

        // Fallback to legacy Uri configuration
        var legacyUri = configuration.GetValue<string>("Uri");
        if (!string.IsNullOrEmpty(legacyUri))
        {
            return legacyUri.TrimEnd('/') + "/";
        }

        // Final fallback to localhost for development
        return "http://localhost:5089/";
    }

    public static string GetRestUrl(this IFileCategorizationService service)
    {
        // All services now implement GetRestUrl() in the interface
        // This extension method is now redundant but kept for compatibility
        return service.GetRestUrl();
    }
}