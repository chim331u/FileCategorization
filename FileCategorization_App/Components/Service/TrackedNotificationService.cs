using FileCategorization_App.Components.Interface;
using Microsoft.Extensions.Logging;
using Radzen;
using System.Runtime.CompilerServices;

namespace FileCategorization_App.Components.Service;

/// <summary>
/// Wrapper per NotificationService di Radzen che traccia automaticamente tutte le notifiche
/// </summary>
public class TrackedNotificationService
{
    private readonly NotificationService _notificationService;
    private readonly INotificationTrackingService _trackingService;
    private readonly ILogger<TrackedNotificationService> _logger;

    public TrackedNotificationService(
        NotificationService notificationService,
        INotificationTrackingService trackingService,
        ILogger<TrackedNotificationService> logger)
    {
        _notificationService = notificationService;
        _trackingService = trackingService;
        _logger = logger;
    }

    /// <summary>
    /// Mostra una notificazione con tracking automatico
    /// </summary>
    public void Notify(NotificationMessage message, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "")
    {
        // Estrai il nome della classe dal file path
        var source = ExtractSourceFromFilePath(callerFilePath);
        var triggerEvent = $"{source}.{callerMethod}";

        // Traccia la notificazione prima di mostrarla
        _trackingService.TrackNotification(
            message: message.Summary ?? message.Detail ?? "Notifica senza testo",
            severity: message.Severity,
            source: source,
            triggerEvent: triggerEvent,
            additionalData: message.Detail
        );

        // Mostra la notificazione originale
        _notificationService.Notify(message);

        _logger.LogDebug("Notificazione mostrata e tracciata - {Source}.{Method}: {Message}", 
            source, callerMethod, message.Summary ?? message.Detail);
    }

    /// <summary>
    /// Metodo helper per mostrare notifiche Success con tracking
    /// </summary>
    public void NotifySuccess(string message, string? detail = null, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "")
    {
        Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = message,
            Detail = detail,
            Duration = 4000
        }, callerMethod, callerFilePath);
    }

    /// <summary>
    /// Metodo helper per mostrare notifiche Info con tracking
    /// </summary>
    public void NotifyInfo(string message, string? detail = null, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "")
    {
        Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = message,
            Detail = detail,
            Duration = 4000
        }, callerMethod, callerFilePath);
    }

    /// <summary>
    /// Metodo helper per mostrare notifiche Warning con tracking
    /// </summary>
    public void NotifyWarning(string message, string? detail = null, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "")
    {
        Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = message,
            Detail = detail,
            Duration = 6000
        }, callerMethod, callerFilePath);
    }

    /// <summary>
    /// Metodo helper per mostrare notifiche Error con tracking
    /// </summary>
    public void NotifyError(string message, string? detail = null, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "")
    {
        Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = message,
            Detail = detail,
            Duration = 8000
        }, callerMethod, callerFilePath);
    }

    /// <summary>
    /// Estrae il nome della classe/componente dal file path
    /// </summary>
    private string ExtractSourceFromFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "Unknown";

        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Se il file è in una pagina, estrai il nome della pagina
            if (filePath.Contains("Components/Pages/"))
            {
                var parts = filePath.Split('/');
                var pageIndex = Array.IndexOf(parts, "Pages");
                if (pageIndex >= 0 && pageIndex < parts.Length - 1)
                {
                    return $"Page.{parts[pageIndex + 1]}.{fileName}";
                }
            }
            
            // Se il file è un servizio
            if (filePath.Contains("Components/Service/"))
            {
                return $"Service.{fileName}";
            }
            
            // Se il file è un layout
            if (filePath.Contains("Components/Layout/"))
            {
                return $"Layout.{fileName}";
            }
            
            return fileName ?? "Unknown";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }
}