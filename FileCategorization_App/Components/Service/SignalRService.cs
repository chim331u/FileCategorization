using Microsoft.AspNetCore.SignalR.Client;
using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;
using Radzen;

namespace FileCategorization_App.Components.Service;

public class SignalRService : ISignalRService
{
    private HubConnection? _hubConnection;
    private readonly ILogger<SignalRService> _logger;
    private readonly IUtilityServices _utilityServices;
    private readonly NotificationService _notificationService;
    private readonly IGlobalConsoleService _globalConsoleService;
    private bool _disposed = false;

    // Events
    public event Action<string, decimal>? StockNotificationReceived;
    public event Action<int, string, string, string, FileCategorization_Shared.Enums.MoveFilesResults>? MoveFileNotificationReceived;
    public event Action<string, FileCategorization_Shared.Enums.MoveFilesResults>? JobNotificationReceived;
    public event Action<string, FileCategorization_Shared.Enums.MoveFilesResults, int, int, int>? JobNotificationWithStatsReceived;
    public event Action<string, int>? RefreshFilesNotificationReceived;
    public event Action<string>? ConnectionEstablished;
    public event Action<string?>? ConnectionLost;
    public event Action<string>? ErrorOccurred;

    // Refresh job callbacks for direct method calling
    public Func<string, Task>? OnRefreshJobCompleted { get; set; }
    public Action<string>? OnRefreshJobFailed { get; set; }

    // Properties
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? ConnectionId => _hubConnection?.ConnectionId;

    public SignalRService(
        ILogger<SignalRService> logger,
        IUtilityServices utilityServices,
        NotificationService notificationService,
        IGlobalConsoleService globalConsoleService)
    {
        _logger = logger;
        _utilityServices = utilityServices;
        _notificationService = notificationService;
        _globalConsoleService = globalConsoleService;
        
        _logger.LogInformation("SignalR service initialized");
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
        {
            _logger.LogWarning("SignalR connection already exists. Current state: {State}", _hubConnection.State);
            return;
        }

        try
        {
            var baseUrl = _utilityServices.ApiUrl.TrimEnd('/') + "/";
            var hubUrl = baseUrl + "notifications";
            
            _logger.LogInformation("Initializing SignalR connection to {Url}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            // Configure event handlers
            SetupEventHandlers();

            // Start connection
            _logger.LogInformation("🔌 DEBUG: Attempting SignalR connection to: {Url}", hubUrl);
            
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("🔗 DEBUG: SignalR connection established. Connection ID: {ConnectionId}", _hubConnection.ConnectionId);
                
                // Show success notification
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "SignalR Connected",
                    Detail = $"Real-time notifications enabled. Connection ID: {_hubConnection.ConnectionId}",
                    Duration = 3000
                });
                
                ConnectionEstablished?.Invoke(_hubConnection.ConnectionId ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            
            // Show error notification
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "SignalR Connection Failed",
                Detail = $"Real-time notifications unavailable: {ex.Message}",
                Duration = 5000
            });
            
            ErrorOccurred?.Invoke($"SignalR Error connection: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                _logger.LogInformation("SignalR connection stopped");
                
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "SignalR Disconnected",
                    Detail = "Real-time notifications stopped",
                    Duration = 3000
                });
                
                ConnectionLost?.Invoke("Connection stopped manually");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection");
            }
        }
    }

    public async Task SendMessageAsync(string method, params object[] args)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync(method, args);
                _logger.LogDebug("Message sent via SignalR: {Method}", method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR message: {Method}", method);
                ErrorOccurred?.Invoke($"Error sending message: {ex.Message}");
            }
        }
        else
        {
            _logger.LogWarning("Cannot send message: SignalR not connected. Current state: {State}", 
                _hubConnection?.State ?? HubConnectionState.Disconnected);
        }
    }

    private void SetupEventHandlers()
    {
        if (_hubConnection == null) return;
        
        _logger.LogInformation("Setting up SignalR event handlers");

        // Stock notifications (legacy support)
        _hubConnection.On<string, decimal>("notifications", (stockName, stockPrice) =>
        {
            _logger.LogDebug("Stock notification received: {StockName} = {StockPrice}", stockName, stockPrice);
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Test Notification",
                Detail = $"Stock: {stockName} = {stockPrice}",
                Duration = 3000
            });
            
            StockNotificationReceived?.Invoke(stockName, stockPrice);
        });

        // File move notifications
        _hubConnection.On<int, string, string, string, FileCategorization_Shared.Enums.MoveFilesResults>("moveFilesNotifications", 
            (fileId, fileName, destinationPath, resultText, result) =>
        {
            _logger.LogInformation("File move notification: File {FileId} '{FileName}' → '{DestinationPath}' - {ResultText} - {Result}", 
                fileId, fileName, destinationPath, resultText, result);
            
            var severity = result switch
            {
                FileCategorization_Shared.Enums.MoveFilesResults.Moved => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Completed => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Failed => NotificationSeverity.Error,
                FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent => NotificationSeverity.Warning,
                FileCategorization_Shared.Enums.MoveFilesResults.Processing => NotificationSeverity.Info,
                _ => NotificationSeverity.Info
            };
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = severity,
                Summary = $"File Move: {result}",
                Detail = $"{fileName}: {resultText}",
                Duration = result == FileCategorization_Shared.Enums.MoveFilesResults.Failed ? 5000 : 3000
            });
            
            MoveFileNotificationReceived?.Invoke(fileId, fileName, destinationPath, resultText, result);
        });

        // Refresh files notifications
        _hubConnection.On<string, int>("refreshFilesNotifications", async (message, progress) =>
        {
            _logger.LogInformation("🔔 SignalR refreshFiles notification: {Message} - Progress: {Progress}", message, progress);
            
            // Format message for global console (remove redundant prefixes)
            string consoleMessage;
            if (message.StartsWith("Started refresh Files"))
            {
                consoleMessage = "Started refresh Files ...";
            }
            else if (message.StartsWith("Refresh completed. Total files processed:"))
            {
                // Extract just the number part
                var totalPart = message.Replace("Refresh completed. Total files processed:", "").Trim();
                consoleMessage = $"Refresh completed: Total files processed: {totalPart}";
            }
            else
            {
                consoleMessage = message;
            }
            
            // Add to global console only (no pop-up)
            _globalConsoleService.AddTimestampedMessage(consoleMessage);
            
            // Check if job completed (progress = 100 or contains "completed")
            if (progress >= 100 || message.ToLower().Contains("completed") || message.ToLower().Contains("finished"))
            {
                _logger.LogInformation("🎉 SignalR: Refresh job COMPLETED - calling Index.OnRefreshJobCompleted");
                
                // Call Index.razor method directly
                if (OnRefreshJobCompleted != null)
                {
                    try
                    {
                        await OnRefreshJobCompleted(consoleMessage);
                        _logger.LogInformation("✅ SignalR: Index.OnRefreshJobCompleted executed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("❌ SignalR: Error calling Index.OnRefreshJobCompleted: {Error}", ex.Message);
                        
                        // Add error to global console only (no pop-up)
                        _globalConsoleService.AddTimestampedMessage($"Refresh error: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ SignalR: OnRefreshJobCompleted callback not set");
                }
            }
            else if (message.ToLower().Contains("error") || message.ToLower().Contains("failed"))
            {
                _logger.LogError("❌ SignalR: Refresh job FAILED - calling Index.OnRefreshJobFailed");
                
                // Call Index.razor method directly
                if (OnRefreshJobFailed != null)
                {
                    try
                    {
                        OnRefreshJobFailed(consoleMessage);
                        _logger.LogInformation("✅ SignalR: Index.OnRefreshJobFailed executed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("❌ SignalR: Error calling Index.OnRefreshJobFailed: {Error}", ex.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ SignalR: OnRefreshJobFailed callback not set");
                }
            }
            
            // Still invoke the event for other subscribers
            RefreshFilesNotificationReceived?.Invoke(consoleMessage, progress);
        });

        // Job notifications without statistics (for training, categorization) - MUST BE FIRST
        _hubConnection.On<string, FileCategorization_Shared.Enums.MoveFilesResults>("jobNotifications", (resultText, result) =>
        {
            _logger.LogInformation("🔔 DEBUG: SignalR Job notification received: {ResultText} - {Result}", resultText, result);
            
            var severity = result switch
            {
                FileCategorization_Shared.Enums.MoveFilesResults.Moved => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Completed => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Failed => NotificationSeverity.Error,
                FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent => NotificationSeverity.Warning,
                FileCategorization_Shared.Enums.MoveFilesResults.Processing => NotificationSeverity.Info,
                _ => NotificationSeverity.Info
            };

            // Try to parse JSON for structured messages (like training results)
            string summary = "Job Notification";
            string detail = resultText;
            
            try
            {
                if (resultText.StartsWith("{") && resultText.EndsWith("}"))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(resultText);
                    if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        summary = result switch
                        {
                            FileCategorization_Shared.Enums.MoveFilesResults.Moved => "Job Completed - Files Moved",
                            FileCategorization_Shared.Enums.MoveFilesResults.Completed => "Job Completed",
                            FileCategorization_Shared.Enums.MoveFilesResults.Failed => "Job Failed",
                            FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent => "Job Error - ID Not Present",
                            FileCategorization_Shared.Enums.MoveFilesResults.Processing => "Job Processing",
                            _ => "Job Status Unknown"
                        };
                        detail = messageElement.GetString() ?? resultText;
                    }
                }
            }
            catch
            {
                // If JSON parsing fails, use original text
            }
            
            // Don't show toast for training/model messages
            bool isTrainingMessage = resultText.Contains("training", StringComparison.OrdinalIgnoreCase) ||
                                    resultText.Contains("model", StringComparison.OrdinalIgnoreCase);
            
            if (!isTrainingMessage)
            {
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = severity,
                    Summary = summary,
                    Detail = detail,
                    Duration = result == FileCategorization_Shared.Enums.MoveFilesResults.Failed ? 7000 : 5000
                });
            }
            
            JobNotificationReceived?.Invoke(resultText, result);
        });
        
        // Job notifications with statistics (move files job completion) - SECOND
        _hubConnection.On<string, FileCategorization_Shared.Enums.MoveFilesResults, int, int, int>("jobNotifications", 
            (resultText, result, totalFiles, successfulFiles, failedFiles) =>
        {
            _logger.LogInformation("Job notification with stats received: {ResultText} - {Result} - Total: {Total}, Success: {Success}, Failed: {Failed}", 
                resultText, result, totalFiles, successfulFiles, failedFiles);
            
            var severity = result switch
            {
                FileCategorization_Shared.Enums.MoveFilesResults.Moved => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Completed => NotificationSeverity.Success,
                FileCategorization_Shared.Enums.MoveFilesResults.Failed => NotificationSeverity.Error,
                FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent => NotificationSeverity.Warning,
                FileCategorization_Shared.Enums.MoveFilesResults.Processing => NotificationSeverity.Info,
                _ => NotificationSeverity.Info
            };
            
            string summary = result switch
            {
                FileCategorization_Shared.Enums.MoveFilesResults.Moved => "Files Moved Successfully",
                FileCategorization_Shared.Enums.MoveFilesResults.Completed => "Move Files Completed",
                FileCategorization_Shared.Enums.MoveFilesResults.Failed => "Move Files Failed",
                FileCategorization_Shared.Enums.MoveFilesResults.IdNotPresent => "Move Files - ID Not Present",
                FileCategorization_Shared.Enums.MoveFilesResults.Processing => "Move Files Processing",
                _ => "Move Files Status Unknown"
            };
            string detail = $"Total: {totalFiles}, Success: {successfulFiles}, Failed: {failedFiles}";
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = severity,
                Summary = summary,
                Detail = detail,
                Duration = 5000
            });
            
            JobNotificationWithStatsReceived?.Invoke(resultText, result, totalFiles, successfulFiles, failedFiles);
        });

        // Connection lifecycle events
        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed. Error: {Error}", error?.Message);
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "SignalR Disconnected",
                Detail = error?.Message ?? "Connection closed",
                Duration = 4000
            });
            
            ConnectionLost?.Invoke(error?.Message);
            
            // Attempt to reconnect after a delay
            await Task.Delay(Random.Shared.Next(0, 5) * 1000);
            try
            {
                await StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect SignalR");
            }
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected. New Connection ID: {ConnectionId}", connectionId);
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "SignalR Reconnected",
                Detail = $"Real-time notifications restored. ID: {connectionId}",
                Duration = 3000
            });
            
            ConnectionEstablished?.Invoke(connectionId ?? "Unknown");
        };

        _hubConnection.Reconnecting += (error) =>
        {
            _logger.LogWarning("SignalR reconnecting. Error: {Error}", error?.Message);
            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "SignalR Reconnecting",
                Detail = "Attempting to restore real-time notifications...",
                Duration = 3000
            });
            
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing SignalR connection");
                }
                finally
                {
                    _hubConnection = null;
                }
            }

            _logger.LogInformation("SignalR service disposed");
        }
    }
}