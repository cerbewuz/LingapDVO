using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class NotificationsController : Controller
{
    public readonly ApplicationDbContext context;
    private readonly IWebHostEnvironment environment;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<NotificationsController> _logger;

    public  NotificationsController(ApplicationDbContext context, IWebHostEnvironment environment, IDateTimeService dateTimeService, ILogger<NotificationsController> logger)
    {
        this.context = context;
        this.environment = environment;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet]
    public JsonResult GetUserNotifications()
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString))
        {
            return Json(new { error = "User not authenticated" });
        }

        if (!int.TryParse(userIdString, out int userId))
        {
            return Json(new { error = "Invalid user ID" });
        }

        var notifications = new List<object>();

        try
        {
            // Get read notification IDs from session
            var readNotificationIds = GetReadNotificationIdsFromSession(userId);

            // Check for recent form submissions (last 7 days)
            var recentHospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentMedicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentFuneralForms = context.FuneralAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create notifications for hospital bills
            foreach (var bill in recentHospitalBills.Take(5))
            {
                var notificationId = $"hospital_{bill.Id}_{bill.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                var (title, message, type) = GetStatusNotificationDetails("Hospital Assistance", bill.Status, bill.CreatedAt);
                var priority = CalculateApplicationPriority(bill.Status, bill.Status2, bill.CreatedAt);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = bill.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = "/Applicationtracking",
                    status = bill.Status,
                    priority = priority
                });

                // Add separate delay notification if priority is high or medium
                if (priority == "high" || priority == "medium")
                {
                    var delayNotificationId = $"delay_hospital_{bill.Id}";
                    var isDelayRead = readNotificationIds.Contains(delayNotificationId);
                    var (delayTitle, delayMessage) = GetDelayNotificationDetails("Hospital Assistance", priority, bill.CreatedAt);

                    notifications.Add(new
                    {
                        id = delayNotificationId,
                        title = delayTitle,
                        message = delayMessage,
                        isRead = isDelayRead,
                        createdAt = bill.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        type = "delay_alert",
                        link = "/Applicationtracking",
                        status = bill.Status,
                        priority = priority
                    });
                }
            }

            // Create notifications for medical lab forms
            foreach (var medical in recentMedicalLabForms.Take(5))
            {
                var notificationId = $"medical_{medical.Id}_{medical.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                var (title, message, type) = GetStatusNotificationDetails("Other Assistance", medical.Status, medical.CreatedAt);
                var priority = CalculateApplicationPriority(medical.Status, medical.Status2, medical.CreatedAt);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = medical.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = "/Applicationtracking",
                    status = medical.Status,
                    priority = priority
                });

                // Add separate delay notification if priority is high or medium
                if (priority == "high" || priority == "medium")
                {
                    var delayNotificationId = $"delay_medical_{medical.Id}";
                    var isDelayRead = readNotificationIds.Contains(delayNotificationId);
                    var (delayTitle, delayMessage) = GetDelayNotificationDetails("Other Assistance", priority, medical.CreatedAt);

                    notifications.Add(new
                    {
                        id = delayNotificationId,
                        title = delayTitle,
                        message = delayMessage,
                        isRead = isDelayRead,
                        createdAt = medical.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        type = "delay_alert",
                        link = "/Applicationtracking",
                        status = medical.Status,
                        priority = priority
                    });
                }
            }

            // Create notifications for funeral forms
            foreach (var funeral in recentFuneralForms.Take(5))
            {
                var notificationId = $"funeral_{funeral.Id}_{funeral.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                var (title, message, type) = GetStatusNotificationDetails("Funeral Assistance", funeral.Status, funeral.CreatedAt);
                var priority = CalculateApplicationPriority(funeral.Status, funeral.Status2, funeral.CreatedAt);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = funeral.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = "/Applicationtracking",
                    status = funeral.Status,
                    priority = priority
                });

                // Add separate delay notification if priority is high or medium
                if (priority == "high" || priority == "medium")
                {
                    var delayNotificationId = $"delay_funeral_{funeral.Id}";
                    var isDelayRead = readNotificationIds.Contains(delayNotificationId);
                    var (delayTitle, delayMessage) = GetDelayNotificationDetails("Funeral Assistance", priority, funeral.CreatedAt);

                    notifications.Add(new
                    {
                        id = delayNotificationId,
                        title = delayTitle,
                        message = delayMessage,
                        isRead = isDelayRead,
                        createdAt = funeral.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        type = "delay_alert",
                        link = "/Applicationtracking",
                        status = funeral.Status,
                        priority = priority
                    });
                }
            }

            // Check if user is verified
            var verification = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            bool isVerified = verification != null;

            // Generate appropriate welcome notification based on verification status
            var welcomeNotificationId = isVerified ? $"welcome_verified_{userId}" : $"welcome_unverified_{userId}";
            var isWelcomeRead = readNotificationIds.Contains(welcomeNotificationId);

            // Get user's actual registration date from RegistrationAuditLog
            var userRegistrationDate = context.RegistrationAuditLogs
                .Where(log => log.RegisteredUserId == userId && log.Action == "SUCCESS")
                .OrderBy(log => log.AttemptedAt)
                .Select(log => log.AttemptedAt)
                .FirstOrDefault();

            // If no registration date found, use current time minus 1 hour
            var welcomeDate = userRegistrationDate != default(DateTime)
                ? userRegistrationDate
                : _dateTimeService.Now.AddHours(-1);

            string welcomeTitle, welcomeMessage;
            if (isVerified)
            {
                // Verified user - congratulations message
                welcomeTitle = "Account Verified!";
                welcomeMessage = "Congratulations! Your account has been successfully verified. You now have full access to all LingapDVO services and can submit assistance applications.";
            }
            else
            {
                // Unverified user - encourage verification
                welcomeTitle = "Welcome to LingapDVO!";
                welcomeMessage = "Thank you for joining LingapDVO! To access all services and submit assistance applications, please complete your account verification.";
            }

            notifications.Add(new
            {
                id = welcomeNotificationId,
                title = welcomeTitle,
                message = welcomeMessage,
                isRead = isWelcomeRead,
                createdAt = welcomeDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                type = "welcome",
                link = "/Home",
                status = "Welcome",
                priority = "normal"
            });

            // Order by creation date (newest first)
            var orderedNotifications = notifications
                .OrderByDescending(n => ((dynamic)n).createdAt)
                .ToList();

            // Prevent browser caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return Json(orderedNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading notifications for user");
            return Json(new { error = "An error occurred while loading notifications" });
        }
    }

    [HttpPost]
    public JsonResult MarkNotificationAsRead(string notificationId)
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Json(new { success = false, error = "User not authenticated" });
        }

        if (string.IsNullOrEmpty(notificationId))
        {
            return Json(new { success = false, error = "Notification ID is required" });
        }

        try
        {
            // Update session
            UpdateSessionReadNotifications(userId, notificationId);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking notification {NotificationId} as read", notificationId);
            return Json(new { success = false, error = "An error occurred while marking notification as read" });
        }
    }

    [HttpPost]
    public JsonResult MarkAllNotificationsAsRead()
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        try
        {
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            // Get current notification IDs using the fixed method
            var notificationIds = GetCurrentUserNotificationIds(userId);

            // Update session with all notification IDs marked as read
            HttpContext.Session.SetString($"ReadNotifications_{userId}", string.Join(",", notificationIds));

            return Json(new { success = true, message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking all notifications as read for user {UserIdString}", userIdString);
            return Json(new { success = false, error = "An error occurred while marking all notifications as read" });
        }
    }

    // Fixed helper method that returns List<string> instead of List<dynamic>
    private List<string> GetCurrentUserNotificationIds(int userId)
    {
        var notificationIds = new List<string>();

        try
        {
            // Get recent forms (same logic as GetUserNotifications but without read check)
            var recentHospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            var recentMedicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            var recentFuneralForms = context.FuneralAssistance
                .Where(f => f.UserId == userId && f.CreatedAt >= _dateTimeService.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var bill in recentHospitalBills)
            {
                notificationIds.Add($"hospital_{bill.Id}_{bill.Status}");

                // Add delay notification ID if applicable
                var priority = CalculateApplicationPriority(bill.Status, bill.Status2, bill.CreatedAt);
                if (priority == "high" || priority == "medium")
                {
                    notificationIds.Add($"delay_hospital_{bill.Id}");
                }
            }

            foreach (var medical in recentMedicalLabForms)
            {
                notificationIds.Add($"medical_{medical.Id}_{medical.Status}");

                // Add delay notification ID if applicable
                var priority = CalculateApplicationPriority(medical.Status, medical.Status2, medical.CreatedAt);
                if (priority == "high" || priority == "medium")
                {
                    notificationIds.Add($"delay_medical_{medical.Id}");
                }
            }

            foreach (var funeral in recentFuneralForms)
            {
                notificationIds.Add($"funeral_{funeral.Id}_{funeral.Status}");

                // Add delay notification ID if applicable
                var priority = CalculateApplicationPriority(funeral.Status, funeral.Status2, funeral.CreatedAt);
                if (priority == "high" || priority == "medium")
                {
                    notificationIds.Add($"delay_funeral_{funeral.Id}");
                }
            }

            // Always include welcome notifications (both types to handle verification state changes)
            notificationIds.Add($"welcome_verified_{userId}");
            notificationIds.Add($"welcome_unverified_{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting current notification IDs for user {UserId}", userId);
            // Always include welcome notifications even on error
            notificationIds.Add($"welcome_verified_{userId}");
            notificationIds.Add($"welcome_unverified_{userId}");
        }

        return notificationIds;
    }

    // Session-based methods
    private HashSet<string> GetReadNotificationIdsFromSession(int userId)
    {
        try
        {
            var readNotificationsString = HttpContext.Session.GetString($"ReadNotifications_{userId}") ?? "";
            return new HashSet<string>(readNotificationsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting read notification IDs from session for user {UserId}", userId);
            return new HashSet<string>();
        }
    }

    private void UpdateSessionReadNotifications(int userId, string notificationId)
    {
        try
        {
            var readNotificationsString = HttpContext.Session.GetString($"ReadNotifications_{userId}") ?? "";
            var readNotificationIds = new List<string>(readNotificationsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            if (!readNotificationIds.Contains(notificationId))
            {
                readNotificationIds.Add(notificationId);
                // Limit to 100 IDs to prevent session from getting too large
                if (readNotificationIds.Count > 100)
                {
                    readNotificationIds = readNotificationIds.Skip(readNotificationIds.Count - 100).ToList();
                }
                HttpContext.Session.SetString($"ReadNotifications_{userId}", string.Join(",", readNotificationIds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating session read notifications for user {UserId}", userId);
            throw;
        }
    }

    // Helper method to get notification details based on status
    private (string title, string message, string type) GetStatusNotificationDetails(string formType, string status, DateTime createdAt)
    {
        return status switch
        {
            "Pending" => (
                "Application Submitted",
                $"Your {formType} application has been submitted and is pending review. Submitted on {createdAt:MMM dd, yyyy}.",
                "application_submitted"
            ),
            "Processing" => (
                "Application Being Processed",
                $"Your {formType} application is now being processed by our team.",
                "application_processing"
            ),
            "Approve" => (
                "Application Approved",
                $"Good news! Your {formType} application has been approved. Please visit the office for claiming.",
                "application_approved"
            ),
            "Disapprove" => (
                "Application Disapproved",
                $"We regret to inform you that your {formType} application has been disapproved. Please contact us for more details.",
                "application_disapproved"
            ),
            "Claimed" => (
                "Assistance Claimed",
                $"Your {formType} has been successfully claimed. Thank you for using our service.",
                "application_claimed"
            ),
            _ => (
                "Application Update",
                $"Your {formType} application status has been updated. Submitted on {createdAt:MMM dd, yyyy}.",
                "status_change"
            )
        };
    }

    /// <summary>
    /// Get delay notification details based on priority level
    /// Messages match the Application Tracking timeline content for delays
    /// </summary>
    private (string title, string message) GetDelayNotificationDetails(string formType, string priority, DateTime createdAt)
    {
        var hoursElapsed = (_dateTimeService.Now - createdAt).TotalHours;

        return priority switch
        {
            "high" => (
                $"{formType} - Experiencing Delay",
                $"Your application has been waiting for more than 2 hours. We apologize for the delay. We will review it within 1-2 hours and are working to process it as soon as possible."
            ),
            "medium" => (
                $"{formType} - Processing Delay",
                $"Your application has been waiting for over 1 hour. We are reviewing your application and will make a decision within 1-2 hours."
            ),
            _ => (
                $"{formType} - Update",
                $"Your application is waiting to be reviewed. We will review it within 1-2 hours."
            )
        };
    }

    /// <summary>
    /// Calculate priority level for applications in pending or processing state
    /// Priority is based on waiting time since submission
    /// High Priority: 2+ hours | Medium Priority: 1-2 hours | Normal: < 1 hour
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
}
