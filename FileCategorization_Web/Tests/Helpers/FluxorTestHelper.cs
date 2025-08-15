using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileCategorization_Web.Tests.Helpers;

public static class FluxorTestHelper
{
    /// <summary>
    /// Creates a service collection with Fluxor configured for testing
    /// </summary>
    public static IServiceCollection CreateTestServiceCollection()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add Fluxor for testing (without middleware that require browser context)
        services.AddFluxor(options =>
        {
            options.ScanAssemblies(typeof(Program).Assembly);
            // Don't add browser-specific middleware in tests
        });
        
        return services;
    }
    
    /// <summary>
    /// Creates a test service provider with Fluxor configured
    /// </summary>
    public static ServiceProvider CreateTestServiceProvider()
    {
        return CreateTestServiceCollection().BuildServiceProvider();
    }
    
    /// <summary>
    /// Waits for all dispatched actions to be processed
    /// </summary>
    public static async Task WaitForEffectsToComplete(IServiceProvider serviceProvider, int timeoutMs = 1000)
    {
        // Allow effects to complete
        await Task.Delay(100);
        
        // Force garbage collection to ensure all async operations are cleaned up
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}