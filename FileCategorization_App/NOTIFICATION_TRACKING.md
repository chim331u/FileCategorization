# 📢 Sistema di Tracking Notifiche TOAST

Questo documento descrive il sistema implementato per monitorare e tracciare tutte le notifiche TOAST nell'applicazione mobile FileCategorization_App.

## 🎯 Panoramica

Il sistema di tracking notifiche fornisce:
- **Tracciamento automatico** di tutte le notifiche TOAST
- **Identificazione dell'origine** (pagina/servizio che ha generato la notifica)
- **Evento scatenante** (metodo che ha attivato la notifica)
- **Resoconto dettagliato** con statistiche e filtri
- **Esportazione dati** in formato JSON
- **Modalità live** per monitoraggio in tempo reale

## 🏗️ Architettura

### Componenti Principali

#### 1. INotificationTrackingService
```csharp
// Interfaccia per il servizio di tracking
public interface INotificationTrackingService
{
    void TrackNotification(string message, NotificationSeverity severity, string source, string triggerEvent, string? additionalData = null);
    List<NotificationTrackingEntry> GetAllNotifications();
    NotificationStatistics GetNotificationStatistics();
}
```

#### 2. NotificationTrackingService
- **Implementazione thread-safe** del servizio di tracking
- **Storage in-memory** con auto-pulizia
- **Eventi real-time** per notificare nuove tracce
- **Statistiche automatiche** e aggregazioni

#### 3. TrackedNotificationService
- **Wrapper trasparente** per NotificationService di Radzen
- **Tracking automatico** usando `[CallerMemberName]` e `[CallerFilePath]`
- **Metodi helper** per i diversi tipi di notifica
- **Identificazione automatica** della sorgente

#### 4. NotificationReportPage
- **Dashboard completa** per visualizzare tutte le notifiche
- **Filtri avanzati** per periodo, severità e sorgente
- **Modalità live** con aggiornamento automatico
- **Export JSON** per analisi esterne
- **Statistiche real-time**

## 📋 Modello Dati

### NotificationTrackingEntry
```csharp
public class NotificationTrackingEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
    public NotificationSeverity Severity { get; set; }
    public string Source { get; set; }           // Es: "Page.Settings.Setting"
    public string TriggerEvent { get; set; }     // Es: "Page.Settings.Setting.SaveSetting"
    public string? AdditionalData { get; set; }
}
```

### NotificationStatistics
```csharp
public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int InfoCount, SuccessCount, WarningCount, ErrorCount { get; set; }
    public List<string> TopSources { get; set; }
    public List<string> TopTriggerEvents { get; set; }
    public double NotificationsPerHour { get; set; }
}
```

## 🔧 Utilizzo

### 1. Notifiche Tracciate Automaticamente

**Metodo Normale** (NotificationService):
```csharp
NotificationService.Notify(new NotificationMessage
{
    Severity = NotificationSeverity.Success,
    Summary = "Operazione completata",
    Detail = "Il file è stato salvato correttamente"
});
```

**Metodo Tracciato** (TrackedNotificationService):
```csharp
TrackedNotification.NotifySuccess("Operazione completata", "Il file è stato salvato correttamente");
// Automatically tracked: Source="Page.Settings.Setting", TriggerEvent="SaveSetting"
```

### 2. Accesso alla Dashboard

1. Navigare a `/notification-report` nell'app
2. Oppure usare il menu: **Notifiche TOAST**

### 3. Funzionalità Dashboard

#### Filtri Disponibili
- **Periodo**: Ultimi 5min, 30min, 1h, 6h, Oggi, Tutto
- **Severità**: Info, Successo, Avviso, Errore
- **Sorgente**: Filtra per pagina/servizio specifico

#### Modalità Live
- Attiva con il pulsante **⚡ Live**
- Aggiornamento automatico ogni 5 secondi
- Mostra notifiche in tempo reale

#### Esportazione
- **📄 Esporta JSON**: Salva tutte le notifiche in formato JSON
- File salvato in: `{Device.CacheDirectory}/notifications_export_{timestamp}.json`

## 📊 Esempi di Tracking

### Esempio 1: Salvataggio Impostazioni
```
Timestamp: 15:30:45 31/08/2025
Severity: Success
Source: Page.Settings.Setting  
TriggerEvent: Page.Settings.Setting.SaveSetting
Message: Setting saved
AdditionalData: Setting saved in json file
```

### Esempio 2: Errore di Rete
```
Timestamp: 15:32:10 31/08/2025
Severity: Error
Source: Service.ServiceApi
TriggerEvent: Service.ServiceApi.GetFilesAsync  
Message: Network error
AdditionalData: Failed to connect to server
```

## 🔍 Identificazione Automatica Sorgente

Il sistema identifica automaticamente la sorgente basandosi sul percorso del file:

- `Components/Pages/Settings/Setting.razor` → `"Page.Settings.Setting"`
- `Components/Service/ServiceApi.cs` → `"Service.ServiceApi"`  
- `Components/Layout/MainLayout.razor` → `"Layout.MainLayout"`

## 📈 Statistiche Disponibili

- **Contatori per Severità**: Info, Successo, Avvisi, Errori
- **Top 5 Sorgenti**: Pagine/servizi che generano più notifiche
- **Top 5 Eventi**: Metodi che attivano più notifiche
- **Frequenza**: Notifiche per ora
- **Periodo**: Prima e ultima notifica registrata

## 🛠️ Configurazione

### Registrazione Servizi (MauiProgram.cs)
```csharp
// Notification tracking services
builder.Services.AddSingleton<INotificationTrackingService, NotificationTrackingService>();
builder.Services.AddScoped<TrackedNotificationService>();
```

### Injection Globale (_Imports.razor)
```csharp
@inject TrackedNotificationService TrackedNotification
```

## 🧪 Test e Debug

### Test Notifica
La pagina dashboard include un pulsante **🧪 Test Notifica** che genera notifiche casuali per testare il sistema.

### Pulizia Automatica
- Manuale: Pulsante **🗑️ Pulisci Vecchie** (24h)
- Automatica: Quando si superano 1000 notifiche (12h)

## 📋 Benefici

1. **Debugging Facilitato**: Tracciamento completo dell'origine delle notifiche
2. **Monitoring UX**: Comprensione dei pattern di utilizzo dell'app  
3. **Quality Assurance**: Identificazione di notifiche eccessive o problematiche
4. **Analytics**: Dati per migliorare l'esperienza utente
5. **Zero Configuration**: Tracking automatico senza modifiche al codice esistente

## 🚀 Estensioni Future

- [ ] Persistence su database locale
- [ ] Integrazione con analytics esterni
- [ ] Alert per soglie di errore
- [ ] Raggruppamento notifiche simili
- [ ] Dashboard web condivisa
- [ ] Notifiche push per eventi critici