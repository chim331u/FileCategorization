using System.Net;
using System.Text.Json;
using FileCategorization_Shared.Common;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Base class for API services providing common functionality for v2 API integration
/// </summary>
public abstract class BaseApiService
{
    protected readonly HttpClient _client;
    protected readonly JsonSerializerOptions _serializerOptions;
    protected readonly ILogger _logger;

    protected BaseApiService(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true // For v1/v2 compatibility
        };
    }

    /// <summary>
    /// Handles HTTP responses and converts them to Result pattern
    /// </summary>
    protected async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response, string operationName)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Try to deserialize as Result<T> first (v2 pattern)
                if (content.Contains("\"isSuccess\"") || content.Contains("\"error\""))
                {
                    var result = JsonSerializer.Deserialize<Result<T>>(content, _serializerOptions);
                    if (result != null)
                    {
                        _logger.LogInformation($"{operationName}: {response.StatusCode} - Result pattern response received");
                        return result;
                    }
                }

                // Fallback to direct deserialization (v1 pattern)
                var data = JsonSerializer.Deserialize<T>(content, _serializerOptions);
                _logger.LogInformation($"{operationName}: {response.StatusCode} - Direct response received");
                return Result<T>.Success(data);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"{operationName}: {response.StatusCode} - {errorContent}");
                return Result<T>.Failure($"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"{operationName}: JSON deserialization error - {ex.Message}");
            return Result<T>.Failure($"JSON deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{operationName}: Unexpected error - {ex.Message}");
            return Result<T>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles simple string responses
    /// </summary>
    protected async Task<Result<string>> HandleStringResponseAsync(HttpResponseMessage response, string operationName)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"{operationName}: {response.StatusCode} - String response received");
                return Result<string>.Success(content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"{operationName}: {response.StatusCode} - {errorContent}");
                return Result<string>.Failure($"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{operationName}: Error - {ex.Message}");
            return Result<string>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates URI with proper encoding
    /// </summary>
    protected Uri CreateUri(string endpoint)
    {
        return new Uri(_client.BaseAddress, endpoint);
    }
}