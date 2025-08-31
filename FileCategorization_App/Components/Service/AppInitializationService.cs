using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Service to handle app initialization tasks including connectivity setup
/// </summary>
public class AppInitializationService
{
    private readonly IUtilityServices _utilityServices;
    private readonly IConnectivityService _connectivityService;
    private readonly ILogger<AppInitializationService> _logger;
    private bool _isInitialized = false;

    public AppInitializationService(
        IUtilityServices utilityServices, 
        IConnectivityService connectivityService,
        ILogger<AppInitializationService> logger)
    {
        _utilityServices = utilityServices;
        _connectivityService = connectivityService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes app services with proper configuration
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Starting app initialization...");

            // 1. Set up network configuration
            var apiUrl = _utilityServices.SetApiUrl();
            _logger.LogInformation($"Network configuration set: {apiUrl}");

            // 2. Update connectivity service with correct URL
            _connectivityService.UpdateApiBaseUrl(apiUrl);

            // 3. Start connectivity monitoring
            await _connectivityService.StartMonitoringAsync();
            _logger.LogInformation("Connectivity monitoring started");

            _isInitialized = true;
            _logger.LogInformation("App initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during app initialization: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets whether the app has been initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;
}