using System.Net.Http.Json;
using System.Text.Json;
using FileCategorization_App.Components.Interface;
using FileCategorization_App.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_App.Data.DTOs.Actions;
using System.Text;

namespace FileCategorization_App.Components.Service
{
    public class ServiceApi : BaseApiService, IServiceApi
    {
        IHttpsClientHandlerService _httpsClientHandlerService;
        private readonly IConfiguration _config;
        private readonly IUtilityServices _utilityServices;

        public ServiceApi(IHttpsClientHandlerService service, IConfiguration config, IUtilityServices utilityServices, ILogger<ServiceApi> logger) 
            : base(CreateHttpClient(service), logger)
        {
            _httpsClientHandlerService = service;
            _config = config;
            _utilityServices = utilityServices;

            // Set base address for HttpClient
            if (_client.BaseAddress == null)
            {
                _client.BaseAddress = new Uri(_utilityServices.ApiUrl);
            }
        }

        private static HttpClient CreateHttpClient(IHttpsClientHandlerService service)
        {
#if DEBUG
            HttpMessageHandler handler = service.GetPlatformMessageHandler();
            return handler != null ? new HttpClient(handler) : new HttpClient();
#else
            return new HttpClient();
#endif
        }

        public async Task<List<FilesDetailDto>> GetFiles()
        {
            _logger.LogInformation($"Request files to categorize (GetFiles) - Using v2 API");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/files/filtered/3", string.Empty));

            var dataResponse = new List<FilesDetailDto>();

            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    dataResponse = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions);
                    _logger.LogInformation($"{response.StatusCode.ToString()} - Files to categorize received (v2)");
                }
                else
                {
                    _logger.LogWarning($"{response.StatusCode.ToString()} - Files to categorize NOT received (v2)");
                }

                return dataResponse ?? new List<FilesDetailDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetFiles v2 error: {ex.Message} - {ex.InnerException}");
                return new List<FilesDetailDto>();
            }


        }

        public async Task<string> RefreshCategory()
        {
            _logger.LogInformation($"Request refresh categories (RefreshCategory) - Using v2 API");

            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/actions/refresh-files", string.Empty));

            var request = new RefreshFilesRequest
            {
                BatchSize = 100,
                ForceRecategorization = false,
                FileExtensionFilters = null // Process all files
            };

            try
            {
                HttpResponseMessage response = await _client.PostAsJsonAsync(uri, request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jobResponse = JsonSerializer.Deserialize<ActionJobResponse>(content, _serializerOptions);
                    
                    _logger.LogInformation($"{response.StatusCode.ToString()} - Refresh job started: {jobResponse?.JobId}");
                    return $"Refresh job started: {jobResponse?.JobId}";
                }
                _logger.LogWarning($"{response.StatusCode.ToString()} - Refresh job NOT started");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");
                return null;
            }
        }

        public async Task<FilesDetailDto> GetFile(int id)
        {
            _logger.LogInformation($"Request Get File by Id (GetFile(id))");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/GetFilesDetail/{id}", string.Empty));

            try
            {
                var fileDetail = await _client.GetFromJsonAsync<FilesDetailDto>(uri);

                if (fileDetail != null)
                {
                    _logger.LogInformation($"Received File {fileDetail.Name}");
                }
                else
                {
                    _logger.LogWarning($"Received File: null");
                }

                return fileDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }




        }

        public async Task<List<string>> GetCategories()
        {
            _logger.LogInformation($"Request categories List (GetCategories) - Using v2 API");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/files/categories", string.Empty));

            var dataResponse = new List<string>();

            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    dataResponse = JsonSerializer.Deserialize<List<string>>(content, _serializerOptions);
                    _logger.LogInformation($"{response.StatusCode.ToString()} - Categories received");
                }
                else
                {
                    _logger.LogWarning($"{response.StatusCode.ToString()} - Categories NOT received");
                }
                return dataResponse;

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }



        }

        public async Task<string> MoveFile(FilesDetailDto fileDetail)
        {
            _logger.LogInformation($"Request to move single file (MoveFile) - Using v2 API as batch");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/actions/move-files", string.Empty));

            try
            {
                var request = new MoveFilesRequest
                {
                    FilesToMove = new List<FileMoveDto>
                    {
                        new FileMoveDto { Id = fileDetail.Id, FileCategory = fileDetail.FileCategory }
                    },
                    ContinueOnError = false,
                    ValidateCategories = true,
                    CreateDirectories = true
                };

                HttpResponseMessage response = await _client.PostAsJsonAsync(uri, request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jobResponse = JsonSerializer.Deserialize<ActionJobResponse>(content, _serializerOptions);
                    
                    _logger.LogInformation($"{response.StatusCode.ToString()} - File move job started: {jobResponse?.JobId}");
                    return $"File {fileDetail.Name} move job started: {jobResponse?.JobId}";
                }
                _logger.LogWarning($"{response.StatusCode.ToString()} - File NOT moved");
                return null;

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");
                return null;
            }

        }

        public async Task<string> MoveFiles(List<FilesDetailDto> filesToMove)
        {
            _logger.LogInformation($"Request move files batch - Using v2 API");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/actions/move-files", string.Empty));

            try
            {
                var fileMoveList = new List<FileMoveDto>();

                foreach (var item in filesToMove)
                {
                    fileMoveList.Add(new FileMoveDto { Id = item.Id, FileCategory = item.FileCategory });
                }

                if (fileMoveList == null || fileMoveList.Count == 0)
                {
                    _logger.LogWarning($"No files to move");
                    return "No files to move";
                }

                var request = new MoveFilesRequest
                {
                    FilesToMove = fileMoveList,
                    ContinueOnError = true,
                    ValidateCategories = true,
                    CreateDirectories = true
                };

                HttpResponseMessage response = await _client.PostAsJsonAsync(uri, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jobResponse = JsonSerializer.Deserialize<ActionJobResponse>(content, _serializerOptions);
                    
                    _logger.LogInformation($"{response.StatusCode.ToString()} - Move files job started: {jobResponse?.JobId}");
                    return $"Move files job started: {jobResponse?.JobId}";
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(@"ERROR {0}", ex.Message);
                return null;
            }

        }

        public async Task<string> TrainModel()
        {
            _logger.LogInformation($"Request train model (TrainModel) - Using v2 API");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/actions/train-model", string.Empty));

            try
            {
                HttpResponseMessage response = await _client.PostAsync(uri, null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jobResponse = JsonSerializer.Deserialize<ActionJobResponse>(content, _serializerOptions);
                    
                    _logger.LogInformation($"{response.StatusCode.ToString()} - Train model job started: {jobResponse?.JobId}");
                    return $"Train model job started: {jobResponse?.JobId}";
                }
                _logger.LogWarning($"{response.StatusCode.ToString()} - Model training NOT started");
                return "Model training FAILED";


            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }
        }

        public async Task<List<FilesDetailDto>> GetLastFilesList()
        {
            _logger.LogInformation($"Request Last file list (GetLastFilesList) - Using v2 API");

            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/files/lastview", string.Empty));

            var dataResponse = new List<FilesDetailDto>();

            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    dataResponse = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions);
                    _logger.LogInformation($"{response.StatusCode.ToString()} - File List received");
                }
                else
                {
                    _logger.LogWarning($"{response.StatusCode.ToString()} - File List NOT received");
                }
                return dataResponse;

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }
        }

        public async Task<List<FilesDetailDto>> GetAllFiles(string fileCategory)
        {
            _logger.LogInformation($"Request Get all file list (GetAllFiles) - Using v2 API");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v2/files/category/{fileCategory}", string.Empty));

            var dataResponse = new List<FilesDetailDto>();

            try
            {
                HttpResponseMessage response = await _client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    dataResponse = JsonSerializer.Deserialize<List<FilesDetailDto>>(content, _serializerOptions);
                    _logger.LogInformation($"{response.StatusCode.ToString()} - All File List received");
                }
                else
                {
                    _logger.LogWarning($"{response.StatusCode.ToString()} - File List NOT received");
                }


                return dataResponse;

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }
        }

        public async Task<FilesDetailDto> UpdateFileDetail(FilesDetailDto item)
        {
            _logger.LogInformation($"Update file (UpdateFileDetail)");
            Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/UpdateFilesDetail", string.Empty));

            try
            {
                HttpResponseMessage response = await _client.PutAsJsonAsync(uri, item);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var dataResponse = JsonSerializer.Deserialize<FilesDetailDto>(content, _serializerOptions);
                    _logger.LogInformation($"{response.StatusCode.ToString()} - File Updated");
                    return dataResponse;
                }

                _logger.LogWarning($"{response.StatusCode.ToString()} - File  NOT updated");


                return null;

            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.InnerException}");

                return null;
            }
        }

        #region New Interface Implementation (Async with Result Pattern)
        
        public async Task<Result<List<FilesDetailDto>>> GetFilesAsync()
        {
            try
            {
                var result = await GetFiles();
                return Result<List<FilesDetailDto>>.Success(result ?? new List<FilesDetailDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting files: {ex.Message}");
                return Result<List<FilesDetailDto>>.Failure($"Error getting files: {ex.Message}");
            }
        }
        
        public async Task<Result<string>> RefreshCategoryAsync()
        {
            try
            {
                var result = await RefreshCategory();
                return Result<string>.Success(result ?? "Refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing category: {ex.Message}");
                return Result<string>.Failure($"Error refreshing category: {ex.Message}");
            }
        }
        
        public async Task<Result<FilesDetailDto>> GetFileAsync(int id)
        {
            try
            {
                var result = await GetFile(id);
                return result != null 
                    ? Result<FilesDetailDto>.Success(result) 
                    : Result<FilesDetailDto>.Failure($"File with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting file {id}: {ex.Message}");
                return Result<FilesDetailDto>.Failure($"Error getting file {id}: {ex.Message}");
            }
        }
        
        public async Task<Result<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var result = await GetCategories();
                return Result<List<string>>.Success(result ?? new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting categories: {ex.Message}");
                return Result<List<string>>.Failure($"Error getting categories: {ex.Message}");
            }
        }
        
        public async Task<Result<string>> MoveFileAsync(FilesDetailDto fileDetail)
        {
            try
            {
                var result = await MoveFile(fileDetail);
                return Result<string>.Success(result ?? "File moved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving file: {ex.Message}");
                return Result<string>.Failure($"Error moving file: {ex.Message}");
            }
        }
        
        public async Task<Result<string>> TrainModelAsync()
        {
            try
            {
                var result = await TrainModel();
                return Result<string>.Success(result ?? "Model trained successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error training model: {ex.Message}");
                return Result<string>.Failure($"Error training model: {ex.Message}");
            }
        }
        
        public async Task<Result<List<FilesDetailDto>>> GetLastFilesListAsync()
        {
            try
            {
                var result = await GetLastFilesList();
                return Result<List<FilesDetailDto>>.Success(result ?? new List<FilesDetailDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting last files list: {ex.Message}");
                return Result<List<FilesDetailDto>>.Failure($"Error getting last files list: {ex.Message}");
            }
        }
        
        public async Task<Result<List<FilesDetailDto>>> GetAllFilesAsync(string fileCategory)
        {
            try
            {
                var result = await GetAllFiles(fileCategory);
                return Result<List<FilesDetailDto>>.Success(result ?? new List<FilesDetailDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all files: {ex.Message}");
                return Result<List<FilesDetailDto>>.Failure($"Error getting all files: {ex.Message}");
            }
        }
        
        public async Task<Result<FilesDetailDto>> UpdateFileDetailAsync(FilesDetailDto item)
        {
            try
            {
                var result = await UpdateFileDetail(item);
                return result != null 
                    ? Result<FilesDetailDto>.Success(result) 
                    : Result<FilesDetailDto>.Failure("Failed to update file detail");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating file detail: {ex.Message}");
                return Result<FilesDetailDto>.Failure($"Error updating file detail: {ex.Message}");
            }
        }
        
        public async Task<Result<string>> MoveFilesAsync(List<FilesDetailDto> filesToMove)
        {
            try
            {
                var result = await MoveFiles(filesToMove);
                return Result<string>.Success(result ?? "Files moved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving files: {ex.Message}");
                return Result<string>.Failure($"Error moving files: {ex.Message}");
            }
        }
        
        #endregion
    }
}
