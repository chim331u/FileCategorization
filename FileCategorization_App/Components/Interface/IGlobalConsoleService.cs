using System.Collections.ObjectModel;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Interface for managing global console messages across the application
/// </summary>
public interface IGlobalConsoleService
{
    /// <summary>
    /// Gets the current console messages
    /// </summary>
    ReadOnlyObservableCollection<string> Messages { get; }
    
    /// <summary>
    /// Adds a new message to the console
    /// </summary>
    void AddMessage(string message);
    
    /// <summary>
    /// Adds a timestamped message to the console
    /// </summary>
    void AddTimestampedMessage(string message);
    
    /// <summary>
    /// Clears all console messages
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets the last N messages formatted as a single string
    /// </summary>
    string GetFormattedMessages(int maxMessages = 50);
    
    /// <summary>
    /// Event raised when messages change
    /// </summary>
    event EventHandler? MessagesChanged;
}