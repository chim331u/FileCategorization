using Microsoft.AspNetCore.SignalR.Client;
using FileCategorization_Shared.DTOs.FileManagement;using FileCategorization_Shared.DTOs.Configuration;using FileCategorization_Shared.Enums;
using FileCategorization_Web.Features.FileManagement.Actions;
using Fluxor;

namespace FileCategorization_Web.Services.SignalR;

public class SignalRNotificationService : INotificationService
{
    private HubConnection? _hubConnection;
    private readonly ILogger<SignalRNotificationService> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly string _baseUrl;
    private bool _disposed = false;

    // Events
    public event Action<string, decimal>? StockNotificationReceived;
    public event Action<int, string, MoveFilesResults>? MoveFileNotificationReceived;
    public event Action<string, MoveFilesResults>? JobNotificationReceived;
    public event Action<string>? ConnectionEstablished;
    public event Action<string?>? ConnectionLost;
    public event Action<string>? ErrorOccurred;

    // Properties
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? ConnectionId => _hubConnection?.ConnectionId;

    public SignalRNotificationService(
        ILogger<SignalRNotificationService> logger,
        IDispatcher dispatcher,
        IConfiguration configuration)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _baseUrl = configuration["Uri"] ?? configuration["FileCategorizationApi:BaseUrl"] ?? "http://localhost:5000/";
        
        _logger.LogInformation("SignalR service initialized with base URL: {BaseUrl}", _baseUrl);
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
            _logger.LogInformation("Initializing SignalR connection to {Url}", _baseUrl + "notifications");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_baseUrl + "notifications")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            // Configure event handlers
            SetupEventHandlers();

            // Start connection
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection established. Connection ID: {ConnectionId}", _hubConnection.ConnectionId);
                
                // Dispatch Fluxor action and raise event
                _dispatcher.Dispatch(new SignalRConnectedAction(_hubConnection.ConnectionId ?? "Unknown"));
                ConnectionEstablished?.Invoke(_hubConnection.ConnectionId ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            _dispatcher.Dispatch(new AddConsoleMessageAction($"SignalR Error connection: {ex.Message}"));
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
                
                _dispatcher.Dispatch(new SignalRDisconnectedAction());
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

        // Stock notifications (legacy support)
        _hubConnection.On<string, decimal>("notifications", (stockName, stockPrice) =>
        {
            _logger.LogDebug("Stock notification received: {StockName} = {StockPrice}", stockName, stockPrice);
            
            _dispatcher.Dispatch(new AddConsoleMessageAction($"Message received--> Stock Name: {stockName} Stock Price: {stockPrice}"));
            StockNotificationReceived?.Invoke(stockName, stockPrice);
        });

        // File move notifications
        _hubConnection.On<int, string, MoveFilesResults>("moveFilesNotifications", (fileId, resultText, result) =>
        {
            _logger.LogInformation("File move notification: File {FileId} - {ResultText} - {Result}", fileId, resultText, result);
            
            _dispatcher.Dispatch(new SignalRFileMovedAction(fileId, resultText, result));
            MoveFileNotificationReceived?.Invoke(fileId, resultText, result);
        });

        // Job notifications  
        _hubConnection.On<string, MoveFilesResults>("jobNotifications", (resultText, result) =>
        {
            _logger.LogInformation("Job notification received: {ResultText} - {Result}", resultText, result);
            
            _dispatcher.Dispatch(new SignalRJobCompletedAction(resultText, result));
            JobNotificationReceived?.Invoke(resultText, result);
        });

        // Connection lifecycle events
        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed. Error: {Error}", error?.Message);
            _dispatcher.Dispatch(new SignalRDisconnectedAction());
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
            _dispatcher.Dispatch(new SignalRConnectedAction(connectionId ?? "Unknown"));
            ConnectionEstablished?.Invoke(connectionId ?? "Unknown");
        };

        _hubConnection.Reconnecting += (error) =>
        {
            _logger.LogWarning("SignalR reconnecting. Error: {Error}", error?.Message);
            _dispatcher.Dispatch(new AddConsoleMessageAction("SignalR - Reconnecting..."));
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