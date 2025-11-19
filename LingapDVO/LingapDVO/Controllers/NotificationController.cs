using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class NotificationsController : Controller
{
    public readonly ApplicationDbContext context;
    private readonly IWebHostEnvironment environment;
    private readonly IDateTimeService _dateTimeService;

    public  NotificationsController(ApplicationDbContext context, IWebHostEnvironment environment, IDateTimeService dateTimeService)
    {
        this.context = context;
        this.environment = environment;
        _dateTimeService = dateTimeService;
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
            }

            // Add a demo notification if no forms found
            if (!notifications.Any())
            {
                var demoNotificationId = "welcome_1";
                var isDemoRead = readNotificationIds.Contains(demoNotificationId);

                notifications.Add(new
                {
                    id = demoNotificationId,
                    title = "Welcome to LingapDVO",
                    message = "Get started by submitting your first application",
                    isRead = isDemoRead,
                    createdAt = _dateTimeService.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = "welcome",
                    link = "#"
                });
            }

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
            // Log exception
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
            // Log exception
            return Json(new { success = false, error = "An error occurred while marking notification as read" });
        }
    }

    [HttpPost]
    public JsonResult MarkAllNotificationsAsRead()
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");

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
            // Log the exception
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
            }

            foreach (var medical in recentMedicalLabForms)
            {
                notificationIds.Add($"medical_{medical.Id}_{medical.Status}");
            }

            foreach (var funeral in recentFuneralForms)
            {
                notificationIds.Add($"funeral_{funeral.Id}_{funeral.Status}");
            }

            if (!notificationIds.Any())
            {
                notificationIds.Add("welcome_1");
            }
        }
        catch (Exception ex)
        {
            // Log error and return at least the welcome notification
            if (!notificationIds.Any())
            {
                notificationIds.Add("welcome_1");
            }
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
            // Log error and return empty hashset
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
            // Log error
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
