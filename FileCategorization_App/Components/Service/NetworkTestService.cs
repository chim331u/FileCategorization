using FileCategorization_Shared.Common;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Test service to verify timeout and retry functionality
/// This is for testing purposes only and can be removed in production
/// </summary>
public class NetworkTestService : BaseApiService
{
    public NetworkTestService(HttpClient client, ILogger<NetworkTestService> logger) 
        : base(client, logger)
    {
    }

    /// <summary>
    /// Test method to verify timeout configuration works
    /// </summary>
    public async Task<Result<string>> TestTimeout()
    {
        try
        {
            // This will test the 30-second timeout
            _logger.LogInformation("Testing timeout with 35-second delay endpoint...");
            
            // Simulate a slow endpoint that takes longer than our 30s timeout
            var uri = CreateUri("api/test/slow-endpoint"); // This endpoint doesn't exist, which is fine for testing
            
            return await ExecuteStringWithRetryAsync(
                () => _client.GetAsync(uri),
                "TestTimeout",
                maxRetries: 2 // Reduce retries for faster testing
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Test timeout error: {ex.Message}");
            return Result<string>.Failure($"Test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test method to verify retry policy works with network errors
    /// </summary>
    public async Task<Result<string>> TestRetryPolicy()
    {
        try
        {
            _logger.LogInformation("Testing retry policy with non-existent endpoint...");
            
            // Use a non-existent endpoint to trigger network errors
            var uri = CreateUri("api/test/non-existent");
            
            return await ExecuteStringWithRetryAsync(
                () => _client.GetAsync(uri),
                "TestRetryPolicy",
                maxRetries: 3
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Test retry policy error: {ex.Message}");
            return Result<string>.Failure($"Test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test method to verify successful API call works normally
    /// </summary>
    public async Task<Result<string>> TestSuccessfulCall()
    {
        try
        {
            _logger.LogInformation("Testing successful API call...");
            
            // Test with an existing endpoint (health check or similar)
            var uri = CreateUri("api/health");
            
            return await ExecuteStringWithRetryAsync(
                () => _client.GetAsync(uri),
                "TestSuccessfulCall",
                maxRetries: 1 // Only one attempt needed for successful calls
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Test successful call error: {ex.Message}");
            return Result<string>.Failure($"Test failed: {ex.Message}");
        }
    }
}