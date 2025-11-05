using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class NotificationsController : Controller
{
    public readonly ApplicationDbContext context;
    private readonly IWebHostEnvironment environment;

    public  NotificationsController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        this.context = context;
        this.environment = environment;
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
            var recentHospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentMedicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var recentFuneralForms = context.Funeralburialform
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create notifications for hospital bills
            foreach (var bill in recentHospitalBills.Take(5))
            {
                var notificationId = $"hospital_{bill.Id}";
                var isRead = readNotificationIds.Contains(notificationId);

                notifications.Add(new
                {
                    id = notificationId,
                    title = "Hospital Bill Application",
                    message = $"Your hospital bill assistance application was submitted on {bill.CreatedAt:MMM dd, yyyy}",
                    isRead = isRead,
                    createdAt = bill.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = "application_submitted",
                    link = "/Uploads"
                });
            }

            // Create notifications for medical lab forms
            foreach (var medical in recentMedicalLabForms.Take(5))
            {
                var notificationId = $"medical_{medical.Id}";
                var isRead = readNotificationIds.Contains(notificationId);

                notifications.Add(new
                {
                    id = notificationId,
                    title = "Medical Procedure Application",
                    message = $"Your medical procedure application was submitted on {medical.CreatedAt:MMM dd, yyyy}",
                    isRead = isRead,
                    createdAt = medical.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = "application_submitted",
                    link = "/Uploads"
                });
            }

            // Create notifications for funeral forms
            foreach (var funeral in recentFuneralForms.Take(5))
            {
                var notificationId = $"funeral_{funeral.Id}";
                var isRead = readNotificationIds.Contains(notificationId);

                notifications.Add(new
                {
                    id = notificationId,
                    title = "Funeral Assistance Application",
                    message = $"Your funeral assistance application was submitted on {funeral.CreatedAt:MMM dd, yyyy}",
                    isRead = isRead,
                    createdAt = funeral.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    type = "application_submitted",
                    link = "/Uploads"
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
                    createdAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
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
            var recentHospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            var recentMedicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            var recentFuneralForms = context.Funeralburialform
                .Where(f => f.UserId == userId && f.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var bill in recentHospitalBills)
            {
                notificationIds.Add($"hospital_{bill.Id}");
            }

            foreach (var medical in recentMedicalLabForms)
            {
                notificationIds.Add($"medical_{medical.Id}");
            }

            foreach (var funeral in recentFuneralForms)
            {
                notificationIds.Add($"funeral_{funeral.Id}");
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
}