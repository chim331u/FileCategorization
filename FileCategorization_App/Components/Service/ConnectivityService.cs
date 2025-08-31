using System.Diagnostics;
using FileCategorization_App.Components.Interface;
using FileCategorization_Shared.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using MauiConnectivityChangedEventArgs = Microsoft.Maui.Networking.ConnectivityChangedEventArgs;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Service for monitoring network connectivity and server availability in MAUI mobile apps
/// </summary>
public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly IConnectivity _connectivity;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectivityService> _logger;
    private string _apiBaseUrl;
    private readonly Timer _serverPingTimer;
    
    // State tracking
    private bool _isServerReachable = true;
    private ConnectionType _currentConnectionType = ConnectionType.Unknown;
    private TimeSpan _lastPingDuration = TimeSpan.Zero;
    private DateTime _lastSuccessfulPing = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private string? _lastError;
    private bool _isMonitoring = false;

    public ConnectivityService(
        IConnectivity connectivity,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ConnectivityService> logger)
    {
        _connectivity = connectivity;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        // Get API base URL from configuration - will be set dynamically by UtilityServices
        _apiBaseUrl = "http://10.0.2.2:5089"; // Default to LocalNetwork config
        
        // Subscribe to connectivity changes
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        
        // Initialize server ping timer (every 30 seconds) - will be started by AppInitializationService
        _serverPingTimer = new Timer(async _ => await PingServerInternalAsync(), null, 
            Timeout.Infinite, 30000); // 30 seconds in milliseconds
        
        // Initialize state
        UpdateConnectionType();
    }

    #region Public Properties

    public bool IsConnected => _connectivity.NetworkAccess == NetworkAccess.Internet;
    
    public bool IsServerReachable => _isServerReachable;
    
    public ConnectionType ConnectionType => _currentConnectionType;

    #endregion

    #region Public Events

    public event EventHandler<AppConnectivityChangedEventArgs>? ConnectivityChanged;
    
    public event EventHandler<ServerReachabilityChangedEventArgs>? ServerReachabilityChanged;

    #endregion

    #region Public Methods

    /// <summary>
    /// Pings the API server to check availability
    /// </summary>
    public async Task<bool> PingServerAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot ping server: No network connection");
            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Short timeout for ping
            var healthUrl = _apiBaseUrl.TrimEnd('/') + "/api/v1/CategoryList";
            using var response = await httpClient.GetAsync(healthUrl, cancellationToken);
            stopwatch.Stop();
            
            _lastPingDuration = stopwatch.Elapsed;
            var isReachable = response.IsSuccessStatusCode;
            
            if (isReachable)
            {
                _lastSuccessfulPing = DateTime.UtcNow;
                _consecutiveFailures = 0;
                _lastError = null;
                _logger.LogDebug($"Server ping successful: {_lastPingDuration.TotalMilliseconds}ms");
            }
            else
            {
                _consecutiveFailures++;
                _lastError = $"HTTP {response.StatusCode}";
                _logger.LogWarning($"Server ping failed: {response.StatusCode}");
            }
            
            await UpdateServerReachability(isReachable, stopwatch.Elapsed);
            return isReachable;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            _consecutiveFailures++;
            _lastError = "Timeout";
            _logger.LogWarning($"Server ping timeout after {stopwatch.Elapsed.TotalMilliseconds}ms");
            await UpdateServerReachability(false, stopwatch.Elapsed, "Server ping timeout");
            return false;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _consecutiveFailures++;
            _lastError = ex.Message;
            _logger.LogError($"Server ping failed with network error: {ex.Message}");
            await UpdateServerReachability(false, stopwatch.Elapsed, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _consecutiveFailures++;
            _lastError = ex.Message;
            _logger.LogError($"Server ping failed with unexpected error: {ex.Message}");
            await UpdateServerReachability(false, stopwatch.Elapsed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Executes operation with connectivity pre-check
    /// </summary>
    public async Task<Result<T>> ExecuteWithConnectivityCheckAsync<T>(
        Func<Task<Result<T>>> operation, 
        string operationName = "Operation",
        bool requireServerReachability = true,
        CancellationToken cancellationToken = default)
    {
        // Check basic connectivity
        if (!IsConnected)
        {
            _logger.LogWarning($"{operationName}: No network connection available");
            return Result<T>.Failure("No internet connection. Please check your network settings and try again.");
        }

        // Check server reachability if required
        if (requireServerReachability && !IsServerReachable)
        {
            _logger.LogWarning($"{operationName}: Server not reachable, attempting ping");
            
            if (!await PingServerAsync(cancellationToken))
            {
                var errorMessage = ConnectionType == ConnectionType.Cellular 
                    ? "Server is unreachable. Check your mobile data connection and try again."
                    : "Server is unreachable. Check your WiFi connection and try again.";
                    
                return Result<T>.Failure(errorMessage);
            }
        }

        try
        {
            _logger.LogDebug($"{operationName}: Connectivity checks passed, executing operation");
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{operationName}: Operation failed after connectivity check: {ex.Message}");
            return Result<T>.Failure($"Operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes operation with connectivity pre-check (non-Result version)
    /// </summary>
    public async Task<T> ExecuteWithConnectivityCheckAsync<T>(
        Func<Task<T>> operation, 
        T fallbackValue,
        string operationName = "Operation",
        bool requireServerReachability = true,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning($"{operationName}: No network connection, returning fallback value");
            return fallbackValue;
        }

        if (requireServerReachability && !IsServerReachable)
        {
            if (!await PingServerAsync(cancellationToken))
            {
                _logger.LogWarning($"{operationName}: Server unreachable, returning fallback value");
                return fallbackValue;
            }
        }

        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{operationName}: Operation failed, returning fallback value: {ex.Message}");
            return fallbackValue;
        }
    }

    /// <summary>
    /// Starts monitoring connectivity changes
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        _logger.LogInformation("Started connectivity monitoring");
        
        // Start the timer
        _serverPingTimer?.Change(5000, 30000); // 5 seconds delay, 30 seconds interval
        
        // Initial ping
        await PingServerInternalAsync();
    }

    /// <summary>
    /// Stops monitoring connectivity changes
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
            return;

        _isMonitoring = false;
        // Stop the timer
        _serverPingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.LogInformation("Stopped connectivity monitoring");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets detailed connectivity information
    /// </summary>
    public async Task<ConnectivityInfo> GetConnectivityInfoAsync()
    {
        var profiles = _connectivity.ConnectionProfiles;
        var networkName = GetNetworkName();
        
        return new ConnectivityInfo
        {
            IsConnected = IsConnected,
            IsServerReachable = IsServerReachable,
            ConnectionType = ConnectionType,
            NetworkName = networkName,
            IsMetered = IsConnectionMetered(),
            IsRoaming = await IsRoamingAsync(),
            LastPingDuration = _lastPingDuration,
            LastSuccessfulPing = _lastSuccessfulPing,
            ConsecutiveFailures = _consecutiveFailures,
            LastError = _lastError
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Internal ping method called by timer
    /// </summary>
    private async Task PingServerInternalAsync()
    {
        if (!_isMonitoring)
            return;

        try
        {
            await PingServerAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in background server ping: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles connectivity changes from the platform
    /// </summary>
    private void OnConnectivityChanged(object? sender, MauiConnectivityChangedEventArgs e)
    {
        var previousConnectionType = _currentConnectionType;
        UpdateConnectionType();
        
        _logger.LogInformation($"Connectivity changed: {previousConnectionType} → {_currentConnectionType}, Connected: {IsConnected}");
        
        // Fire connectivity changed event
        ConnectivityChanged?.Invoke(this, new AppConnectivityChangedEventArgs
        {
            IsConnected = IsConnected,
            ConnectionType = _currentConnectionType,
            PreviousConnectionType = previousConnectionType
        });
        
        // If we lost connection, mark server as unreachable
        if (!IsConnected)
        {
            _ = UpdateServerReachability(false, TimeSpan.Zero, "No network connection");
        }
        else if (previousConnectionType == ConnectionType.None)
        {
            // Connection restored, ping server
            _ = Task.Run(PingServerInternalAsync);
        }
    }

    /// <summary>
    /// Updates the current connection type based on platform connectivity
    /// </summary>
    private void UpdateConnectionType()
    {
        if (!IsConnected)
        {
            _currentConnectionType = ConnectionType.None;
            return;
        }

        var profiles = _connectivity.ConnectionProfiles;
        
        if (profiles.Contains(ConnectionProfile.WiFi))
            _currentConnectionType = ConnectionType.WiFi;
        else if (profiles.Contains(ConnectionProfile.Cellular))
            _currentConnectionType = ConnectionType.Cellular;
        else if (profiles.Contains(ConnectionProfile.Bluetooth))
            _currentConnectionType = ConnectionType.Bluetooth;
        else if (profiles.Contains(ConnectionProfile.Ethernet))
            _currentConnectionType = ConnectionType.Ethernet;
        else
            _currentConnectionType = ConnectionType.Unknown;
    }

    /// <summary>
    /// Updates server reachability state and fires events
    /// </summary>
    private async Task UpdateServerReachability(bool isReachable, TimeSpan? responseTime = null, string? error = null)
    {
        var previousReachability = _isServerReachable;
        _isServerReachable = isReachable;
        
        if (previousReachability != isReachable)
        {
            _logger.LogInformation($"Server reachability changed: {previousReachability} → {isReachable}");
            
            ServerReachabilityChanged?.Invoke(this, new ServerReachabilityChangedEventArgs
            {
                IsReachable = isReachable,
                PreviousReachability = previousReachability,
                ResponseTime = responseTime,
                Error = error
            });
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current network name (WiFi SSID, cellular carrier, etc.)
    /// </summary>
    private string? GetNetworkName()
    {
        // This would require platform-specific implementations
        // For now, return connection type as string
        return ConnectionType.ToString();
    }

    /// <summary>
    /// Checks if the current connection is metered (has data limits)
    /// </summary>
    private bool IsConnectionMetered()
    {
        // Cellular connections are typically metered
        return ConnectionType == ConnectionType.Cellular;
    }

    /// <summary>
    /// Checks if the device is roaming (for cellular connections)
    /// </summary>
    private async Task<bool> IsRoamingAsync()
    {
        // This would require platform-specific implementations
        // For now, return false as a safe default
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// Updates the API base URL for server connectivity tests
    /// </summary>
    public void UpdateApiBaseUrl(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl;
        _logger.LogInformation($"Updated API base URL to: {_apiBaseUrl}");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _serverPingTimer?.Dispose();
    }

    #endregion
}