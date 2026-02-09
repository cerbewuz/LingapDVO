using LingapDVO.Models;

namespace LingapDVO.Services
{
    /// <summary>
    /// Adapter that implements IMultiChannelNotificationService by delegating to IUnifiedNotificationService.
    /// Provides backward compatibility for existing code that uses IMultiChannelNotificationService.
    /// </summary>
    public class MultiChannelNotificationServiceAdapter : IMultiChannelNotificationService
    {
        private readonly IUnifiedNotificationService _unifiedService;

        public MultiChannelNotificationServiceAdapter(IUnifiedNotificationService unifiedService)
        {
            _unifiedService = unifiedService;
        }

        public Task SendNotificationAsync(int userId, string title, string message, string type, string? link = null)
        {
            return _unifiedService.SendNotificationAsync(
                NotificationRecipientType.User,
                userId,
                title,
                message,
                type,
                link);
        }

        public Task SendStatusChangeNotificationAsync(int userId, string applicantName, string formType, string status, int formId, string? comments = null, string? processedBy = null)
        {
            return _unifiedService.SendStatusChangeNotificationAsync(userId, applicantName, formType, status, formId, comments, processedBy);
        }

        public Task SendDelayNotificationAsync(int userId, string applicantName, string formType, string priority, DateTime submittedDate, int applicationId)
        {
            return _unifiedService.SendDelayNotificationAsync(userId, applicantName, formType, priority, submittedDate, applicationId);
        }
    }

    /// <summary>
    /// Adapter that implements IAdminNotificationService by delegating to IUnifiedNotificationService.
    /// Provides backward compatibility for existing code that uses IAdminNotificationService.
    /// </summary>
    public class AdminNotificationServiceAdapter : IAdminNotificationService
    {
        private readonly IUnifiedNotificationService _unifiedService;

        public AdminNotificationServiceAdapter(IUnifiedNotificationService unifiedService)
        {
            _unifiedService = unifiedService;
        }

        public Task SendAdminNotificationAsync(string notificationType, string assistanceType, int relatedId, int? userId, string applicantName, string title, string message, string? link = null, string priority = "normal")
        {
            return _unifiedService.SendAdminNotificationAsync(notificationType, assistanceType, relatedId, userId, applicantName, title, message, link, priority);
        }

        public Task<int> GetUnreadCountAsync()
        {
            return _unifiedService.GetUnreadCountAsync(NotificationRecipientType.Admin);
        }

        public Task<List<Notification>> GetRecentNotificationsAsync(int limit = 50)
        {
            return _unifiedService.GetRecentNotificationsAsync(NotificationRecipientType.Admin, null, limit);
        }

        public Task MarkAsReadAsync(int notificationId)
        {
            return _unifiedService.MarkAsReadAsync(notificationId, NotificationRecipientType.Admin);
        }

        public Task MarkAllAsReadAsync()
        {
            return _unifiedService.MarkAllAsReadAsync(NotificationRecipientType.Admin);
        }
    }
}
