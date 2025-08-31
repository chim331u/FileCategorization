using Microsoft.AspNetCore.SignalR.Client;
using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;
using Radzen;
using FileCategorization_Shared.DTOs.FileManagement;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Unified notification service that wraps SignalRService with business-logic specific events
/// Provides high-level events for common UI scenarios while maintaining access to low-level SignalR events
/// </summary>
public class UnifiedNotificationService : IUnifiedNotificationService
{
    private readonly ISignalRService _signalRService;
    private readonly ILogger<UnifiedNotificationService> _logger;
    private bool _disposed = false;

    // High-level Business Events
    public event Action<string>? FileOperationCompleted;
    public event Action<int>? FileMoved;
    public event Action<string>? JobStatusUpdated;
    public event Action<string>? ModelTrainingCompleted;
    public event Action<string>? RefreshProgress;
    public event Action<string>? ConnectionStatusChanged;
    public event Action<string>? ErrorOccurred;

    // Low-level SignalR Events (for debugging/monitoring)
    public event Action<string, decimal>? StockNotificationReceived;
    public event Action<int, string, string, string, FileCategorization_Shared.Enums.MoveFilesResults>? MoveFileNotificationReceived;
    public event Action<string, FileCategorization_Shared.Enums.MoveFilesResults>? JobNotificationReceived;
    public event Action<string, FileCategorization_Shared.Enums.MoveFilesResults, int, int, int>? JobNotificationWithStatsReceived;
    public event Action<string, int>? RefreshFilesNotificationReceived;

    // Connection Properties
    public bool IsConnected => _signalRService.IsConnected;
    public string? ConnectionId => _signalRService.ConnectionId;

    public UnifiedNotificationService(
        ISignalRService signalRService,
        ILogger<UnifiedNotificationService> logger)
    {
        _signalRService = signalRService;
        _logger = logger;
        
        SetupEventHandlers();
        _logger.LogInformation("UnifiedNotificationService initialized");
    }

    public async Task StartAsync()
    {
        await _signalRService.StartAsync();
    }

    public async Task StopAsync()
    {
        await _signalRService.StopAsync();
    }

    public async Task SendMessageAsync(string method, params object[] args)
    {
        await _signalRService.SendMessageAsync(method, args);
    }

    /// <summary>
    /// Helper method for pages that need to handle file operations
    /// Automatically manages subscription/unsubscription
    /// </summary>
    public void SubscribeToFileOperations(Action<int> onFileMoved, Action<string> onJobCompleted)
    {
        FileMoved += onFileMoved;
        FileOperationCompleted += onJobCompleted;
        
        _logger.LogDebug("Subscribed to file operations");
    }

    public void UnsubscribeFromFileOperations()
    {
        // Note: This removes ALL subscribers, which is acceptable for MAUI single-page scenarios
        // In a more complex scenario, we'd need to track individual subscriptions
        FileMoved = null;
        FileOperationCompleted = null;
        
        _logger.LogDebug("Unsubscribed from file operations");
    }

    private void SetupEventHandlers()
    {
        // Wire up low-level SignalR events to high-level business events
        
        _signalRService.ConnectionEstablished += (connectionId) =>
        {
            ConnectionStatusChanged?.Invoke($"Connected: {connectionId}");
        };
        
        _signalRService.ConnectionLost += (error) =>
        {
            ConnectionStatusChanged?.Invoke($"Disconnected: {error ?? "Unknown reason"}");
        };
        
        _signalRService.ErrorOccurred += (error) =>
        {
            ErrorOccurred?.Invoke(error);
        };

        // Stock notifications (pass through for debugging)
        _signalRService.StockNotificationReceived += (stockName, stockPrice) =>
        {
            StockNotificationReceived?.Invoke(stockName, stockPrice);
        };

        // File move notifications - Convert to business events
        _signalRService.MoveFileNotificationReceived += (fileId, fileName, destinationPath, resultText, result) =>
        {
            // Pass through low-level event
            MoveFileNotificationReceived?.Invoke(fileId, fileName, destinationPath, resultText, result);
            
            // Convert to high-level business event
            if (result == FileCategorization_Shared.Enums.MoveFilesResults.Completed)
            {
                _logger.LogInformation("File {FileId} moved successfully: {FileName}", fileId, fileName);
                FileMoved?.Invoke(fileId);
            }
            else if (result == FileCategorization_Shared.Enums.MoveFilesResults.Failed)
            {
                _logger.LogError("File {FileId} move failed: {ResultText}", fileId, resultText);
                ErrorOccurred?.Invoke($"File move failed: {fileName} - {resultText}");
            }
        };

        // Job notifications - Convert to business events
        _signalRService.JobNotificationReceived += (resultText, result) =>
        {
            // Pass through low-level event
            JobNotificationReceived?.Invoke(resultText, result);
            
            // Convert to high-level business events
            switch (result)
            {
                case FileCategorization_Shared.Enums.MoveFilesResults.Moved:
                    _logger.LogInformation("Files moved successfully: {ResultText}", resultText);
                    FileOperationCompleted?.Invoke(ParseJobMessage(resultText));
                    break;
                
                case FileCategorization_Shared.Enums.MoveFilesResults.Completed:
                    // Try to determine job type from message content
                    if (resultText.Contains("training", StringComparison.OrdinalIgnoreCase) ||
                        resultText.Contains("model", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Model training completed: {ResultText}", resultText);
                        ModelTrainingCompleted?.Invoke(ParseJobMessage(resultText));
                    }
                    else
                    {
                        _logger.LogInformation("Job completed: {ResultText}", resultText);
                        FileOperationCompleted?.Invoke(ParseJobMessage(resultText));
                    }
                    break;
                
                case FileCategorization_Shared.Enums.MoveFilesResults.Processing:
                    _logger.LogInformation("Job processing: {ResultText}", resultText);
                    JobStatusUpdated?.Invoke(ParseJobMessage(resultText));
                    break;
                
                case FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent:
                    _logger.LogWarning("Job warning - ID not present: {ResultText}", resultText);
                    ErrorOccurred?.Invoke($"Job warning - ID not found: {ParseJobMessage(resultText)}");
                    break;
                
                case FileCategorization_Shared.Enums.MoveFilesResults.Failed:
                    _logger.LogError("Job failed: {ResultText}", resultText);
                    ErrorOccurred?.Invoke($"Job failed: {ParseJobMessage(resultText)}");
                    break;
                
                default:
                    _logger.LogWarning("Unknown job result type: {Result} - {ResultText}", result, resultText);
                    JobStatusUpdated?.Invoke(ParseJobMessage(resultText));
                    break;
            }
        };

        // Job notifications with statistics
        _signalRService.JobNotificationWithStatsReceived += (resultText, result, totalFiles, successfulFiles, failedFiles) =>
        {
            // Pass through low-level event
            JobNotificationWithStatsReceived?.Invoke(resultText, result, totalFiles, successfulFiles, failedFiles);
            
            // Convert to high-level business event
            var statsMessage = $"Processed {totalFiles} files - Success: {successfulFiles}, Failed: {failedFiles}";
            
            if (result == FileCategorization_Shared.Enums.MoveFilesResults.Completed)
            {
                _logger.LogInformation("File operation completed: {StatsMessage}", statsMessage);
                FileOperationCompleted?.Invoke(statsMessage);
            }
            else
            {
                _logger.LogError("File operation failed: {StatsMessage}", statsMessage);
                ErrorOccurred?.Invoke($"File operation failed: {statsMessage}");
            }
        };

        // Refresh files notifications
        _signalRService.RefreshFilesNotificationReceived += (message, progress) =>
        {
            _logger.LogInformation("🔔 UnifiedNotificationService: RefreshFiles SignalR received - '{Message}', Progress: {Progress}%", message, progress);
            
            // Pass through low-level event FIRST (this is what Index.razor listens to)
            RefreshFilesNotificationReceived?.Invoke(message, progress);
            
            // Convert to high-level business events based on content and progress
            if (progress >= 100 || message.ToLower().Contains("completed") || message.ToLower().Contains("finished"))
            {
                _logger.LogInformation("🎉 UnifiedNotificationService: Refresh job COMPLETED - triggering FileOperationCompleted event");
                // NOTE: For refresh jobs, Index.razor handles completion via RefreshFilesNotificationReceived
                // FileOperationCompleted is for other job types, but we still trigger it for consistency
                FileOperationCompleted?.Invoke($"Refresh completed: {message}");
            }
            else if (message.ToLower().Contains("error") || message.ToLower().Contains("failed"))
            {
                _logger.LogError("❌ UnifiedNotificationService: Refresh job FAILED - triggering ErrorOccurred event");
                ErrorOccurred?.Invoke($"Refresh failed: {message}");
            }
            else
            {
                // Regular progress update
                _logger.LogInformation("📊 UnifiedNotificationService: Refresh progress - triggering RefreshProgress event");
                RefreshProgress?.Invoke(message);
            }
        };
    }

    /// <summary>
    /// Parse job message from JSON or plain text
    /// </summary>
    private string ParseJobMessage(string resultText)
    {
        try
        {
            if (resultText.StartsWith("{") && resultText.EndsWith("}"))
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(resultText);
                if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString() ?? resultText;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to parse job message as JSON: {Error}", ex.Message);
        }
        
        return resultText;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            // Clear all event subscriptions
            UnsubscribeFromFileOperations();
            ConnectionStatusChanged = null;
            ErrorOccurred = null;
            JobStatusUpdated = null;
            ModelTrainingCompleted = null;
            RefreshProgress = null;
            
            // Clear low-level event subscriptions
            StockNotificationReceived = null;
            MoveFileNotificationReceived = null;
            JobNotificationReceived = null;
            JobNotificationWithStatsReceived = null;
            RefreshFilesNotificationReceived = null;
            
            // The underlying SignalRService will be disposed by DI container
            
            _logger.LogInformation("UnifiedNotificationService disposed");
        }
    }
}