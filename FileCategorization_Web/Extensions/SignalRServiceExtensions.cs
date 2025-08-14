using FileCategorization_Web.Services.SignalR;

namespace FileCategorization_Web.Extensions;

public static class SignalRServiceExtensions
{
    public static IServiceCollection AddSignalRNotifications(this IServiceCollection services)
    {
        // Register as singleton to maintain connection across the application
        services.AddSingleton<INotificationService, SignalRNotificationService>();
        
        return services;
    }
}