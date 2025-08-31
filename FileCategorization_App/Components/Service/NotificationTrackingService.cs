using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;
using Radzen;
using System.Text.Json;
using System.Collections.Concurrent;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Servizio per tracciare e monitorare le notifiche TOAST nell'applicazione mobile
/// </summary>
public class NotificationTrackingService : INotificationTrackingService
{
    private readonly ConcurrentList<NotificationTrackingEntry> _notifications = new();
    private readonly ILogger<NotificationTrackingService> _logger;
    
    public event Action<NotificationTrackingEntry>? NotificationTracked;

    public NotificationTrackingService(ILogger<NotificationTrackingService> logger)
    {
        _logger = logger;
        _logger.LogInformation("NotificationTrackingService inizializzato");
    }

    public void TrackNotification(string message, NotificationSeverity severity, string source, string triggerEvent, string? additionalData = null)
    {
        var entry = new NotificationTrackingEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Severity = severity,
            Source = source,
            TriggerEvent = triggerEvent,
            AdditionalData = additionalData
        };

        _notifications.Add(entry);
        
        _logger.LogInformation("📢 Notifica tracciata - Sorgente: {Source}, Evento: {TriggerEvent}, Severità: {Severity}, Messaggio: {Message}", 
            source, triggerEvent, severity, message);

        // Pulisci automaticamente le notifiche vecchie se sono troppe
        if (_notifications.Count > 1000)
        {
            CleanOldNotifications(12); // Mantieni solo le ultime 12 ore
        }

        // Notifica i listener
        NotificationTracked?.Invoke(entry);
    }

    public List<NotificationTrackingEntry> GetAllNotifications()
    {
        return _notifications.OrderByDescending(x => x.Timestamp).ToList();
    }

    public List<NotificationTrackingEntry> GetNotificationsBySource(string source)
    {
        return _notifications
            .Where(x => x.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public List<NotificationTrackingEntry> GetNotificationsBySeverity(NotificationSeverity severity)
    {
        return _notifications
            .Where(x => x.Severity == severity)
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public List<NotificationTrackingEntry> GetRecentNotifications(int minutes = 30)
    {
        var cutoffTime = DateTime.Now.AddMinutes(-minutes);
        return _notifications
            .Where(x => x.Timestamp >= cutoffTime)
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public void CleanOldNotifications(int hours = 24)
    {
        var cutoffTime = DateTime.Now.AddHours(-hours);
        var oldCount = _notifications.Count;
        
        // Rimuovi le notifiche vecchie
        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            if (_notifications[i].Timestamp < cutoffTime)
            {
                _notifications.RemoveAt(i);
            }
        }

        var removedCount = oldCount - _notifications.Count;
        if (removedCount > 0)
        {
            _logger.LogInformation("🗑️ Pulizia notifiche: rimosse {RemovedCount} notifiche più vecchie di {Hours} ore", removedCount, hours);
        }
    }

    public string ExportNotificationsAsJson()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(GetAllNotifications(), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'esportazione JSON delle notifiche");
            return "[]";
        }
    }

    public NotificationStatistics GetNotificationStatistics()
    {
        var allNotifications = _notifications.ToList();
        
        if (allNotifications.Count == 0)
        {
            return new NotificationStatistics();
        }

        var stats = new NotificationStatistics
        {
            TotalNotifications = allNotifications.Count,
            InfoCount = allNotifications.Count(x => x.Severity == NotificationSeverity.Info),
            SuccessCount = allNotifications.Count(x => x.Severity == NotificationSeverity.Success),
            WarningCount = allNotifications.Count(x => x.Severity == NotificationSeverity.Warning),
            ErrorCount = allNotifications.Count(x => x.Severity == NotificationSeverity.Error),
            FirstNotification = allNotifications.Min(x => x.Timestamp),
            LastNotification = allNotifications.Max(x => x.Timestamp)
        };

        // Top sources
        stats.TopSources = allNotifications
            .GroupBy(x => x.Source)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        // Top trigger events
        stats.TopTriggerEvents = allNotifications
            .GroupBy(x => x.TriggerEvent)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        // Notifiche per ora
        if (stats.FirstNotification.HasValue && stats.LastNotification.HasValue)
        {
            var timeSpan = stats.LastNotification.Value - stats.FirstNotification.Value;
            if (timeSpan.TotalHours > 0)
            {
                stats.NotificationsPerHour = Math.Round(stats.TotalNotifications / timeSpan.TotalHours, 2);
            }
        }

        return stats;
    }
}

/// <summary>
/// Thread-safe list implementation per le notifiche
/// </summary>
public class ConcurrentList<T> : IEnumerable<T>
{
    private readonly List<T> _list = new();
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                return _list[index];
            }
        }
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _list.Add(item);
        }
    }

    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _list.Count)
            {
                _list.RemoveAt(index);
            }
        }
    }

    public List<T> ToList()
    {
        lock (_lock)
        {
            return new List<T>(_list);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return new List<T>(_list).GetEnumerator();
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}