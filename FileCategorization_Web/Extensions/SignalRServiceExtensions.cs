using FileCategorization_Web.Services.SignalR;

namespace FileCategorization_Web.Extensions;

public static class SignalRServiceExtensions
{
    public static IServiceCollection AddSignalRNotifications(this IServiceCollection services)
    {
        // Register as scoped to allow injection of scoped services like IDispatcher
        services.AddScoped<INotificationService, SignalRNotificationService>();
        
        return services;
    }
}