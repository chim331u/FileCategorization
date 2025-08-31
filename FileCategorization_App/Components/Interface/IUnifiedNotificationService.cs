using FileCategorization_Shared.DTOs.FileManagement;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Unified notification service that provides business-logic specific SignalR handling
/// Similar to WEB project's approach but adapted for MAUI with direct UI updates
/// </summary>
public interface IUnifiedNotificationService : IAsyncDisposable
{
    // Connection Management
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
    string? ConnectionId { get; }

    // Business Logic Events - High Level
    event Action<string>? FileOperationCompleted;
    event Action<int>? FileMoved; // fileId moved successfully
    event Action<string>? JobStatusUpdated;
    event Action<string>? ModelTrainingCompleted;
    event Action<string>? RefreshProgress;
    event Action<string>? ConnectionStatusChanged;
    event Action<string>? ErrorOccurred;

    // Low Level Events - for debugging/monitoring
    event Action<string, decimal>? StockNotificationReceived;
    event Action<int, string, string, string, FileCategorization_Shared.Enums.MoveFilesResults>? MoveFileNotificationReceived;
    event Action<string, FileCategorization_Shared.Enums.MoveFilesResults>? JobNotificationReceived;
    event Action<string, FileCategorization_Shared.Enums.MoveFilesResults, int, int, int>? JobNotificationWithStatsReceived;
    event Action<string, int>? RefreshFilesNotificationReceived;

    // Manual message sending (if needed)
    Task SendMessageAsync(string method, params object[] args);

    // Helper methods for common scenarios
    void SubscribeToFileOperations(Action<int> onFileMoved, Action<string> onJobCompleted);
    void UnsubscribeFromFileOperations();
}