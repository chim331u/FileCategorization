namespace FileCategorization_App.Components.Interface;

public interface ISignalRService : IAsyncDisposable
{
    // Connection Management
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
    string? ConnectionId { get; }
    
    // Refresh job callbacks for direct method calling
    Func<string, Task>? OnRefreshJobCompleted { get; set; }
    Action<string>? OnRefreshJobFailed { get; set; }

    // Events for real-time notifications
    event Action<string, decimal>? StockNotificationReceived;
    event Action<int, string, string, string, FileCategorization_Shared.Enums.MoveFilesResults>? MoveFileNotificationReceived;
    event Action<string, FileCategorization_Shared.Enums.MoveFilesResults>? JobNotificationReceived;
    event Action<string, FileCategorization_Shared.Enums.MoveFilesResults, int, int, int>? JobNotificationWithStatsReceived;
    event Action<string, int>? RefreshFilesNotificationReceived;
    event Action<string>? ConnectionEstablished;
    event Action<string?>? ConnectionLost;
    event Action<string>? ErrorOccurred;

    // Manual message sending (if needed)
    Task SendMessageAsync(string method, params object[] args);
}