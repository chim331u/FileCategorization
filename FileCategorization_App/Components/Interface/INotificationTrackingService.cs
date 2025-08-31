using Radzen;

namespace FileCategorization_App.Components.Interface;

/// <summary>
/// Service per tracciare le notifiche TOAST e i loro eventi scatenanti
/// </summary>
public interface INotificationTrackingService
{
    /// <summary>
    /// Event raised when a new notification is tracked
    /// </summary>
    event Action<NotificationTrackingEntry>? NotificationTracked;
    
    /// <summary>
    /// Traccia una notificazione TOAST
    /// </summary>
    /// <param name="message">Messaggio della notificazione</param>
    /// <param name="severity">Severità della notificazione</param>
    /// <param name="source">Sorgente dell'evento (pagina/servizio)</param>
    /// <param name="triggerEvent">Evento scatenante</param>
    /// <param name="additionalData">Dati aggiuntivi opzionali</param>
    void TrackNotification(string message, NotificationSeverity severity, string source, string triggerEvent, string? additionalData = null);
    
    /// <summary>
    /// Ottiene tutte le notifiche tracciate
    /// </summary>
    List<NotificationTrackingEntry> GetAllNotifications();
    
    /// <summary>
    /// Ottiene le notifiche filtrate per sorgente
    /// </summary>
    List<NotificationTrackingEntry> GetNotificationsBySource(string source);
    
    /// <summary>
    /// Ottiene le notifiche filtrate per severità
    /// </summary>
    List<NotificationTrackingEntry> GetNotificationsBySeverity(NotificationSeverity severity);
    
    /// <summary>
    /// Ottiene le notifiche degli ultimi N minuti
    /// </summary>
    List<NotificationTrackingEntry> GetRecentNotifications(int minutes = 30);
    
    /// <summary>
    /// Pulisce le notifiche più vecchie di N ore
    /// </summary>
    void CleanOldNotifications(int hours = 24);
    
    /// <summary>
    /// Esporta le notifiche come stringa JSON
    /// </summary>
    string ExportNotificationsAsJson();
    
    /// <summary>
    /// Ottiene statistiche sulle notifiche
    /// </summary>
    NotificationStatistics GetNotificationStatistics();
}

/// <summary>
/// Entry per una notificazione tracciata
/// </summary>
public class NotificationTrackingEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public string Source { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string? AdditionalData { get; set; }
    public string SeverityText => Severity.ToString();
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss dd/MM/yyyy");
}

/// <summary>
/// Statistiche delle notifiche
/// </summary>
public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int InfoCount { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> TopSources { get; set; } = new();
    public List<string> TopTriggerEvents { get; set; } = new();
    public DateTime? FirstNotification { get; set; }
    public DateTime? LastNotification { get; set; }
    public double NotificationsPerHour { get; set; }
}