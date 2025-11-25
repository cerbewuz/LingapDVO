using LingapDVO.Models;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    /// <summary>
    /// Handles saving notifications to the database for complete audit trail
    /// and synchronization across Homepage and Application Tracking pages
    /// </summary>
    public interface INotificationPersistenceService
    {
        Task<Notification> SaveNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            int? formId,
            string title,
            string message,
            string type,
            string? link,
            string? status,
            string? status2,
            string? status3,
            string? comments,
            string? processedBy,
            string processStage,
            DateTime? eventTimestamp,
            string? priority = null,
            bool sentViaInApp = false,
            bool sentViaEmail = false,
            bool sentViaSms = false);

        Task<int?> GetRetakeIterationAsync(string formType, int formId);
    }

    public class NotificationPersistenceService : INotificationPersistenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<NotificationPersistenceService> _logger;

        public NotificationPersistenceService(
            ApplicationDbContext context,
            IDateTimeService dateTimeService,
            ILogger<NotificationPersistenceService> logger)
        {
            _context = context;
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<Notification> SaveNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            int? formId,
            string title,
            string message,
            string type,
            string? link,
            string? status,
            string? status2,
            string? status3,
            string? comments,
            string? processedBy,
            string processStage,
            DateTime? eventTimestamp,
            string? priority = null,
            bool sentViaInApp = false,
            bool sentViaEmail = false,
            bool sentViaSms = false)
        {
            try
            {
                // Check for retake workflow
                int? retakeIteration = null;
                bool isRetake = false;

                if (formId.HasValue && status2 == "Retake")
                {
                    // Count previous retake notifications for this application
                    var previousRetakes = await _context.Notifications
                        .Where(n => n.ApplicationId == formId.Value &&
                                   n.ApplicationType == formType &&
                                   n.IsRetake)
                        .CountAsync();

                    isRetake = true;
                    retakeIteration = previousRetakes + 1;
                }

                // Generate unique notification identifier
                var notificationIdentifier = formId.HasValue
                    ? $"{formType}_{formId}_{processStage}_{_dateTimeService.Now.Ticks}"
                    : $"system_{type}_{userId}_{_dateTimeService.Now.Ticks}";

                // Determine display order based on process stage
                var displayOrder = GetDisplayOrder(processStage, status2);

                // Create notification object
                var notification = new Notification
                {
                    UserId = userId,
                    ApplicationType = formType,
                    ApplicationId = formId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link ?? "/Applicationtracking",
                    Status = status,
                    Status2 = status2,
                    Status3 = status3,
                    Comments = comments,
                    ProcessedBy = processedBy,
                    ProcessStage = processStage,
                    Priority = priority,
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = eventTimestamp ?? _dateTimeService.Now,
                    RetakeIteration = retakeIteration,
                    IsRetake = isRetake,
                    IsPermanent = type == "welcome",
                    IsRead = false,
                    ReadAt = null,
                    SentViaInApp = sentViaInApp,
                    SentViaEmail = sentViaEmail,
                    SentViaSms = sentViaSms,
                    NotificationIdentifier = notificationIdentifier,
                    IsArchived = false,
                    DisplayOrder = displayOrder
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notification saved to database: ID={notification.Id}, Type={type}, User={userId}, App={formType}_{formId}");

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving notification to database for user {userId}");
                throw;
            }
        }

        public async Task<int?> GetRetakeIterationAsync(string formType, int formId)
        {
            try
            {
                var retakeCount = await _context.Notifications
                    .Where(n => n.ApplicationId == formId &&
                               n.ApplicationType == formType &&
                               n.IsRetake)
                    .CountAsync();

                return retakeCount > 0 ? retakeCount : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting retake iteration for {formType} {formId}");
                return null;
            }
        }

        private int GetDisplayOrder(string processStage, string? status2)
        {
            // Determine display order for timeline rendering
            // Lower numbers appear first
            return processStage switch
            {
                "submitted" => 1,
                "processing" => 2,
                "result" => status2 == "Retake" ? 3 : 3,
                "claimed" => 4,
                "delayed" => 5,
                "system" => 0,
                _ => 999
            };
        }
    }
}
