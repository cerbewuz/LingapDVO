using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class NotificationsController : Controller
{
    [HttpGet]
    public IActionResult GetUserNotifications()
    {
        // Temporary demo data - replace with your actual data
        var notifications = new[]
        {
            new {
                id = 1,
                title = "Application Submitted",
                message = "Your hospital bill assistance application has been submitted successfully.",
                isRead = false,
                createdAt = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ss"),
                link = "/Uploads"
            }
        };

        return Json(notifications);
    }

    [HttpPost]
    public IActionResult MarkAsRead(int id)
    {
        // Add your logic to mark notification as read
        return Ok();
    }

    [HttpPost]
    public IActionResult MarkAllAsRead()
    {
        // Add your logic to mark all notifications as read
        return Ok();
    }
}