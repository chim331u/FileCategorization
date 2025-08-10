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
            _logger.LogInformation($"Signalr client connected with id: {connectionId}");
            await base.OnConnectedAsync();
        }
    }
}