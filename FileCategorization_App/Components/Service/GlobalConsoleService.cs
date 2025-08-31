using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Service for managing global console messages across the application
/// Similar to WEB project's Fluxor ConsoleMessages but for MAUI without state management
/// </summary>
public class GlobalConsoleService : IGlobalConsoleService
{
    private readonly ObservableCollection<string> _messages = new();
    private readonly ILogger<GlobalConsoleService> _logger;
    private readonly object _lock = new();

    public ReadOnlyObservableCollection<string> Messages { get; }

    public event EventHandler? MessagesChanged;

    public GlobalConsoleService(ILogger<GlobalConsoleService> logger)
    {
        _logger = logger;
        Messages = new ReadOnlyObservableCollection<string>(_messages);
        
        // Log service initialization
        _logger.LogInformation("GlobalConsoleService initialized");
        AddTimestampedMessage("Global console service initialized");
    }

    public void AddMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        lock (_lock)
        {
            _messages.Add(message);
            
            // Limit to last 100 messages to prevent memory issues
            while (_messages.Count > 100)
            {
                _messages.RemoveAt(0);
            }
        }

        _logger.LogDebug("Console message added: {Message}", message);
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddTimestampedMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        var timestampedMessage = $"{DateTime.Now:G} - {message}";
        AddMessage(timestampedMessage);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }

        _logger.LogInformation("Console messages cleared");
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetFormattedMessages(int maxMessages = 50)
    {
        lock (_lock)
        {
            var messagesToShow = _messages.TakeLast(maxMessages).Reverse();
            return string.Join("\n", messagesToShow);
        }
    }
}