using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileCategorization_Shared.Common;
using FileCategorization_Web.Data.Configuration;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
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
            _logger.LogInformation("Refreshing category data using v2 API");
            
            var response = await _httpClient.PostAsync("api/v2/actions/refresh-files", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Category data refreshed successfully using v2 API");
                return Result.Success(result);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to refresh category data using v2 API: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while refreshing category data using v2 API");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<string>>> GetCategoryListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching category list using v2 API");
            
            var response = await _httpClient.GetAsync("api/v2/files/categories");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<string>>(content, _serializerOptions) ?? new List<string>();
                
                _logger.LogInformation("Retrieved {Count} categories from v2 API", categories.Count);
                return Result.Success(categories);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get category list from v2 API: {Error}", error);
            return Result.Failure<List<string>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching category list from v2 API");
            return Result.Failure<List<string>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileListAsync(int searchPar)
    {
        try
        {
            _logger.LogInformation("Fetching file list with filter type: {FilterType} using v2 API", searchPar);
            
            var response = await _httpClient.GetAsync($"api/v2/files/filtered/{searchPar}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files for filter type: {FilterType} from v2 API", files.Count, searchPar);
                return Result.Success(files);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Invalid filter type provided to v2 API: {FilterType}", searchPar);
                return Result.Failure<List<FilesDetailDto>>($"Invalid filter type: {searchPar}");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get file list from v2 API: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching file list from v2 API");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item)
    {
        try
        {
            _logger.LogInformation("Updating file detail for ID: {Id} using v2 API", item.Id);
            
            // Create v2 update request object matching the API's expected format
            var updateRequest = new
            {
                name = item.Name,
                fileCategory = item.FileCategory,
                fileSize = item.FileSize,
                isToCategorize = item.IsToCategorize,
                isNew = item.IsNew,
                isNotToMove = item.IsNotToMove
            };
            
            var response = await _httpClient.PutAsJsonAsync($"api/v2/files/{item.Id}", updateRequest, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedFile = JsonSerializer.Deserialize<FilesDetailDto>(content, _serializerOptions);
                
                if (updatedFile != null)
                {
                    _logger.LogInformation("File detail updated successfully for ID: {Id} using v2 API", item.Id);
                    return Result.Success(updatedFile);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File with ID {Id} not found in v2 API", item.Id);
                return Result.Failure<FilesDetailDto>($"File with ID {item.Id} not found");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to update file detail using v2 API: {Error}", error);
            return Result.Failure<FilesDetailDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating file detail using v2 API");
            return Result.Failure<FilesDetailDto>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Config

    public async Task<Result<List<ConfigsDto>>> GetConfigListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching config list using v2 API");
            
            var response = await _httpClient.GetAsync("api/v2/configs");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var configs = JsonSerializer.Deserialize<List<ConfigsDto>>(content, _serializerOptions) ?? new List<ConfigsDto>();
                
                _logger.LogInformation("Retrieved {Count} configs from v2 API", configs.Count);
                return Result.Success(configs);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get config list from v2 API: {Error}", error);
            return Result.Failure<List<ConfigsDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching config list from v2 API");
            return Result.Failure<List<ConfigsDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> GetConfigAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching config with ID: {Id} using v2 API", id);
            
            var response = await _httpClient.GetAsync($"api/v2/configs/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (config != null)
                {
                    _logger.LogInformation("Config retrieved successfully for ID: {Id} from v2 API", id);
                    return Result.Success(config);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Config with ID {Id} not found in v2 API", id);
                return Result.Failure<ConfigsDto>($"Config with ID {id} not found");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get config from v2 API: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching config from v2 API");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> UpdateConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Updating config ID: {Id} using v2 API", item.Id);
            
            // Create v2 update request object
            var updateRequest = new
            {
                key = item.Key,
                value = item.Value,
                isDev = false // Default to production, can be enhanced later
            };
            
            var response = await _httpClient.PutAsJsonAsync($"api/v2/configs/{item.Id}", updateRequest, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedConfig = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (updatedConfig != null)
                {
                    _logger.LogInformation("Config updated successfully using v2 API");
                    return Result.Success(updatedConfig);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Config with ID {Id} not found for update in v2 API", item.Id);
                return Result.Failure<ConfigsDto>($"Config with ID {item.Id} not found");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Config key conflict during update in v2 API");
                return Result.Failure<ConfigsDto>("Configuration key already exists");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to update config using v2 API: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating config using v2 API");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> AddConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Adding new config using v2 API");
            
            // Create v2 create request object
            var createRequest = new
            {
                key = item.Key,
                value = item.Value,
                isDev = false // Default to production, can be enhanced later
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/v2/configs", createRequest, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var addedConfig = JsonSerializer.Deserialize<ConfigsDto>(content, _serializerOptions);
                
                if (addedConfig != null)
                {
                    _logger.LogInformation("Config added successfully using v2 API");
                    return Result.Success(addedConfig);
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Config key conflict during creation in v2 API");
                return Result.Failure<ConfigsDto>("Configuration key already exists");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to add config using v2 API: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while adding config using v2 API");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<ConfigsDto>> DeleteConfigAsync(ConfigsDto item)
    {
        try
        {
            _logger.LogInformation("Deleting config ID: {Id} using v2 API", item.Id);
            
            var response = await _httpClient.DeleteAsync($"api/v2/configs/{item.Id}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Config deleted successfully using v2 API");
                // v2 DELETE returns 204 No Content, so we return the original item
                return Result.Success(item);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Config with ID {Id} not found for deletion in v2 API", item.Id);
                return Result.Failure<ConfigsDto>($"Config with ID {item.Id} not found");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to delete config using v2 API: {Error}", error);
            return Result.Failure<ConfigsDto>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting config using v2 API");
            return Result.Failure<ConfigsDto>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Last View

    public async Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync()
    {
        try
        {
            _logger.LogInformation("Fetching last files list using v2 API");
            
            var response = await _httpClient.GetAsync("api/v2/files/lastview");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} last files from v2 API", files.Count);
                return Result.Success(files);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get last files list from v2 API: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching last files list from v2 API");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory)
    {
        try
        {
            _logger.LogInformation("Fetching all files for category: {Category} using v2 API", fileCategory);
            
            var response = await _httpClient.GetAsync($"api/v2/files/category/{fileCategory}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files for category {Category} from v2 API", files.Count, fileCategory);
                return Result.Success(files);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Invalid category provided to v2 API: {Category}", fileCategory);
                return Result.Failure<List<FilesDetailDto>>($"Invalid category: {fileCategory}");
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get files for category {Category} from v2 API: {Error}", fileCategory, error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching files for category {Category} from v2 API", fileCategory);
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    #region Service

    public async Task<Result<string>> TrainModelAsync()
    {
        try
        {
            _logger.LogInformation("Starting model training using v2 API");
            
            var response = await _httpClient.PostAsync("api/v2/actions/train-model", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Model training completed successfully using v2 API");
                return Result.Success(result);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to train model using v2 API: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while training model using v2 API");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> ForceCategoryAsync()
    {
        try
        {
            _logger.LogInformation("Starting force categorization using v2 API");
            
            var response = await _httpClient.PostAsync("api/v2/actions/force-categorize", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Force categorization completed successfully using v2 API");
                return Result.Success(result);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to force categorization using v2 API: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while forcing categorization using v2 API");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<List<FilesDetailDto>>> GetFileToMoveAsync()
    {
        try
        {
            _logger.LogInformation("Fetching files to move using v2 API");
            
            var response = await _httpClient.GetAsync("api/v2/files/tocategorize");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions) ?? new List<FilesDetailDto>();
                
                _logger.LogInformation("Retrieved {Count} files to move from v2 API", files.Count);
                return Result.Success(files);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get files to move from v2 API: {Error}", error);
            return Result.Failure<List<FilesDetailDto>>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching files to move from v2 API");
            return Result.Failure<List<FilesDetailDto>>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> MoveFileAsync(int id, string category)
    {
        try
        {
            _logger.LogInformation("Moving file {Id} to category {Category} using v2 batch API", id, category);
            
            // Create batch request for single file (v2 uses batch operations)
            var moveRequest = new
            {
                files = new[] 
                {
                    new { id = id, fileCategory = category }
                }
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/v2/actions/move-files", moveRequest, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("File {Id} moved successfully to category {Category} using v2 API", id, category);
                return Result.Success(result);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to move file {Id} using v2 API: {Error}", id, error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while moving file {Id} using v2 API", id);
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    public async Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove)
    {
        try
        {
            _logger.LogInformation("Moving {Count} files using v2 batch API", filesToMove.Count);
            
            if (!filesToMove.Any())
            {
                _logger.LogWarning("No files to move");
                return Result.Success("No files to move");
            }

            // Create v2 batch request object
            var moveRequest = new
            {
                files = filesToMove.Select(file => new 
                { 
                    id = file.Id, 
                    fileCategory = file.FileCategory 
                }).ToArray()
            };

            var response = await _httpClient.PostAsJsonAsync("api/v2/actions/move-files", moveRequest, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Files moved successfully using v2 API");
                return Result.Success(result);
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to move files using v2 API: {Error}", error);
            return Result.Failure<string>(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while moving files using v2 API");
            return Result.Failure<string>($"Network Error: {ex.Message}", ex);
        }
    }

    #endregion

    public string GetRestUrl()
    {
        // Return the base URL from the configured options
        return _options.BaseUrl.TrimEnd('/') + "/";
    }
}