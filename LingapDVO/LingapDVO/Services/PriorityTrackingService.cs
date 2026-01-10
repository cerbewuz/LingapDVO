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
        private readonly IDateTimeService _dateTimeService;
        private readonly IMultiChannelNotificationService _notificationService;

        public PriorityTrackingService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<PriorityTrackingService> logger,
            IDateTimeService dateTimeService,
            IMultiChannelNotificationService notificationService)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _dateTimeService = dateTimeService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Calculate priority level based on processing time (from submission to completion)
        /// </summary>
        public string CalculatePriority(DateTime createdAt, DateTime completedAt)
        {
            var processingHours = (completedAt - createdAt).TotalHours;

            if (processingHours >= 2)
                return "high";
            else if (processingHours >= 1)
                return "medium";
            else
                return "normal";
        }

        /// <summary>
        /// Get all priority applications counts
        /// Tracks ONLY Pending or Processing applications that have NOT been approved/disapproved/claims
        /// Priority is based on waiting time (time since CreatedAt to now)
        /// Excludes applications with Status2 = "approve" or "disapprove"
        /// Excludes applications with Status3 = "claims" or "claimed"
        /// High Priority: 2+ hours waiting | Medium Priority: 1-2 hours waiting
        /// </summary>
        public async Task<(int high, int medium, int total)> GetPriorityCountsAsync()
        {
            // Get ONLY Pending, Processing, or Retake medical/other assistance applications
            // EXCLUDE applications that have been approved/disapproved, claims, or removed
            // For Retake status, use Result time as the start time for priority calculation
            var medicalApps = await _context.OtherAssistance
                .Where(m => m.Status != "Removed"
                    && (m.Status == "pending" || m.Status == "processing" || m.Status2 == "Retake")
                    && (m.Status2 == null || m.Status2 == "" || m.Status2 == "Retake" || (m.Status2.ToLower() != "approve" && m.Status2.ToLower() != "disapprove"))
                    && (m.Status3 == null || m.Status3 == "" || (m.Status3.ToLower() != "claims" && m.Status3.ToLower() != "claimed")))
                .Select(m => new { m.CreatedAt, m.Result, m.Status2 })
                .ToListAsync();

            // Get ONLY Pending, Processing, or Retake funeral assistance applications
            // EXCLUDE applications that have been approved/disapproved, claims, or removed
            // For Retake status, use Result time as the start time for priority calculation
            var funeralApps = await _context.FuneralAssistance
                .Where(f => f.Status != "Removed"
                    && (f.Status == "pending" || f.Status == "processing" || f.Status2 == "Retake")
                    && (f.Status2 == null || f.Status2 == "" || f.Status2 == "Retake" || (f.Status2.ToLower() != "approve" && f.Status2.ToLower() != "disapprove"))
                    && (f.Status3 == null || f.Status3 == "" || (f.Status3.ToLower() != "claims" && f.Status3.ToLower() != "claimed")))
                .Select(f => new { f.CreatedAt, f.Result, f.Status2 })
                .ToListAsync();

            // Get ONLY Pending, Processing, or Retake hospital assistance applications
            // EXCLUDE applications that have been approved/disapproved, claims, or removed
            // For Retake status, use Result time as the start time for priority calculation
            var hospitalApps = await _context.HospitalAssistance
                .Where(h => h.Status != "Removed"
                    && (h.Status == "pending" || h.Status == "processing" || h.Status2 == "Retake")
                    && (h.Status2 == null || h.Status2 == "" || h.Status2 == "Retake" || (h.Status2.ToLower() != "approve" && h.Status2.ToLower() != "disapprove"))
                    && (h.Status3 == null || h.Status3 == "" || (h.Status3.ToLower() != "claims" && h.Status3.ToLower() != "claimed")))
                .Select(h => new { h.CreatedAt, h.Result, h.Status2 })
                .ToListAsync();

            // Combine all pending/processing/retake applications from all three application types
            var allApplications = medicalApps.Concat(funeralApps).Concat(hospitalApps).ToList();

            var now = _dateTimeService.Now;

            // Calculate time waiting for each application
            // For Retake status: use Result time (when retake was initiated) as start time
            // For others: use CreatedAt (submission time) as start time
            var waitingTimes = allApplications
                .Select(app => {
                    var startTime = app.Status2 == "Retake" && app.Result > DateTime.MinValue
                        ? app.Result
                        : app.CreatedAt;
                    return (now - startTime).TotalHours;
                })
                .ToList();

            // Calculate priority counts based on waiting time
            // High priority: applications waiting 2+ hours
            int highPriority = waitingTimes.Count(hours => hours >= 2);
            // Medium priority: applications waiting 1-2 hours
            int mediumPriority = waitingTimes.Count(hours => hours >= 1 && hours < 2);
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
                    timestamp = _dateTimeService.Now
                });

                // Also update the sidebar badge count
                await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateSidebarBadge", new
                {
                    count = total,
                    timestamp = _dateTimeService.Now
                });

                _logger.LogInformation($"Priority counts broadcasted: High={high}, Medium={medium}, Total={total}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting priority counts");
            }
        }

        /// <summary>
        /// Check for delayed applications and send notifications to users
        /// Only checks Pending and Processing applications
        /// Completed statuses (Approve, Disapprove, Claims) are excluded
        /// Sends notification for applications delayed 1+ hours
        /// </summary>
        public async Task CheckDelayedApplicationsAsync()
        {
            try
            {
                var now = _dateTimeService.Now;
                var twoHoursAgo = now.AddHours(-2);
                var oneHourAgo = now.AddHours(-1);

                // Check medical/other assistance applications (ONLY Pending and Processing, exclude Removed)
                var delayedMedical = await _context.OtherAssistance
                    .Where(m => m.Status != "Removed"
                        && (m.Status == "pending" || m.Status == "processing") 
                        && (m.Status2 == null || m.Status2 == "" || (m.Status2.ToLower() != "approve" && m.Status2.ToLower() != "disapprove"))
                        && (m.Status3 == null || m.Status3 == "" || (m.Status3.ToLower() != "claims" && m.Status3.ToLower() != "claimed"))
                        && m.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedMedical)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "Critical Delay" : "Standard Delay";

                    // Get applicant name
                    var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == app.UserId);
                    var applicantName = verifyAccount != null
                        ? $"{verifyAccount.Firstname} {verifyAccount.Lastname}".Trim()
                        : "Applicant";

                    // Check if delay notification already sent for this application
                    var existingDelayNotification = await _context.Notifications
                        .AnyAsync(n => n.UserId == app.UserId 
                            && n.ApplicationId == app.Id 
                            && n.ApplicationType == "OtherAssistance" 
                            && n.Type == "delay"
                            && n.Title.Contains(priority));

                    if (!existingDelayNotification)
                    {
                        // Send multi-channel delay notification (saves to database)
                        await _notificationService.SendDelayNotificationAsync(
                            app.UserId,
                            applicantName,
                            "OtherAssistance",
                            priority,
                            app.CreatedAt,
                            app.Id
                        );

                        // Also send SignalR real-time notification
                        await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                        {
                            priority = priority,
                            hoursElapsed = hoursElapsed,
                            applicationType = "Other Assistance",
                            message = GetDelayMessage(priority, hoursElapsed),
                            formId = app.Id,
                            timestamp = _dateTimeService.Now
                        });

                        _logger.LogInformation($"Delay notification sent to User {app.UserId} for Other Assistance Form {app.Id} - Priority: {priority}");
                    }
                }

                // Check funeral assistance applications (ONLY Pending and Processing, exclude Removed)
                var delayedFuneral = await _context.FuneralAssistance
                    .Where(f => f.Status != "Removed"
                        && (f.Status == "pending" || f.Status == "processing")
                        && (f.Status2 == null || f.Status2 == "" || (f.Status2.ToLower() != "approve" && f.Status2.ToLower() != "disapprove"))
                        && (f.Status3 == null || f.Status3 == "" || (f.Status3.ToLower() != "claims" && f.Status3.ToLower() != "claimed"))
                        && f.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedFuneral)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "Critical Delay" : "Standard Delay";

                    // Get applicant name
                    var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == app.UserId);
                    var applicantName = verifyAccount != null
                        ? $"{verifyAccount.Firstname} {verifyAccount.Lastname}".Trim()
                        : "Applicant";

                    // Check if delay notification already sent for this application
                    var existingDelayNotification = await _context.Notifications
                        .AnyAsync(n => n.UserId == app.UserId 
                            && n.ApplicationId == app.Id 
                            && n.ApplicationType == "FuneralAssistance" 
                            && n.Type == "delay"
                            && n.Title.Contains(priority));

                    if (!existingDelayNotification)
                    {
                        // Send multi-channel delay notification (saves to database)
                        await _notificationService.SendDelayNotificationAsync(
                            app.UserId,
                            applicantName,
                            "FuneralAssistance",
                            priority,
                            app.CreatedAt,
                            app.Id
                        );

                        // Also send SignalR real-time notification
                        await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                        {
                            priority = priority,
                            hoursElapsed = hoursElapsed,
                            applicationType = "Funeral Assistance",
                            message = GetDelayMessage(priority, hoursElapsed),
                            formId = app.Id,
                            timestamp = _dateTimeService.Now
                        });

                        _logger.LogInformation($"Delay notification sent to User {app.UserId} for Funeral Assistance Form {app.Id} - Priority: {priority}");
                    }
                }

                // Check hospital assistance applications (ONLY Pending and Processing, exclude Removed)
                var delayedHospital = await _context.HospitalAssistance
                    .Where(h => h.Status != "Removed"
                        && (h.Status == "pending" || h.Status == "processing")
                        && (h.Status2 == null || h.Status2 == "" || (h.Status2.ToLower() != "approve" && h.Status2.ToLower() != "disapprove"))
                        && (h.Status3 == null || h.Status3 == "" || (h.Status3.ToLower() != "claims" && h.Status3.ToLower() != "claimed"))
                        && h.CreatedAt <= oneHourAgo)
                    .ToListAsync();

                foreach (var app in delayedHospital)
                {
                    var hoursElapsed = (now - app.CreatedAt).TotalHours;
                    var priority = hoursElapsed >= 2 ? "Critical Delay" : "Standard Delay";

                    // Get applicant name
                    var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == app.UserId);
                    var applicantName = verifyAccount != null
                        ? $"{verifyAccount.Firstname} {verifyAccount.Lastname}".Trim()
                        : "Applicant";

                    // Check if delay notification already sent for this application
                    var existingDelayNotification = await _context.Notifications
                        .AnyAsync(n => n.UserId == app.UserId 
                            && n.ApplicationId == app.Id 
                            && n.ApplicationType == "HospitalAssistance" 
                            && n.Type == "delay"
                            && n.Title.Contains(priority));

                    if (!existingDelayNotification)
                    {
                        // Send multi-channel delay notification (saves to database)
                        await _notificationService.SendDelayNotificationAsync(
                            app.UserId,
                            applicantName,
                            "HospitalAssistance",
                            priority,
                            app.CreatedAt,
                            app.Id
                        );

                        // Also send SignalR real-time notification
                        await _hubContext.Clients.Group($"User_{app.UserId}").SendAsync("ReceiveDelayNotification", new
                        {
                            priority = priority,
                            hoursElapsed = hoursElapsed,
                            applicationType = "Hospital Assistance",
                            message = GetDelayMessage(priority, hoursElapsed),
                            formId = app.Id,
                            timestamp = _dateTimeService.Now
                        });

                        _logger.LogInformation($"Delay notification sent to User {app.UserId} for Hospital Assistance Form {app.Id} - Priority: {priority}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delayed applications");
            }
        }

        /// <summary>
        /// Notify admins of new priority application
        /// Note: For pending applications, uses current time to calculate wait time
        /// </summary>
        public async Task NotifyNewPriorityApplicationAsync(string applicationType, string applicantName, int formId, DateTime applicationDate)
        {
            try
            {
                // For pending applications, calculate wait time using current time
                var priority = CalculatePriority(applicationDate, _dateTimeService.Now);

                // Only notify if medium or high priority
                if (priority == "medium" || priority == "high")
                {
                    await _hubContext.Clients.Group("AdminUsers").SendAsync("ReceiveNewPriorityApplication", new
                    {
                        applicationType = applicationType,
                        applicantName = applicantName,
                        priority = priority,
                        formId = formId,
                        timestamp = _dateTimeService.Now
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
                    timestamp = _dateTimeService.Now
                });

                _logger.LogInformation($"Status update notification sent to User {userId}: {applicationType} - {status}");

                // If status changed to Processing/Approve/Disapprove, update priority counts
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
            if (priority == "Critical Delay")
            {
                return $"Your application has been pending for {Math.Floor(hoursElapsed)} hours. " +
                       "We apologize for the delay. Our team is working on it with high priority and will process it as soon as possible.";
            }
            else if (priority == "Standard Delay")
            {
                return "Your application has been pending for over an hour. " +
                       "Don't worry! It's in our queue and will be reviewed shortly.";
            }
            return "";
        }
    }
}
