using Microsoft.AspNetCore.SignalR;

namespace LingapDVO.Hubs
{
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Send notification to a specific user by userId
        /// </summary>
        public async Task SendNotificationToUser(int userId, string title, string message, string type, string link = null)
        {
            await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                type = type,
                link = link,
                createdAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Send notification to all connected clients
        /// </summary>
        public async Task SendNotificationToAll(string title, string message, string type, string link = null)
        {
            await Clients.All.SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                type = type,
                link = link,
                createdAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Get the userId from the connection (if authenticated)
            var userId = Context.User?.FindFirst("UserId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add to a group with their userId for targeted messaging
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
