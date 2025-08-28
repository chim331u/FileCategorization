using FileCategorization_App.Components.Interface;
using FileCategorization_App.Components.Service;
using Microsoft.Extensions.Logging;
using Radzen;
using Serilog;

namespace FileCategorization_App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        //builder.Services.AddScoped<SessionStorageAccessor>();
        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddScoped<IUtilityServices, UtilityServices>();
        builder.Services.AddScoped<IServiceApi, ServiceApi>();
        builder.Services.AddSingleton<IHttpsClientHandlerService, HttpsClientHandlerService>();
        builder.Services.AddSingleton<IDDwebService, DDwebService>();

        var _cachePath = FileSystem.Current.CacheDirectory;

        var _appData = FileSystem.AppDataDirectory;

        var logFileName = Path.Combine(_cachePath, "serilog_.log");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Debug(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} {Level:u3} {Username} {Message:lj}{Exception}{NewLine}")
            .WriteTo.File(logFileName, rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} {Level:u3} {Username} {Message:lj}{Exception}{NewLine}")
            .CreateLogger();

        builder.Services.AddLogging(logging =>
        {
            logging.AddSerilog(dispose: true);
        });
        return builder.Build();
    }
}