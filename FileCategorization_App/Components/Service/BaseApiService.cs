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
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError($"{operationName}: Request timeout - {ex.Message}");
            return Result<T>.Failure("Request timed out: Server may be unavailable or connection is too slow");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"{operationName}: Request cancelled - {ex.Message}");
            return Result<T>.Failure("Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"{operationName}: Network error - {ex.Message}");
            return Result<T>.Failure("Network connection failed: Check internet connectivity");
        }
        catch (JsonException ex)
        {
            _logger.LogError($"{operationName}: JSON deserialization error - {ex.Message}");
            return Result<T>.Failure("Invalid response format from server");
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
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError($"{operationName}: Request timeout - {ex.Message}");
            return Result<string>.Failure("Request timed out: Server may be unavailable or connection is too slow");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"{operationName}: Request cancelled - {ex.Message}");
            return Result<string>.Failure("Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"{operationName}: Network error - {ex.Message}");
            return Result<string>.Failure("Network connection failed: Check internet connectivity");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{operationName}: Unexpected error - {ex.Message}");
            return Result<string>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates URI with proper encoding
    /// </summary>
    protected Uri CreateUri(string endpoint)
    {
        return new Uri(_client.BaseAddress, endpoint);
    }

    /// <summary>
    /// Executes HTTP operation with retry policy and exponential backoff
    /// </summary>
    protected async Task<Result<T>> ExecuteWithRetryAsync<T>(
        Func<Task<HttpResponseMessage>> operation, 
        string operationName,
        int maxRetries = 3)
    {
        var baseDelay = TimeSpan.FromSeconds(1);
        Exception lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await operation();
                return await HandleResponseAsync<T>(response, operationName);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning($"{operationName}: Network error on attempt {attempt}/{maxRetries} - {ex.Message}");
                
                if (attempt == maxRetries)
                    break;
                    
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogInformation($"{operationName}: Retrying in {delay.TotalSeconds} seconds...");
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogWarning($"{operationName}: Timeout on attempt {attempt}/{maxRetries}");
                
                if (attempt == maxRetries)
                    break;
                    
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                // Don't retry for non-network related exceptions
                _logger.LogError($"{operationName}: Non-retryable error - {ex.Message}");
                return Result<T>.Failure($"Operation failed: {ex.Message}");
            }
        }

        var errorMessage = lastException switch
        {
            HttpRequestException => "Network connection failed after multiple attempts. Check internet connectivity.",
            TaskCanceledException => "Operation timed out after multiple attempts. Server may be unavailable.",
            _ => $"Operation failed: {lastException?.Message}"
        };

        return Result<T>.Failure(errorMessage);
    }

    /// <summary>
    /// Executes string response operation with retry policy
    /// </summary>
    protected async Task<Result<string>> ExecuteStringWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation, 
        string operationName,
        int maxRetries = 3)
    {
        var baseDelay = TimeSpan.FromSeconds(1);
        Exception lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await operation();
                return await HandleStringResponseAsync(response, operationName);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning($"{operationName}: Network error on attempt {attempt}/{maxRetries} - {ex.Message}");
                
                if (attempt == maxRetries)
                    break;
                    
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogWarning($"{operationName}: Timeout on attempt {attempt}/{maxRetries}");
                
                if (attempt == maxRetries)
                    break;
                    
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{operationName}: Non-retryable error - {ex.Message}");
                return Result<string>.Failure($"Operation failed: {ex.Message}");
            }
        }

        var errorMessage = lastException switch
        {
            HttpRequestException => "Network connection failed after multiple attempts. Check internet connectivity.",
            TaskCanceledException => "Operation timed out after multiple attempts. Server may be unavailable.",
            _ => $"Operation failed: {lastException?.Message}"
        };

        return Result<string>.Failure(errorMessage);
    }
}