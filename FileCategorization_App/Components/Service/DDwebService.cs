using System.Net.Http.Json;
using System.Text.Json;
using FileCategorization_App.Components.Interface;
using FileCategorization_App.Data.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
    public async Task<List<ThreadsDto>> GetActiveThreads()
    {
        _logger.LogInformation($"Request active threads");
        
        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/GetActiveThreads", string.Empty));
        
        try
        {
            var threadsList = await _client.GetFromJsonAsync<List<ThreadsDto>>(uri);
        
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

    public async Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId)
    {
        _logger.LogInformation($"Request links for thread i = {threadId}");
        
        Uri uri = new Uri(string.Format(_utilityServices.ApiUrl + $"api/v1/GetLinks/{threadId}", string.Empty));
        
        try
        {
            var linkList = await _client.GetFromJsonAsync<List<Ed2kLinkDto>>(uri);
        
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
}