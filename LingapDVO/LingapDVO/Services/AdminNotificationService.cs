using LingapDVO.Hubs;
using LingapDVO.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    public interface IAdminNotificationService
    {
        Task SendAdminNotificationAsync(string notificationType, string assistanceType, int relatedId, int? userId, string applicantName, string title, string message, string? link = null, string priority = "normal");
        Task<int> GetUnreadCountAsync();
        Task<List<Notification>> GetRecentNotificationsAsync(int limit = 50);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync();
    }

    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<AdminNotificationService> _logger;

        public AdminNotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            IDateTimeService dateTimeService,
            ILogger<AdminNotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        /// <summary>
        /// Send notification to all admin users (RecipientType = 2)
        /// </summary>
        public async Task SendAdminNotificationAsync(
            string notificationType,
            string assistanceType,
            int relatedId,
            int? userId,
            string applicantName,
            string title,
            string message,
            string? link = null,
            string priority = "normal")
        {
            try
            {
                // Create and save admin notification to database (RecipientType = 2 for admin)
                var notification = new Notification
                {
                    RecipientType = 2, // Admin notification
                    UserId = userId, // Store user ID who submitted (optional)
                    ApplicationType = assistanceType,
                    ApplicationId = relatedId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = notificationType,
                    Link = link ?? $"/{assistanceType}PendingStatus/{relatedId}",
                    ProcessStage = "admin_notification",
                    Priority = priority,
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    IsRead = false,
                    IsArchived = false,
                    SentViaInApp = true,
                    NotificationIdentifier = $"admin_{notificationType}_{assistanceType}_{relatedId}_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = 0
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Get current unread count
                var unreadCount = await GetUnreadCountAsync();

                // Send real-time notification to all admin users via SignalR
                await _hubContext.Clients.Group("AdminUsers").SendAsync("ReceiveAdminNotification", new
                {
                    id = notification.Id,
                    notificationType = notificationType,
                    assistanceType = assistanceType,
                    relatedId = relatedId,
                    userId = userId,
                    applicantName = applicantName,
                    title = title,
                    message = message,
                    link = notification.Link,
                    priority = priority,
                    createdAt = _dateTimeService.Now,
                    unreadCount = unreadCount
                });

                _logger.LogInformation($"Admin notification sent: {notificationType} for {assistanceType}/{relatedId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending admin notification for {assistanceType}/{relatedId}");
                throw;
            }
        }

        /// <summary>
        /// Get count of unread admin notifications (RecipientType = 2)
        /// </summary>
        public async Task<int> GetUnreadCountAsync()
        {
            return await _context.Notifications
                .CountAsync(n => n.RecipientType == 2 && !n.IsRead && !n.IsArchived);
        }

        /// <summary>
        /// Get recent admin notifications (unread first, then read)
        /// </summary>
        public async Task<List<Notification>> GetRecentNotificationsAsync(int limit = 50)
        {
            return await _context.Notifications
                .Where(n => n.RecipientType == 2 && !n.IsArchived)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Mark a specific admin notification as read
        /// </summary>
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientType == 2);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = _dateTimeService.Now;
                await _context.SaveChangesAsync();

                // Update unread count for all admins
                var unreadCount = await GetUnreadCountAsync();
                await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateAdminNotificationCount", unreadCount);
            }
        }

        /// <summary>
        /// Mark all admin notifications as read
        /// </summary>
        public async Task MarkAllAsReadAsync()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientType == 2 && !n.IsRead && !n.IsArchived)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = _dateTimeService.Now;
                }

                await _context.SaveChangesAsync();

                // Update unread count for all admins
                await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateAdminNotificationCount", 0);
            }
        }
    }
}
