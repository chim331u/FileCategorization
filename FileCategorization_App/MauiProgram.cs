using FileCategorization_App.Components.Interface;
using FileCategorization_App.Components.Service;
using Microsoft.Extensions.Caching.Memory;
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

        // Core services
        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddScoped<IUtilityServices, UtilityServices>();
        
        // Memory cache infrastructure
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<ICacheService, MemoryCacheService>();
        
        // Register base services
        builder.Services.AddScoped<ServiceApi>();
        builder.Services.AddSingleton<DDwebService>();
        builder.Services.AddSingleton<IHttpsClientHandlerService, HttpsClientHandlerService>();
        
        // Register cached wrapper services as the interface implementations
        builder.Services.AddScoped<IServiceApi>(provider =>
        {
            var baseService = provider.GetRequiredService<ServiceApi>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedServiceApiWrapper>>();
            return new CachedServiceApiWrapper(baseService, cacheService, logger);
        });
        
        builder.Services.AddScoped<IDDwebService>(provider =>
        {
            var baseService = provider.GetRequiredService<DDwebService>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedDDwebServiceWrapper>>();
            return new CachedDDwebServiceWrapper(baseService, cacheService, logger);
        });

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