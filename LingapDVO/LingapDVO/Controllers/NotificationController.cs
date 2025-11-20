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

            // Get ALL form submissions for the user (no date limit - keep all notifications visible)
            var recentHospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentMedicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentFuneralForms = context.FuneralAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create notifications for hospital bills (show up to 100 most recent)
            foreach (var bill in recentHospitalBills.Take(100))
            {
                var notificationId = $"hospital_{bill.Id}_{bill.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                // Pass all timeline fields to match Application Tracking page logic
                var (title, message, type) = GetStatusNotificationDetails(
                    "Hospital Assistance",
                    bill.Status,
                    bill.CreatedAt,
                    bill.ProcessAt,
                    bill.Processby,
                    bill.Result,
                    bill.Status2,
                    bill.ClaimedAt,
                    bill.Status3
                );
                var priority = CalculateApplicationPriority(bill.Status, bill.Status2, bill.CreatedAt);
                var notificationLink = GetNotificationLink(bill.CreatedAt, bill.Status2, bill.ClaimedAt, bill.Status3);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = bill.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = notificationLink,
                    status = bill.Status,
                    priority = priority,
                    isPermanent = false
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
                        link = notificationLink, // Use same link logic
                        status = bill.Status,
                        priority = priority,
                        isPermanent = false
                    });
                }
            }

            // Create notifications for medical lab forms
            foreach (var medical in recentMedicalLabForms.Take(100))
            {
                var notificationId = $"medical_{medical.Id}_{medical.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                // Pass all timeline fields to match Application Tracking page logic
                var (title, message, type) = GetStatusNotificationDetails(
                    "Other Assistance",
                    medical.Status,
                    medical.CreatedAt,
                    medical.ProcessAt,
                    medical.Processby,
                    medical.Result,
                    medical.Status2,
                    medical.ClaimedAt,
                    medical.Status3
                );
                var priority = CalculateApplicationPriority(medical.Status, medical.Status2, medical.CreatedAt);
                var notificationLink = GetNotificationLink(medical.CreatedAt, medical.Status2, medical.ClaimedAt, medical.Status3);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = medical.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = notificationLink,
                    status = medical.Status,
                    priority = priority,
                    isPermanent = false
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
                        link = notificationLink, // Use same link logic
                        status = medical.Status,
                        priority = priority,
                        isPermanent = false
                    });
                }
            }

            // Create notifications for funeral forms
            foreach (var funeral in recentFuneralForms.Take(100))
            {
                var notificationId = $"funeral_{funeral.Id}_{funeral.Status}";
                var isRead = readNotificationIds.Contains(notificationId);

                // Pass all timeline fields to match Application Tracking page logic
                var (title, message, type) = GetStatusNotificationDetails(
                    "Funeral Assistance",
                    funeral.Status,
                    funeral.CreatedAt,
                    funeral.ProcessAt,
                    funeral.Processby,
                    funeral.Result,
                    funeral.Status2,
                    funeral.ClaimedAt,
                    funeral.Status3
                );
                var priority = CalculateApplicationPriority(funeral.Status, funeral.Status2, funeral.CreatedAt);
                var notificationLink = GetNotificationLink(funeral.CreatedAt, funeral.Status2, funeral.ClaimedAt, funeral.Status3);

                notifications.Add(new
                {
                    id = notificationId,
                    title = title,
                    message = message,
                    isRead = isRead,
                    createdAt = funeral.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = type,
                    link = notificationLink,
                    status = funeral.Status,
                    priority = priority,
                    isPermanent = false
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
                        link = notificationLink, // Use same link logic
                        status = funeral.Status,
                        priority = priority,
                        isPermanent = false
                    });
                }
            }

            // Check if user is verified
            var verification = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            bool isVerified = verification != null;

            // Generate appropriate welcome notification based on verification status
            // ALWAYS ADD WELCOME NOTIFICATION - it's a permanent notification
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

            // Add welcome notification with isPermanent flag to ensure it always appears
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
                priority = "normal",
                isPermanent = true  // Mark as permanent so it's always included
            });

            // Order notifications: temporary notifications by date (newest first), then permanent notifications at the end
            // This ensures the welcome notification always appears at the bottom of the list
            var orderedNotifications = notifications
                .OrderBy(n => ((dynamic)n).isPermanent)  // Non-permanent (false) first, permanent (true) last
                .ThenByDescending(n => ((dynamic)n).createdAt)     // Then by creation date within each group
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

    /// <summary>
    /// Determines the appropriate link for the notification based on application status and age
    /// - Completed applications (claimed/disapproved) or applications older than 1 month → Past Tracking Data
    /// - Active applications (Pending/Processing/Approved not yet claimed) → Main Application Tracking
    /// </summary>
    private string GetNotificationLink(
        DateTime createdAt,
        string status2 = null,
        DateTime? claimedAt = null,
        string status3 = null)
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
        string processBy = null,
        DateTime? resultDate = null,
        string status2 = null,
        DateTime? claimedAt = null,
        string status3 = null)
    {
        // Priority 1: Check if claimed (Status3 and ClaimedAt exist) - matches "Assistance Released" on tracking page
        if (claimedAt.HasValue && claimedAt.Value.Year > 1 && !string.IsNullOrWhiteSpace(status3))
        {
            return (
                "Assistance Released",
                $"Your {formType} assistance has been released and completed on {claimedAt:MMM dd, yyyy}.",
                "application_claimed"
            );
        }

        // Priority 2: Check if approved/disapproved (Result date and Status2 exist) - matches timeline "Application Approved/Disapproved"
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

        // Priority 4: Waiting for review (no ProcessAt yet) - matches "Waiting for Review" on tracking page
        if (!processAt.HasValue || processAt.Value.Year <= 1 || string.IsNullOrWhiteSpace(processBy))
        {
            return (
                "Waiting for Review",
                $"Your {formType} application is waiting to be reviewed. Submitted on {createdAt:MMM dd, yyyy}. We will review it within 1-2 hours.",
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
