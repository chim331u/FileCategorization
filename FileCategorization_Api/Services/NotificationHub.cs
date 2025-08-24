using Microsoft.AspNetCore.SignalR;

namespace FileCategorization_Api.Services
{
    public class NotificationHub : Hub
    {
        //public async Task SendStockPrice(string stockName, decimal price)
        //{
        //    await Clients.All.SendAsync("notifications", stockName, price);
        //}
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            _logger.LogInformation("🔗 DEBUG: SignalR client connected with ID: {ConnectionId}", connectionId);
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogWarning("🔌 DEBUG: SignalR client disconnected. ID: {ConnectionId}, Error: {Error}", 
                Context.ConnectionId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }
    }
}