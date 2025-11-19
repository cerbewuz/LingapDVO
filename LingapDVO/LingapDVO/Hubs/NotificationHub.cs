using LingapDVO.Services;
using Microsoft.AspNetCore.SignalR;

namespace LingapDVO.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IDateTimeService _dateTimeService;

        public NotificationHub(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;
        }

        /// <summary>
        /// Send notification to a specific user by userId
        /// </summary>
        public async Task SendNotificationToUser(int userId, string title, string message, string type, string? link = null)
        {
            await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                type = type,
                link = link,
                createdAt = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Send notification to all connected clients
        /// </summary>
        public async Task SendNotificationToAll(string title, string message, string type, string? link = null)
        {
            await Clients.All.SendAsync("ReceiveNotification", new
            {
                title = title,
                message = message,
                type = type,
                link = link,
                createdAt = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Update priority counts for admin dashboard (real-time)
        /// </summary>
        public async Task UpdatePriorityCounts(int highPriority, int mediumPriority, int totalPriority)
        {
            await Clients.Group("AdminUsers").SendAsync("ReceivePriorityCountUpdate", new
            {
                highPriority = highPriority,
                mediumPriority = mediumPriority,
                totalPriority = totalPriority,
                timestamp = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Notify admin of new priority application
        /// </summary>
        public async Task NotifyNewPriorityApplication(string applicationType, string applicantName, string priority, int formId)
        {
            await Clients.Group("AdminUsers").SendAsync("ReceiveNewPriorityApplication", new
            {
                applicationType = applicationType,
                applicantName = applicantName,
                priority = priority,
                formId = formId,
                timestamp = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Notify user about application delay (medium/high priority)
        /// </summary>
        public async Task NotifyUserDelay(int userId, string priority, double hoursElapsed, string applicationType)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveDelayNotification", new
            {
                priority = priority,
                hoursElapsed = hoursElapsed,
                applicationType = applicationType,
                message = GetDelayMessage(priority, hoursElapsed),
                timestamp = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Update application status in real-time
        /// </summary>
        public async Task UpdateApplicationStatus(int userId, int formId, string status, string applicationType)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveStatusUpdate", new
            {
                formId = formId,
                status = status,
                applicationType = applicationType,
                timestamp = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Broadcast to all admin users
        /// </summary>
        public async Task BroadcastToAdmins(string message, string type = "info")
        {
            await Clients.Group("AdminUsers").SendAsync("ReceiveAdminBroadcast", new
            {
                message = message,
                type = type,
                timestamp = _dateTimeService.Now
            });
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Get the userId from the connection (if authenticated)
            var userId = Context.User?.FindFirst("UserId")?.Value;
            var userRole = Context.User?.FindFirst("UserRole")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add to a group with their userId for targeted messaging
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            // Add admin users to AdminUsers group for priority notifications
            if (!string.IsNullOrEmpty(userRole) && (userRole == "Admin" || userRole == "SuperAdmin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminUsers");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            var userRole = Context.User?.FindFirst("UserRole")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            if (!string.IsNullOrEmpty(userRole) && (userRole == "Admin" || userRole == "SuperAdmin"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminUsers");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Get delay message based on priority level
        /// Expected processing time: 1-2 hours from submission to decision
        /// </summary>
        private string GetDelayMessage(string priority, double hoursElapsed)
        {
            if (priority == "high")
            {
                return $"Your application has been pending for {Math.Floor(hoursElapsed)} hours. " +
                       "We apologize for the delay (expected processing time: 1-2 hours). Our team is working on it with high priority and will process it as soon as possible.";
            }
            else if (priority == "medium")
            {
                return $"Your application has been pending for over an hour. " +
                       "Expected processing time is 1-2 hours. Don't worry! It's in our queue and will be reviewed shortly.";
            }
            return "";
        }
    }
}
