using FileCategorization_Web.Services.Caching;
using FileCategorization_Web.Features.FileManagement.Store;
using Fluxor;

namespace FileCategorization_Web.Extensions;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        // Add memory cache from framework
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100_000_000; // 100MB limit
            options.CompactionPercentage = 0.25; // Remove 25% when limit reached
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Scan every 5 minutes
        });

        // Register base cache service
        services.AddScoped<MemoryCacheService>();
        
        // Register state-aware cache service with explicit dependencies
        services.AddScoped<StateAwareCacheService>(provider =>
        {
            var baseCacheService = provider.GetRequiredService<MemoryCacheService>();
            var state = provider.GetRequiredService<IState<FileState>>();
            var logger = provider.GetRequiredService<ILogger<StateAwareCacheService>>();
            return new StateAwareCacheService(baseCacheService, state, logger);
        });
        
        services.AddScoped<IStateAwareCacheService>(provider => provider.GetRequiredService<StateAwareCacheService>());
        
        // Register state-aware cache service as primary ICacheService interface
        services.AddScoped<ICacheService>(provider => provider.GetRequiredService<StateAwareCacheService>());

        return services;
    }

    public static IServiceCollection AddCachingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Add cache-specific configuration if needed
        services.Configure<CachingOptions>(configuration.GetSection("Caching"));
        
        return services;
    }
}

public class CachingOptions
{
    public bool EnableCaching { get; set; } = true;
    public int DefaultExpirationMinutes { get; set; } = 15;
    public long MaxCacheSizeBytes { get; set; } = 100_000_000; // 100MB
    public bool EnableStatistics { get; set; } = true;
    public bool EnableWarmup { get; set; } = true;
    public List<string> DisabledCacheKeys { get; set; } = new();
}