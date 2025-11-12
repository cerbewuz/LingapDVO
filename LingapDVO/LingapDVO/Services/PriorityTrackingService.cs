using LingapDVO.Hubs;
using LingapDVO.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    /// <summary>
    /// Service for tracking and managing priority applications with real-time notifications
    /// </summary>
    public class PriorityTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<PriorityTrackingService> _logger;

        public PriorityTrackingService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<PriorityTrackingService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Calculate priority level based on hours elapsed since creation
        /// </summary>
        public string CalculatePriority(DateTime createdAt)
        {
            var hoursElapsed = (DateTime.Now - createdAt).TotalHours;

            if (hoursElapsed >= 2)
                return "high";
            else if (hoursElapsed >= 1)
                return "medium";
            else
                return "normal";
        }

        /// <summary>
        /// Get all priority applications counts
        /// </summary>
        public async Task<(int high, int medium, int total)> GetPriorityCountsAsync()
        {
            var now = DateTime.Now;
            var twoHoursAgo = now.AddHours(-2);
            var oneHourAgo = now.AddHours(-1);

            // Get pending medical/other assistance applications
            var medicalApps = await _context.OtherAssistance
                .Where(m => m.Status == "Pending")
                .Select(m => m.CreatedAt)
                .ToListAsync();

            // Get pending funeral assistance applications
            var funeralApps = await _context.FuneralAssistance
                .Where(f => f.Status == "Pending")
                .Select(f => f.CreatedAt)
                .ToListAsync();

            // Get pending hospital assistance applications
            var hospitalApps = await _context.HospitalAssistance
                .Where(h => h.Status == "Pending")
                .Select(h => h.CreatedAt)
                .ToListAsync();

            // Combine all pending applications
            var allPendingDates = medicalApps.Concat(funeralApps).Concat(hospitalApps).ToList();

            // Calculate priority counts
            int highPriority = allPendingDates.Count(d => d <= twoHoursAgo);
            int mediumPriority = allPendingDates.Count(d => d > twoHoursAgo && d <= oneHourAgo);
            int totalPriority = highPriority + mediumPriority;

            return (highPriority, mediumPriority, totalPriority);
        }

        /// <summary>
        /// Broadcast priority counts to all admin users
        /// </summary>
        public async Task BroadcastPriorityCountsAsync()
        {
            try
            {
                var (high, medium, total) = await GetPriorityCountsAsync();

                await _hubContext.Clients.Group("AdminUsers").SendAsync("ReceivePriorityCountUpdate", new
                {
                    highPriority = high,
                    mediumPriority = medium,
                    totalPriority = total,
                    timestamp = DateTime.UtcNow
                });

                // Also update the sidebar badge count
                await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateSidebarBadge", new
                {
                    count = total,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Priority counts broadcasted: High={high}, Medium={medium}, Total={total}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting priority counts");
            }
        }

        /// <summary>
        /// Check for delayed applications and send notifications
        /// </summary>
        public async Task CheckDelayedApplicationsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var twoHoursAgo = now.AddHours(-2);
                var oneHourAgo = now.AddHours(-1);

                // Check medical/other assistance applications
                var delayedMedical = await _context.OtherAssistance
                    .Where(m => m.Status == "Pending" && m.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedMedical)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "high" : "medium";

                    // Notify user about delay
                    await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                    {
                        priority = priority,
                        hoursElapsed = hoursElapsed,
                        applicationType = "Other Assistance",
                        message = GetDelayMessage(priority, hoursElapsed),
                        formId = app.Id,
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Delay notification sent to User {app.UserId} for Other Assistance Form {app.Id}");
                }

                // Check funeral assistance applications
                var delayedFuneral = await _context.FuneralAssistance
                    .Where(f => f.Status == "Pending" && f.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedFuneral)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "high" : "medium";

                    // Notify user about delay
                    await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                    {
                        priority = priority,
                        hoursElapsed = hoursElapsed,
                        applicationType = "Funeral Assistance",
                        message = GetDelayMessage(priority, hoursElapsed),
                        formId = app.Id,
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Delay notification sent to User {app.UserId} for Funeral Assistance Form {app.Id}");
                }

                // Check hospital assistance applications
                var delayedHospital = await _context.HospitalAssistance
                    .Where(h => h.Status == "Pending" && h.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedHospital)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "high" : "medium";

                    // Notify user about delay
                    await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                    {
                        priority = priority,
                        hoursElapsed = hoursElapsed,
                        applicationType = "Hospital Assistance",
                        message = GetDelayMessage(priority, hoursElapsed),
                        formId = app.Id,
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Delay notification sent to User {app.UserId} for Hospital Assistance Form {app.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delayed applications");
            }
        }

        /// <summary>
        /// Notify admins of new priority application
        /// </summary>
        public async Task NotifyNewPriorityApplicationAsync(string applicationType, string applicantName, int formId, DateTime applicationDate)
        {
            try
            {
                var priority = CalculatePriority(applicationDate);

                // Only notify if medium or high priority
                if (priority == "medium" || priority == "high")
                {
                    await _hubContext.Clients.Group("AdminUsers").SendAsync("ReceiveNewPriorityApplication", new
                    {
                        applicationType = applicationType,
                        applicantName = applicantName,
                        priority = priority,
                        formId = formId,
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"New priority application notification sent: {applicationType} - {applicantName} ({priority})");
                }

                // Always broadcast updated counts
                await BroadcastPriorityCountsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying new priority application");
            }
        }

        /// <summary>
        /// Notify user and admins of status update
        /// </summary>
        public async Task NotifyStatusUpdateAsync(int userId, int formId, string status, string applicationType)
        {
            try
            {
                // Notify user
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveStatusUpdate", new
                {
                    formId = formId,
                    status = status,
                    applicationType = applicationType,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Status update notification sent to User {userId}: {applicationType} - {status}");

                // If status changed to Processing/Approved/Disapproved, update priority counts
                if (status != "Pending")
                {
                    await BroadcastPriorityCountsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying status update");
            }
        }

        /// <summary>
        /// Get delay message based on priority
        /// </summary>
        private string GetDelayMessage(string priority, double hoursElapsed)
        {
            if (priority == "high")
            {
                return $"Your application has been pending for {Math.Floor(hoursElapsed)} hours. " +
                       "We apologize for the delay. Our team is working on it with high priority and will process it ASAP.";
            }
            else if (priority == "medium")
            {
                return "Your application has been pending for over an hour. " +
                       "Don't worry! It's in our queue and will be reviewed shortly.";
            }
            return "";
        }
    }
}
