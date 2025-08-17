using FileCategorization_Web.Data.Configuration;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services;

namespace FileCategorization_Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileCategorizationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Modern configuration is now required - no legacy fallback
        var apiOptions = configuration.GetSection(FileCategorizationApiOptions.SectionName).Get<FileCategorizationApiOptions>();
        
        if (apiOptions == null || string.IsNullOrEmpty(apiOptions.BaseUrl))
        {
            throw new InvalidOperationException("FileCategorizationApi configuration is required. Please configure the BaseUrl in appsettings.json");
        }

        // Configure modern API options
        services.Configure<FileCategorizationApiOptions>(
            configuration.GetSection(FileCategorizationApiOptions.SectionName));

        // Register modern service with HttpClientFactory and interface
        services.AddHttpClient<ModernFileCategorizationService>();
        
        // Register interface mapping for clean DI
        services.AddScoped<IModernFileCategorizationService, ModernFileCategorizationService>();

        // Add WebScrum services with modern architecture
        services.AddHttpClient<ModernWebScrumService>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseUrl);
            client.Timeout = apiOptions.Timeout;
        });
        
        // Register legacy service for backward compatibility until WebScrum is fully migrated
        services.AddScoped<WebScrumServices>();
        
        // Register adapter that selects appropriate implementation
        services.AddScoped<IWebScrumServices, WebScrumServiceAdapter>();

        return services;
    }
}