using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.DTOs.DD;
using FileCategorization_Web.Data.Configuration;
using FileCategorization_Web.Data.DTOs.WebScrum;
using FileCategorization_Web.Interfaces;
using Microsoft.Extensions.Options;

namespace FileCategorization_Web.Services;

public class ModernWebScrumService : IWebScrumServices
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<ModernWebScrumService> _logger;
    private readonly FileCategorizationApiOptions _options;

    public ModernWebScrumService(
        HttpClient httpClient,
        IOptions<FileCategorizationApiOptions> options,
        ILogger<ModernWebScrumService> logger)
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

    public async Task<List<ThreadsDto>> GetActiveThreads()
    {
        try
        {
            _logger.LogInformation("Fetching active threads using v2 API");
            
            var response = await _httpClient.GetAsync("api/v2/dd/threads");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var threads = JsonSerializer.Deserialize<List<ThreadSummaryDto>>(content, _serializerOptions) ?? new List<ThreadSummaryDto>();
                
                // Convert v2 ThreadSummaryDto to legacy ThreadsDto for compatibility
                var legacyThreads = threads.Select(t => new ThreadsDto
                {
                    Id = t.Id,
                    MainTitle = t.MainTitle,
                    Type = t.Type,
                    Url = t.Url,
                    LinksNumber = t.LinksCount, // Map to legacy property name
                    LinksCount = t.LinksCount,
                    NewLinksCount = t.NewLinksCount,
                    UsedLinksCount = t.UsedLinksCount,
                    NewLinks = t.HasNewLinks, // Map to legacy property name
                    HasNewLinks = t.HasNewLinks,
                    CreatedDate = t.CreatedDate,
                    LastUpdatedDate = t.LastUpdatedDate
                }).ToList();
                
                _logger.LogInformation("Retrieved {Count} active threads from v2 API", legacyThreads.Count);
                return legacyThreads;
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get active threads from v2 API: {Error}", error);
            return new List<ThreadsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching active threads from v2 API");
            return new List<ThreadsDto>();
        }
    }

    public async Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId)
    {
        try
        {
            _logger.LogInformation("Fetching links for thread {ThreadId} using v2 API", threadId);
            
            var response = await _httpClient.GetAsync($"api/v2/dd/threads/{threadId}/links");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var links = JsonSerializer.Deserialize<List<LinkDto>>(content, _serializerOptions) ?? new List<LinkDto>();
                
                // Convert v2 LinkDto to legacy Ed2kLinkDto for compatibility
                var legacyLinks = links.Select(l => new Ed2kLinkDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    Ed2kLink = l.Ed2kLink,
                    IsNew = l.IsNew,
                    IsUsed = l.IsUsed,
                    ThreadId = l.ThreadId,
                    CreatedDate = l.CreatedDate,
                    LastUpdatedDate = l.LastUpdatedDate
                }).ToList();
                
                _logger.LogInformation("Retrieved {Count} links for thread {ThreadId} from v2 API", legacyLinks.Count, threadId);
                return legacyLinks;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Thread {ThreadId} not found in v2 API", threadId);
                return new List<Ed2kLinkDto>();
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to get links for thread {ThreadId} from v2 API: {Error}", threadId, error);
            return new List<Ed2kLinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching links for thread {ThreadId} from v2 API", threadId);
            return new List<Ed2kLinkDto>();
        }
    }

    public async Task<string> UseLink(int linkId)
    {
        try
        {
            _logger.LogInformation("Using link {LinkId} using v2 API", linkId);
            
            var response = await _httpClient.PostAsync($"api/v2/dd/links/{linkId}/use", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LinkUsageResultDto>(content, _serializerOptions);
                
                if (result != null)
                {
                    _logger.LogInformation("Link {LinkId} used successfully using v2 API", linkId);
                    return result.Ed2kLink;
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Link {LinkId} not found in v2 API", linkId);
                return string.Empty;
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to use link {LinkId} using v2 API: {Error}", linkId, error);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while using link {LinkId} using v2 API", linkId);
            return string.Empty;
        }
    }

    public async Task<bool> RenewThread(int threadId)
    {
        try
        {
            _logger.LogInformation("Renewing thread {ThreadId} using v2 API", threadId);
            
            var response = await _httpClient.PostAsync($"api/v2/dd/threads/{threadId}/refresh", null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Thread {ThreadId} renewed successfully using v2 API", threadId);
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Thread {ThreadId} not found for renewal in v2 API", threadId);
                return false;
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to renew thread {ThreadId} using v2 API: {Error}", threadId, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while renewing thread {ThreadId} using v2 API", threadId);
            return false;
        }
    }

    public async Task<bool> CheckUrl(string urlToCheck)
    {
        try
        {
            _logger.LogInformation("Processing thread URL using v2 API: {Url}", urlToCheck);
            
            var request = new { threadUrl = urlToCheck };
            var response = await _httpClient.PostAsJsonAsync("api/v2/dd/threads/process", request, _serializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Thread URL processed successfully using v2 API");
                return true;
            }

            var error = $"API v2 Error: {response.StatusCode}";
            _logger.LogWarning("Failed to process thread URL using v2 API: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while processing thread URL using v2 API");
            return false;
        }
    }
}