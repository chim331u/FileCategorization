using System.Net.Http.Json;
using System.Text.Json;
using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.DD;

namespace FileCategorization_App.Components.Service;

public class DDwebService : IDDwebService
{
    HttpClient _client;
    JsonSerializerOptions _serializerOptions;
    IHttpsClientHandlerService _httpsClientHandlerService;
    private readonly IConfiguration _config;
    private readonly IUtilityServices _utilityServices;
    ILogger<ServiceApi> _logger;
    
    public DDwebService(IHttpsClientHandlerService service, IConfiguration config, IUtilityServices utilityServices, ILogger<ServiceApi> logger)
    {
#if DEBUG
        _httpsClientHandlerService = service;
        HttpMessageHandler handler = _httpsClientHandlerService.GetPlatformMessageHandler();
        if (handler != null)
            _client = new HttpClient(handler);
        else
            _client = new HttpClient();
#else
            _client = new HttpClient();
#endif
        _config = config;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        _utilityServices = utilityServices;
        _logger = logger;
    }
    public async Task<List<ThreadSummaryDto>> GetActiveThreads()
    {
        _logger.LogInformation($"Request active threads");
        
        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/GetActiveThreads", string.Empty));
        
        try
        {
            var threadsList = await _client.GetFromJsonAsync<List<ThreadSummaryDto>>(uri);
        
            if (threadsList != null)
            {
                _logger.LogInformation($"Received threads list");
            }
            else
            {
                _logger.LogWarning($"Threads list not received");
            }
        
            return threadsList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message} - {ex.InnerException}");
        
            return null;
        }
    }

    public async Task<List<LinkDto>> GetEd2kLinks(int threadId)
    {
        _logger.LogInformation($"Request links for thread i = {threadId}");
        
        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/GetLinks/{threadId}", string.Empty));
        
        try
        {
            var linkList = await _client.GetFromJsonAsync<List<LinkDto>>(uri);
        
            if (linkList != null)
            {
                _logger.LogInformation($"Received link list");
            }
            else
            {
                _logger.LogWarning($"Link list not received");
            }
        
            return linkList;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message} - {ex.InnerException}");
        
            return null;
        }
    }    
    
    public async Task<string> UseLink(int linkId)
    {
        _logger.LogInformation($"Request use link id = {linkId}");

        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/UseLink/{linkId}", string.Empty));
        
        try
        {
            var result = await _client.GetFromJsonAsync<string>(uri);

            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogInformation($" Link Used");
                return result;
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message} - {ex.InnerException}");
        
            return string.Empty;
        }
    }

    public async Task<bool> RenewThread(int threadId)
    {
        _logger.LogInformation($"Request check url for thread id= {threadId}");

        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/CheckLinks/{threadId}/", string.Empty));
        
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
            _logger.LogError($"{ex.Message} - {ex.InnerException}");
        
            return false;
        }
    }

    public async Task<bool> CheckUrl(string urlToCheck)
    {
        _logger.LogInformation($"Request check url = {urlToCheck}");
        
        var _urlToCheck = Base64UrlEncoder.Encode(urlToCheck);

        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/CheckLink/{_urlToCheck}/", string.Empty));
        
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
            _logger.LogError($"{ex.Message} - {ex.InnerException}");
        
            return false;
        }
    }

    #region New Interface Implementation (Async with Result Pattern)
    
    public async Task<Result<List<ThreadSummaryDto>>> GetActiveThreadsAsync()
    {
        try
        {
            var result = await GetActiveThreads();
            return Result<List<ThreadSummaryDto>>.Success(result ?? new List<ThreadSummaryDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting active threads: {ex.Message}");
            return Result<List<ThreadSummaryDto>>.Failure($"Error getting active threads: {ex.Message}");
        }
    }
    
    public async Task<Result<List<LinkDto>>> GetEd2kLinksAsync(int threadId)
    {
        try
        {
            var result = await GetEd2kLinks(threadId);
            return Result<List<LinkDto>>.Success(result ?? new List<LinkDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting ED2K links: {ex.Message}");
            return Result<List<LinkDto>>.Failure($"Error getting ED2K links: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> UseLinkAsync(int linkId)
    {
        try
        {
            var result = await UseLink(linkId);
            return Result<string>.Success(result ?? "Link used successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error using link: {ex.Message}");
            return Result<string>.Failure($"Error using link: {ex.Message}");
        }
    }
    
    public async Task<Result<bool>> RenewThreadAsync(int threadId)
    {
        try
        {
            var result = await RenewThread(threadId);
            return Result<bool>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error renewing thread: {ex.Message}");
            return Result<bool>.Failure($"Error renewing thread: {ex.Message}");
        }
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