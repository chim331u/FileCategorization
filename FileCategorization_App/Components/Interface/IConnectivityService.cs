using FileCategorization_Shared.Common;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Interface for monitoring network connectivity and server availability in mobile environments
/// </summary>
public interface IConnectivityService
{
    /// <summary>
    /// Gets current network connectivity status
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets current server availability status
    /// </summary>
    bool IsServerReachable { get; }
    
    /// <summary>
    /// Gets current connection type (WiFi, Cellular, etc.)
    /// </summary>
    ConnectionType ConnectionType { get; }
    
    /// <summary>
    /// Event fired when connectivity status changes
    /// </summary>
    event EventHandler<AppConnectivityChangedEventArgs> ConnectivityChanged;
    
    /// <summary>
    /// Event fired when server reachability changes
    /// </summary>
    event EventHandler<ServerReachabilityChangedEventArgs> ServerReachabilityChanged;
    
    /// <summary>
    /// Pings the API server to check availability
    /// </summary>
    Task<bool> PingServerAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes operation with connectivity pre-check
    /// </summary>
    Task<Result<T>> ExecuteWithConnectivityCheckAsync<T>(
        Func<Task<Result<T>>> operation, 
        string operationName = "Operation",
        bool requireServerReachability = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes operation with connectivity pre-check (non-Result version)
    /// </summary>
    Task<T> ExecuteWithConnectivityCheckAsync<T>(
        Func<Task<T>> operation, 
        T fallbackValue,
        string operationName = "Operation",
        bool requireServerReachability = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts monitoring connectivity changes
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Stops monitoring connectivity changes
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Gets detailed connectivity information
    /// </summary>
    Task<ConnectivityInfo> GetConnectivityInfoAsync();
    
    /// <summary>
    /// Updates the API base URL for server connectivity tests
    /// </summary>
    void UpdateApiBaseUrl(string apiBaseUrl);
}

/// <summary>
/// Types of network connections
/// </summary>
public enum ConnectionType
{
    None,
    Unknown,
    Cellular,
    WiFi,
    Bluetooth,
    Ethernet
}

/// <summary>
/// Detailed connectivity information
/// </summary>
public class ConnectivityInfo
{
    public bool IsConnected { get; set; }
    public bool IsServerReachable { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public string? NetworkName { get; set; }
    public bool IsMetered { get; set; }
    public bool IsRoaming { get; set; }
    public TimeSpan LastPingDuration { get; set; }
    public DateTime LastSuccessfulPing { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Event args for connectivity changes
/// </summary>
public class AppConnectivityChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public ConnectionType PreviousConnectionType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for server reachability changes
/// </summary>
public class ServerReachabilityChangedEventArgs : EventArgs
{
    public bool IsReachable { get; set; }
    public bool PreviousReachability { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}