using FileCategorization_Web.Data.DTOs.FileCategorizationDTOs;

namespace FileCategorization_Web.Services.SignalR;

public interface INotificationService : IAsyncDisposable
{
    // Connection Management
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
    string? ConnectionId { get; }

    // Events for real-time notifications
    event Action<string, decimal> StockNotificationReceived;
    event Action<int, string, MoveFilesResults> MoveFileNotificationReceived;
    event Action<string, MoveFilesResults> JobNotificationReceived;
    event Action<string> ConnectionEstablished;
    event Action<string?> ConnectionLost;
    event Action<string> ErrorOccurred;

    // Manual message sending (if needed)
    Task SendMessageAsync(string method, params object[] args);
}