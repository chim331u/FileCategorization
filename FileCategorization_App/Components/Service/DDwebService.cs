using System.Net.Http.Json;
using System.Text.Json;
using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.DD;
using FileCategorization_App.Data.DTOs.DD;

namespace FileCategorization_App.Components.Service;

public class DDwebService : BaseApiService, IDDwebService
{
    private readonly IConfiguration _config;
    private readonly IUtilityServices _utilityServices;
    
    public DDwebService(IHttpsClientHandlerService service, IConfiguration config, IUtilityServices utilityServices, ILogger<ServiceApi> logger)
        : base(CreateHttpClient(service), logger)
    {
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
        HttpClient client;
#if DEBUG
        HttpMessageHandler handler = service.GetPlatformMessageHandler();
        client = handler != null ? new HttpClient(handler) : new HttpClient();
#else
        client = new HttpClient();
#endif
        
        // Configure timeout and default headers for DD operations
        client.Timeout = TimeSpan.FromSeconds(45); // Longer timeout for DD operations
        client.DefaultRequestHeaders.Add("User-Agent", "FileCategorization_App/1.0");
        
        return client;
    }
    public async Task<List<ThreadSummaryDto>> GetActiveThreads()
    {
        _logger.LogInformation($"Request active threads - Using v2 API");
        
        try
        {
            Uri uri = CreateUri("api/v2/dd/threads");
            HttpResponseMessage response = await _client.GetAsync(uri);
            var result = await HandleResponseAsync<List<ThreadSummaryDto>>(response, "GetActiveThreads");
            
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Received {result.Value?.Count ?? 0} active threads");
                return result.Value ?? new List<ThreadSummaryDto>();
            }
            else
            {
                _logger.LogWarning($"Failed to get active threads: {result.Error}");
                return new List<ThreadSummaryDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting active threads: {ex.Message} - {ex.InnerException}");
            return new List<ThreadSummaryDto>();
        }
    }

    public async Task<List<LinkDto>> GetEd2kLinks(int threadId)
    {
        _logger.LogInformation($"Request links for thread {threadId} - Using v2 API");
        
        try
        {
            Uri uri = CreateUri($"api/v2/dd/threads/{threadId}/links");
            HttpResponseMessage response = await _client.GetAsync(uri);
            var result = await HandleResponseAsync<List<LinkDto>>(response, "GetEd2kLinks");
            
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Received {result.Value?.Count ?? 0} links for thread {threadId}");
                return result.Value ?? new List<LinkDto>();
            }
            else
            {
                _logger.LogWarning($"Failed to get links for thread {threadId}: {result.Error}");
                return new List<LinkDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting links for thread {threadId}: {ex.Message} - {ex.InnerException}");
            return new List<LinkDto>();
        }
    }    
    
    public async Task<string> UseLink(int linkId)
    {
        _logger.LogInformation($"Request use link id = {linkId} - Using v2 API");

        try
        {
            Uri uri = CreateUri($"api/v2/dd/links/{linkId}/use");
            HttpResponseMessage response = await _client.PostAsync(uri, null);
            var result = await HandleResponseAsync<FileCategorization_Shared.DTOs.DD.LinkUsageResultDto>(response, "UseLink");
            
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Link {linkId} used successfully");
                return result.Value?.Title ?? "Link used successfully";
            }
            else
            {
                _logger.LogWarning($"Failed to use link {linkId}: {result.Error}");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error using link {linkId}: {ex.Message} - {ex.InnerException}");
            return string.Empty;
        }
    }

    public async Task<bool> RenewThread(int threadId)
    {
        _logger.LogInformation($"Request refresh thread links for thread id = {threadId} - Using v2 API");

        try
        {
            Uri uri = CreateUri($"api/v2/dd/threads/{threadId}/refresh");
            HttpResponseMessage response = await _client.PostAsync(uri, null);
            var result = await HandleResponseAsync<ThreadProcessingResultDto>(response, "RenewThread");
            
            if (result.IsSuccess)
            {
                _logger.LogInformation($"Thread {threadId} refreshed successfully: {result.Value?.NewLinksCount ?? 0} new links found");
                return true;
            }
            else
            {
                _logger.LogWarning($"Failed to refresh thread {threadId}: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error refreshing thread {threadId}: {ex.Message} - {ex.InnerException}");
            return false;
        }
    }

    /// <summary>
    /// CheckUrl method - remains on v1 API as no v2 equivalent exists
    /// This method will be deprecated in future versions
    /// </summary>
    [Obsolete("This method uses v1 API and will be removed in future versions")]
    public async Task<bool> CheckUrl(string urlToCheck)
    {
        _logger.LogInformation($"Request check url = {urlToCheck} - Using v1 API (deprecated)");
        
        var _urlToCheck = Base64UrlEncoder.Encode(urlToCheck);
        Uri uri = new Uri(_utilityServices.ApiUrl + $"api/v1/CheckLink/{_urlToCheck}/");
        
        try
        {
            var result = await _client.GetAsync(uri);

            if (result.IsSuccessStatusCode)
            {
                _logger.LogInformation($"{result.StatusCode.ToString()} - Url Checked");
                return result.IsSuccessStatusCode;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking URL: {ex.Message} - {ex.InnerException}");
            return false;
        }
    }

    #region New Interface Implementation (Async with Result Pattern)
    
    public async Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync()
    {
        return await ExecuteWithRetryAsync<List<ThreadSummaryDto>>(
            () => _client.GetAsync(CreateUri("api/v2/dd/threads")),
            "GetActiveThreadsAsync",
            maxRetries: 3);
    }
    
    public async Task<Result<List<LinkDto>>> GetEd2kLinksAsync(int threadId)
    {
        return await ExecuteWithRetryAsync<List<LinkDto>>(
            () => _client.GetAsync(CreateUri($"api/v2/dd/threads/{threadId}/links")),
            "GetEd2kLinksAsync",
            maxRetries: 3);
    }
    
    public async Task<Result<string>> UseLinkAsync(int linkId)
    {
        var result = await ExecuteWithRetryAsync<FileCategorization_Shared.DTOs.DD.LinkUsageResultDto>(
            () => _client.PostAsync(CreateUri($"api/v2/dd/links/{linkId}/use"), null),
            "UseLinkAsync",
            maxRetries: 2); // Reduce retries for POST operations
            
        if (result.IsSuccess)
        {
            return Result<string>.Success(result.Value?.Title ?? "Link used successfully");
        }
        return Result<string>.Failure(result.Error);
    }
    
    public async Task<Result<bool>> RenewThreadAsync(int threadId)
    {
        var result = await ExecuteWithRetryAsync<ThreadProcessingResultDto>(
            () => _client.PostAsync(CreateUri($"api/v2/dd/threads/{threadId}/refresh"), null),
            "RenewThreadAsync",
            maxRetries: 2); // Reduce retries for POST operations
            
        return Result<bool>.Success(result.IsSuccess);
    }
    
    [Obsolete("This method is deprecated in v2 API and will be removed")]
    public async Task<Result<bool>> CheckUrlAsync(string urlToCheck)
    {
        try
        {
            var result = await CheckUrl(urlToCheck);
            return Result<bool>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking URL: {ex.Message}");
            return Result<bool>.Failure($"Error checking URL: {ex.Message}");
        }
    }
    
    #endregion
}