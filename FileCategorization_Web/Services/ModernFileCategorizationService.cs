using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileCategorization_Web.Data.Common;
using FileCategorization_Web.Data.Configuration;
using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;
using FileCategorization_Web.Interfaces;
using Microsoft.Extensions.Options;

namespace FileCategorization_Web.Services;

public class ModernFileCategorizationService : IFileCategorizationService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<ModernFileCategorizationService> _logger;
    private readonly FileCategorizationApiOptions _options;

    public ModernFileCategorizationService(
        HttpClient httpClient,
        IOptions<FileCategorizationApiOptions> options,
        ILogger<ModernFileCategorizationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    #region File Detail

    public async Task<Result<string>> RefreshCategoryAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing category data");
            
            var response = await _httpClient.GetAsync("api/v1/RefreshFiles");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Category data refreshed successfully");
                return Result.Success(result);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to refresh category data: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while refreshing category data");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<string>>> GetCategoryListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching category list");
            
            var response = await _httpClient.GetAsync("api/v1/CategoryList");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<string>>(content, _serializerOptions) ?? new List<string>();
                
                _logger.LogInformation("Retrieved {Count} categories", categories.Count);
                return Result.Success(categories);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get category list: {Error}", error);
            return Result.Failure<List<string>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching category list");
            return Result.Failure<List<string>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileListAsync(int searchPar)
    {
        try
        {
            _logger.LogInformation("Fetching file list with search parameter: {SearchPar}", searchPar);
            
            var response = await _httpClient.GetAsync($"api/v1/GetFileList/{searchPar}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files", files.Count);
                return Result.Success(files);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get file list: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching file list");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item)
    {
        try
        {
            _logger.LogInformation("Updating file detail for ID: {Id}", item.Id);
            
            var response = await _httpClient.PutAsJsonAsync($"api/v1/UpdateFilesDetail/{item.Id}", item);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedFile = JsonSerializer.Deserialize<FilesDetailDto>(content, _serializerOptions);
                
                if (updatedFile != null)
                {
                    _logger.LogInformation("File detail updated successfully for ID: {Id}", item.Id);
                    return Result.Success(updatedFile);
                }
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to update file detail: {Error}", error);
            return Result.Failure<FilesDetailDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating file detail");
            return Result.Failure<FilesDetailDto>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Config

    public async Task<Result<List<ConfigsDto>>> GetConfigListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching config list");
            
            var response = await _httpClient.GetAsync("api/v1/GetConfigList");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var configs = JsonSerializer.Deserialize<List<ConfigsDto>>(content, _serializerOptions) ?? new List<ConfigsDto>();
                
                _logger.LogInformation("Retrieved {Count} configs", configs.Count);
                return Result.Success(configs);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get config list: {Error}", error);
            return Result.Failure<List<ConfigsDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching config list");
            return Result.Failure<List<ConfigsDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> GetConfigAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching config with ID: {Id}", id);
            
            var response = await _httpClient.GetAsync($"api/v1/GetConfig/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (config != null)
                {
                    _logger.LogInformation("Config retrieved successfully for ID: {Id}", id);
                    return Result.Success(config);
                }
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get config: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching config");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> UpdateConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Updating config");
            
            var response = await _httpClient.PutAsJsonAsync("api/v1/UpdateConfig", item);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedConfig = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (updatedConfig != null)
                {
                    _logger.LogInformation("Config updated successfully");
                    return Result.Success(updatedConfig);
                }
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to update config: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating config");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> AddConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Adding new config");
            
            var response = await _httpClient.PostAsJsonAsync("api/v1/AddConfig", item);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var addedConfig = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (addedConfig != null)
                {
                    _logger.LogInformation("Config added successfully");
                    return Result.Success(addedConfig);
                }
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to add config: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while adding config");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> DeleteConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Deleting config");
            
            var response = await _httpClient.PutAsJsonAsync("api/v1/DeleteConfig", item);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var deletedConfig = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (deletedConfig != null)
                {
                    _logger.LogInformation("Config deleted successfully");
                    return Result.Success(deletedConfig);
                }
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to delete config: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting config");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Last View

    public async Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching last files list");
            
            var response = await _httpClient.GetAsync("api/v1/GetLastViewList");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} last files", files.Count);
                return Result.Success(files);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get last files list: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching last files list");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory)
    {
        try
        {
            _logger.LogInformation("Fetching all files for category: {Category}", fileCategory);
            
            var response = await _httpClient.GetAsync($"api/v1/GetAllFiles/{fileCategory}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files for category {Category}", files.Count, fileCategory);
                return Result.Success(files);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get files for category {Category}: {Error}", fileCategory, error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching files for category {Category}", fileCategory);
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Service

    public async Task<Result<string>> TrainModelAsync()
    {
        try
        {
            _logger.LogInformation("Starting model training");
            
            var response = await _httpClient.GetAsync("api/v1/TrainModel");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Model training completed successfully");
                return Result.Success(result);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to train model: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while training model");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> ForceCategoryAsync()
    {
        try
        {
            _logger.LogInformation("Starting force categorization");
            
            var response = await _httpClient.GetAsync("api/v1/ForceCategory");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Force categorization completed successfully");
                return Result.Success(result);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to force categorization: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while forcing categorization");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileToMoveAsync()
    {
        try
        {
            _logger.LogInformation("Fetching files to move");
            
            var response = await _httpClient.GetAsync("api/v1/GetFileToMove");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files to move", files.Count);
                return Result.Success(files);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get files to move: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching files to move");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> MoveFileAsync(int id, string category)
    {
        try
        {
            _logger.LogInformation("Moving file {Id} to category {Category}", id, category);
            
            var response = await _httpClient.GetAsync($"api/v1/MoveFile/{id}/{category}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("File {Id} moved successfully to category {Category}", id, category);
                return Result.Success(result);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to move file {Id}: {Error}", id, error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while moving file {Id}", id);
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove)
    {
        try
        {
            _logger.LogInformation("Moving {Count} files", filesToMove.Count);
            
            var fileToMoveDto = filesToMove
                .Select(fileToMove => new FileMoveDto { FileCategory = fileToMove.FileCategory, Id = fileToMove.Id })
                .ToList();

            if (!fileToMoveDto.Any())
            {
                _logger.LogWarning("No files to move");
                return Result.Success("No files to move");
            }

            var response = await _httpClient.PostAsJsonAsync("api/v1/MoveFiles", fileToMoveDto);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Files moved successfully");
                return Result.Success(result);
            }

            var error = $"API Error: {response.StatusCode}";
            _logger.LogWarning("Failed to move files: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while moving files");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion
}