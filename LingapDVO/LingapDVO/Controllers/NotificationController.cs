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

        int userId = 0;
        int.TryParse(userIdString, out userId);

        var notifications = new List<object>();

        // Get read notification IDs from session (store as comma-separated string)
        var readNotificationsString = HttpContext.Session.GetString($"ReadNotifications_{userId}") ?? "";
        var readNotificationIds = new HashSet<string>(readNotificationsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

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
            .OrderByDescending(n => DateTime.Parse(((dynamic)n).createdAt))
            .ToList();

        // Prevent browser caching
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        return Json(orderedNotifications);
    }

    [HttpPost]
    public JsonResult MarkNotificationAsRead(string notificationId)
    {
        var userIdString = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdString))
        {
            return Json(new { success = false, error = "User not authenticated" });
        }

        int userId = 0;
        int.TryParse(userIdString, out userId);

        // Get current read notifications
        var readNotificationsString = HttpContext.Session.GetString($"ReadNotifications_{userId}") ?? "";
        var readNotificationIds = new List<string>(readNotificationsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

        // Add the new notification ID if not already present
        if (!string.IsNullOrEmpty(notificationId) && !readNotificationIds.Contains(notificationId))
        {
            readNotificationIds.Add(notificationId);

            // Update session (limit to 100 IDs to prevent session from getting too large)
            if (readNotificationIds.Count > 100)
            {
                readNotificationIds = readNotificationIds.Skip(readNotificationIds.Count - 100).ToList();
            }

            HttpContext.Session.SetString($"ReadNotifications_{userId}", string.Join(",", readNotificationIds));
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public JsonResult MarkAllNotificationsAsRead()
    {
        try
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            int userId = 0;
            int.TryParse(userIdString, out userId);

            // Get current notifications to mark them all as read
            var notifications = GetCurrentUserNotifications(userId);
            var notificationIds = notifications.Select(n => n.Id).ToList();

            // Store all notification IDs in session
            HttpContext.Session.SetString($"ReadNotifications_{userId}", string.Join(",", notificationIds));

            return Json(new { success = true, message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            // Log the exception
            return Json(new { success = false, error = "An error occurred while marking all notifications as read" });
        }
    }

    // Helper method to get current notifications
    private List<dynamic> GetCurrentUserNotifications(int userId)
    {
        var notifications = new List<dynamic>();

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
            notifications.Add(new { Id = $"hospital_{bill.Id}" });
        }

        foreach (var medical in recentMedicalLabForms)
        {
            notifications.Add(new { Id = $"medical_{medical.Id}" });
        }

        foreach (var funeral in recentFuneralForms)
        {
            notifications.Add(new { Id = $"funeral_{funeral.Id}" });
        }

        if (!notifications.Any())
        {
            notifications.Add(new { Id = "welcome_1" });
        }

        return notifications;
    }


}