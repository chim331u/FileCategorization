using FileCategorization_Web.Data.Configuration;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services;

namespace FileCategorization_Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileCategorizationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Check if modern configuration exists
        var apiOptions = configuration.GetSection(FileCategorizationApiOptions.SectionName).Get<FileCategorizationApiOptions>();
        
        if (apiOptions != null && !string.IsNullOrEmpty(apiOptions.BaseUrl))
        {
            // Use modern service with HttpClientFactory and Polly
            services.Configure<FileCategorizationApiOptions>(
                configuration.GetSection(FileCategorizationApiOptions.SectionName));

            services.AddHttpClient<IFileCategorizationService, ModernFileCategorizationService>(client =>
            {
                client.BaseAddress = new Uri(apiOptions.BaseUrl);
                client.Timeout = apiOptions.Timeout;
            });
            // Note: Polly policies disabled for WebAssembly compatibility
        }
        else
        {
            // Fallback to legacy service with adapter
            services.AddScoped<ILegacyFileCategorizationService, FileCategorizationServices>();
            services.AddScoped<IFileCategorizationService, LegacyServiceAdapter>();
        }

        // Add WebScrum services with modern/legacy support
        if (apiOptions != null && !string.IsNullOrEmpty(apiOptions.BaseUrl))
        {
            // Register modern WebScrum service
            services.AddHttpClient<ModernWebScrumService>(client =>
            {
                client.BaseAddress = new Uri(apiOptions.BaseUrl);
                client.Timeout = apiOptions.Timeout;
            });
        }
        
        // Register legacy service
        services.AddScoped<WebScrumServices>();
        
        // Register adapter that selects appropriate implementation
        services.AddScoped<IWebScrumServices, WebScrumServiceAdapter>();

        return services;
    }
}