using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class NotificationsController : Controller
{
    public readonly ApplicationDbContext context;
    private readonly IWebHostEnvironment environment;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IAdminNotificationService _adminNotificationService;

    public  NotificationsController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IDateTimeService dateTimeService,
        ILogger<NotificationsController> logger,
        IAdminNotificationService adminNotificationService)
    {
        this.context = context;
        this.environment = environment;
        _dateTimeService = dateTimeService;
        _logger = logger;
        _adminNotificationService = adminNotificationService;
    }

    // ═══════════════════════════════════════════════════════════════
    // 🗑️ LEGACY NOTIFICATION SYSTEM REMOVED
    // Old GetUserNotifications() and NotificationReadStatus-based methods removed
    // Now using unified Notifications table with built-in IsRead/ReadAt fields
    // See GetUnifiedNotifications() below for the current implementation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Determines the appropriate link for the notification based on application status and age
    /// - Completed applications (claimed/disapproved) or applications older than 1 month → Past Tracking Data
    /// - Active applications (Pending/Processing/Approved not yet claimed) → Main Application Tracking
    /// </summary>
    private string GetNotificationLink(
        DateTime createdAt,
        string? status2 = null,
        DateTime? claimedAt = null,
        string? status3 = null)
    {
        // Check if application is claimed (completed)
        if (claimedAt.HasValue && claimedAt.Value.Year > 1 && !string.IsNullOrWhiteSpace(status3))
        {
            return "/Applicationtracking#past-data"; // Redirect to Past Tracking Data section
        }

        // Check if application is disapproved (completed)
        if (!string.IsNullOrWhiteSpace(status2) && !status2.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            return "/Applicationtracking#past-data"; // Redirect to Past Tracking Data section
        }

        // Check if application is older than 1 month
        var oneMonthAgo = _dateTimeService.Now.AddMonths(-1);
        if (createdAt < oneMonthAgo)
        {
            return "/Applicationtracking#past-data"; // Redirect to Past Tracking Data section
        }

        // Active applications (Pending, Processing, Approved awaiting claim) → Main tracking page
        return "/Applicationtracking";
    }

    // Helper method to get notification details based on application timeline state (matches Application Tracking page)
    private (string title, string message, string type) GetStatusNotificationDetails(
        string formType,
        string status,
        DateTime createdAt,
        DateTime? processAt = null,
        string? processBy = null,
        DateTime? resultDate = null,
        string? status2 = null,
        DateTime? claimedAt = null,
        string? status3 = null)
    {
        // Priority 1: Check if retake is required (Status == "Retake") - Action Required
        if (status == "Retake")
        {
            return (
                "Action Required - Retake Application",
                $"Your {formType} application requires document resubmission. Please update your attachments to continue processing.",
                "application_retake"
            );
        }

        // Priority 2: Check if claimed (Status3 and ClaimedAt exist) - matches "Assistance Released" on tracking page
        if (claimedAt.HasValue && claimedAt.Value.Year > 1 && !string.IsNullOrWhiteSpace(status3))
        {
            return (
                "Assistance Released",
                $"Your {formType} assistance has been released and completed on {claimedAt:MMM dd, yyyy}.",
                "application_claimed"
            );
        }

        // Priority 3: Check if approved/disapproved (Result date and Status2 exist) - matches timeline "Application Approved/Disapproved"
        if (resultDate.HasValue && resultDate.Value.Year > 1 && !string.IsNullOrWhiteSpace(status2))
        {
            if (status2.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    "Application Approved",
                    $"Good news! Your {formType} application has been approved on {resultDate:MMM dd, yyyy}. Please visit our office to collect your assistance.",
                    "application_approved"
                );
            }
            else
            {
                return (
                    "Application Disapproved",
                    $"Your {formType} application was not approved on {resultDate:MMM dd, yyyy}. Please contact us for more details.",
                    "application_disapproved"
                );
            }
        }

        // Priority 3: Check if being processed (ProcessAt and Processby exist) - matches "Being Reviewed" on tracking page
        if (processAt.HasValue && processAt.Value.Year > 1 && !string.IsNullOrWhiteSpace(processBy))
        {
            return (
                "Being Reviewed",
                $"Your {formType} application is being reviewed by {processBy}. Started on {processAt:MMM dd, yyyy}.",
                "application_processing"
            );
        }

        // Priority 4: In Queue (no ProcessAt yet) - matches "In Queue" on tracking page
        if (!processAt.HasValue || processAt.Value.Year <= 1 || string.IsNullOrWhiteSpace(processBy))
        {
            return (
                "In Queue",
                $"Your {formType} application is now in queue. Submitted on {createdAt:MMM dd, yyyy}. You will receive an update within an hour.",
                "application_submitted"
            );
        }

        // Default: Application submitted (fallback)
        return (
            "Application Submitted",
            $"Your {formType} application has been submitted on {createdAt:MMM dd, yyyy}.",
            "application_submitted"
        );
    }

    /// <summary>
    /// Get delay notification details based on priority level
    /// Messages match the Application Tracking timeline content for delays
    /// Standard Delay (medium): 1-2 hours | Critical Delay (high): 2+ hours
    /// </summary>
    private (string title, string message) GetDelayNotificationDetails(string formType, string priority, DateTime createdAt)
    {
        var hoursElapsed = (_dateTimeService.Now - createdAt).TotalHours;

        return priority switch
        {
            "high" => (
                $"{formType} - Critical Delay",
                $"Your application has been waiting for more than 2 hours. We apologize for the delay and are working to process it as soon as possible."
            ),
            "medium" => (
                $"{formType} - Standard Delay",
                $"Your application has been waiting for 1-2 hours. We are reviewing your application and will provide an update soon."
            ),
            _ => (
                $"{formType} - Update",
                $"Your application is in queue. You will receive an update within an hour."
            )
        };
    }

    /// <summary>
    /// Calculate priority level for applications in pending or processing state
    /// Priority is based on waiting time since submission
    /// Critical Delay (High Priority): 2+ hours | Standard Delay (Medium Priority): 1-2 hours | Normal: < 1 hour
    /// </summary>
    private string CalculateApplicationPriority(string status, string status2, DateTime createdAt)
    {
        // Only calculate priority for pending or processing applications
        // If already approved/disapproved/claimed, no priority needed
        if (!string.IsNullOrEmpty(status2) && (status2.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
            status2.Equals("Disapprove", StringComparison.OrdinalIgnoreCase)))
        {
            return "normal";
        }

        // Check if still in pending or processing state
        if (string.IsNullOrEmpty(status) ||
            (!status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
             !status.Equals("Processing", StringComparison.OrdinalIgnoreCase)))
        {
            return "normal";
        }

        // Calculate hours elapsed since submission (using Philippine time)
        var hoursElapsed = (_dateTimeService.Now - createdAt).TotalHours;

        if (hoursElapsed >= 2)
        {
            return "high";
        }
        else if (hoursElapsed >= 1)
        {
            return "medium";
        }
        else
        {
            return "normal";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🔔 UNIFIED NOTIFICATIONS API - Load from Notifications Table
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all notifications for the current user from the unified Notifications table
    /// Used by Homepage.cshtml for notification dropdown
    /// </summary>
    [HttpGet]
    public async Task<JsonResult> GetUnifiedNotifications()
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "User not authenticated" });
        }

        try
        {
            // Get all non-archived USER notifications (RecipientType = 1), ordered by creation date descending
            var notifications = await context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientType == 1 && n.UserId == userId && !n.IsArchived)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    id = n.Id,
                    notificationId = n.NotificationIdentifier,
                    applicationType = n.ApplicationType,
                    applicationId = n.ApplicationId,
                    applicantName = n.ApplicantName,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    link = n.Link,
                    status = n.Status,
                    status2 = n.Status2,
                    status3 = n.Status3,
                    comments = n.Comments,
                    processedBy = n.ProcessedBy,
                    processStage = n.ProcessStage,
                    priority = n.Priority,
                    createdAt = n.CreatedAt,
                    eventTimestamp = n.EventTimestamp,
                    retakeIteration = n.RetakeIteration,
                    isRetake = n.IsRetake,
                    isPermanent = n.IsPermanent,
                    isRead = n.IsRead,
                    readAt = n.ReadAt,
                    displayOrder = n.DisplayOrder
                })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {notifications.Count} unified notifications for user {userId}");

            return Json(new { success = true, notifications });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving unified notifications for user {userId}");
            return Json(new { error = "Failed to load notifications" });
        }
    }

    /// <summary>
    /// Get notifications for a specific application
    /// Used by Applicationtracking.cshtml for timeline display
    /// </summary>
    [HttpGet]
    public async Task<JsonResult> GetApplicationNotifications(string appType, int appId)
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "User not authenticated" });
        }

        try
        {
            // Get all notifications for specific application, ordered by display order and creation date
            var notifications = await context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId &&
                           n.ApplicationType == appType &&
                           n.ApplicationId == appId &&
                           !n.IsArchived)
                .OrderBy(n => n.DisplayOrder)
                .ThenBy(n => n.CreatedAt)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    status2 = n.Status2,
                    comments = n.Comments,
                    processedBy = n.ProcessedBy,
                    processStage = n.ProcessStage,
                    priority = n.Priority,
                    createdAt = n.CreatedAt,
                    eventTimestamp = n.EventTimestamp,
                    retakeIteration = n.RetakeIteration,
                    isRetake = n.IsRetake,
                    displayOrder = n.DisplayOrder
                })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {notifications.Count} notifications for application {appType}/{appId}");

            return Json(new { success = true, notifications });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving application notifications for {appType}/{appId}");
            return Json(new { error = "Failed to load application notifications" });
        }
    }

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> MarkNotificationAsRead(int notificationId)
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "User not authenticated" });
        }

        try
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return Json(new { error = "Notification not found" });
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = _dateTimeService.Now;
                await context.SaveChangesAsync();

                _logger.LogInformation($"Notification {notificationId} marked as read for user {userId}");
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {notificationId} as read");
            return Json(new { error = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> MarkAllNotificationsAsRead()
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "User not authenticated" });
        }

        try
        {
            var unreadNotifications = await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsArchived)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = _dateTimeService.Now;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation($"Marked {unreadNotifications.Count} notifications as read for user {userId}");
            }

            return Json(new { success = true, count = unreadNotifications.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking all notifications as read for user {userId}");
            return Json(new { error = "Failed to mark all notifications as read" });
        }
    }

    /// <summary>
    /// Get unread notification count for current user
    /// </summary>
    [HttpGet]
    public async Task<JsonResult> GetUnreadNotificationCount()
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "User not authenticated" });
        }

        try
        {
            var unreadCount = await context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsArchived);

            return Json(new { success = true, count = unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting unread notification count for user {userId}");
            return Json(new { error = "Failed to get unread count" });
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 🔔 ADMIN NOTIFICATION ENDPOINTS (RecipientType = 2)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all admin notifications (RecipientType = 2)
    /// </summary>
    [HttpGet]
    public async Task<JsonResult> GetAdminNotifications()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "SuperAdmin"))
        {
            return Json(new { error = "Unauthorized" });
        }

        try
        {
            var notifications = await _adminNotificationService.GetRecentNotificationsAsync(50);
            var unreadCount = await _adminNotificationService.GetUnreadCountAsync();

            return Json(new
            {
                success = true,
                notifications = notifications.Select(n => new
                {
                    id = n.Id,
                    applicationType = n.ApplicationType,
                    applicationId = n.ApplicationId,
                    userId = n.UserId,
                    applicantName = n.ApplicantName,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    link = n.Link,
                    priority = n.Priority,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead,
                    readAt = n.ReadAt
                }),
                unreadCount = unreadCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin notifications");
            return Json(new { error = "Failed to load admin notifications" });
        }
    }

    /// <summary>
    /// Get unread admin notification count
    /// </summary>
    [HttpGet]
    public async Task<JsonResult> GetAdminNotificationCount()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "SuperAdmin"))
        {
            return Json(new { error = "Unauthorized" });
        }

        try
        {
            var unreadCount = await _adminNotificationService.GetUnreadCountAsync();
            return Json(new { success = true, count = unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin notification count");
            return Json(new { error = "Failed to get unread count" });
        }
    }

    /// <summary>
    /// Mark admin notification as read
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> MarkAdminNotificationAsRead([FromBody] JsonElement body)
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "SuperAdmin"))
        {
            return Json(new { error = "Unauthorized" });
        }

        try
        {
            if (!body.TryGetProperty("notificationId", out JsonElement notificationIdElement) ||
                !notificationIdElement.TryGetInt32(out int notificationId))
            {
                return Json(new { error = "Invalid notification ID" });
            }

            await _adminNotificationService.MarkAsReadAsync(notificationId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking admin notification as read");
            return Json(new { error = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Mark all admin notifications as read
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> MarkAllAdminNotificationsAsRead()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "SuperAdmin"))
        {
            return Json(new { error = "Unauthorized" });
        }

        try
        {
            await _adminNotificationService.MarkAllAsReadAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all admin notifications as read");
            return Json(new { error = "Failed to mark all notifications as read" });
        }
    }
}
