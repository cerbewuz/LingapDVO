using LingapDVO.Hubs;
using LingapDVO.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    /// <summary>
    /// Recipient type for notifications
    /// </summary>
    public enum NotificationRecipientType
    {
        User = 1,   // Notifications for regular users
        Admin = 2   // Notifications for admin users
    }

    /// <summary>
    /// Unified notification service interface that handles both user and admin notifications
    /// </summary>
    public interface IUnifiedNotificationService
    {
        // ═══════════════════════════════════════════════════════════════
        // CORE NOTIFICATION METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Send a notification to a specific user or admin group
        /// </summary>
        Task SendNotificationAsync(
            NotificationRecipientType recipientType,
            int? userId,
            string title,
            string message,
            string type,
            string? link = null,
            string? applicationType = null,
            int? applicationId = null,
            string? applicantName = null,
            string priority = "normal");

        /// <summary>
        /// Send status change notification to a user
        /// </summary>
        Task SendStatusChangeNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            string status,
            int formId,
            string? comments = null,
            string? processedBy = null);

        /// <summary>
        /// Send delay notification to a user
        /// </summary>
        Task SendDelayNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            string priority,
            DateTime submittedDate,
            int applicationId);

        /// <summary>
        /// Send admin notification (broadcasts to all admins)
        /// </summary>
        Task SendAdminNotificationAsync(
            string notificationType,
            string assistanceType,
            int relatedId,
            int? userId,
            string applicantName,
            string title,
            string message,
            string? link = null,
            string priority = "normal");

        // ═══════════════════════════════════════════════════════════════
        // READ STATUS MANAGEMENT (for both user and admin)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get unread notification count
        /// </summary>
        Task<int> GetUnreadCountAsync(NotificationRecipientType recipientType, int? userId = null);

        /// <summary>
        /// Get recent notifications
        /// </summary>
        Task<List<Notification>> GetRecentNotificationsAsync(
            NotificationRecipientType recipientType,
            int? userId = null,
            int limit = 50);

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        Task MarkAsReadAsync(int notificationId, NotificationRecipientType recipientType);

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        Task MarkAllAsReadAsync(NotificationRecipientType recipientType, int? userId = null);
    }

    /// <summary>
    /// Unified notification service implementation
    /// Consolidates MultiChannelNotificationService and AdminNotificationService
    /// </summary>
    public class UnifiedNotificationService : IUnifiedNotificationService
    {
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnifiedNotificationService> _logger;
        private readonly IDateTimeService _dateTimeService;

        public UnifiedNotificationService(
            ISmsService smsService,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context,
            ILogger<UnifiedNotificationService> logger,
            IDateTimeService dateTimeService)
        {
            _smsService = smsService;
            _emailService = emailService;
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
            _dateTimeService = dateTimeService;
        }

        // ═══════════════════════════════════════════════════════════════
        // CORE NOTIFICATION METHODS
        // ═══════════════════════════════════════════════════════════════

        public async Task SendNotificationAsync(
            NotificationRecipientType recipientType,
            int? userId,
            string title,
            string message,
            string type,
            string? link = null,
            string? applicationType = null,
            int? applicationId = null,
            string? applicantName = null,
            string priority = "normal")
        {
            try
            {
                if (recipientType == NotificationRecipientType.User && userId.HasValue)
                {
                    await SendUserNotificationInternalAsync(userId.Value, title, message, type, link, applicationType, applicationId, applicantName);
                }
                else if (recipientType == NotificationRecipientType.Admin)
                {
                    await SendAdminNotificationInternalAsync(type, applicationType ?? "System", applicationId ?? 0, userId, applicantName ?? "System", title, message, link, priority);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification: recipientType={recipientType}, userId={userId}");
            }
        }

        private async Task SendUserNotificationInternalAsync(
            int userId,
            string title,
            string message,
            string type,
            string? link,
            string? applicationType,
            int? applicationId,
            string? applicantName)
        {
            // Get user preferences
            var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found");
                return;
            }

            // Get user's full name if not provided
            if (string.IsNullOrEmpty(applicantName))
            {
                var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == userId);
                applicantName = verifyAccount != null
                    ? $"{verifyAccount.Firstname} {verifyAccount.Middlename} {verifyAccount.Lastname}".Trim()
                    : user.Username ?? "User";
            }

            // Create and save notification
            var notification = new Notification
            {
                RecipientType = (int)NotificationRecipientType.User,
                UserId = userId,
                ApplicationType = applicationType ?? "System",
                ApplicationId = applicationId,
                ApplicantName = applicantName,
                Title = title,
                Message = message,
                Type = type,
                Link = link ?? "/Home",
                ProcessStage = "system",
                CreatedAt = _dateTimeService.Now,
                EventTimestamp = _dateTimeService.Now,
                SentViaInApp = user.PreferInAppNotification,
                SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                SentViaSms = user.PreferSmsNotification,
                NotificationIdentifier = $"system_{type}_{userId}_{_dateTimeService.Now.Ticks}",
                DisplayOrder = 0,
                IsRead = false,
                IsPermanent = type == "welcome"
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Send via preferred channels
            await SendViaUserChannelsAsync(user, notification, title, message, type, link);

            _logger.LogInformation($"User notification sent and saved for user {userId}");
        }

        private async Task SendViaUserChannelsAsync(UserAccount user, Notification notification, string title, string message, string type, string? link)
        {
            // Send in-app notification via SignalR if preferred
            if (user.PreferInAppNotification)
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = title,
                    message = message,
                    type = type,
                    link = link,
                    createdAt = _dateTimeService.Now,
                    isRead = false
                });
            }

            // Send email notification if preferred
            if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email))
            {
                var emailBody = GenerateEmailBody(title, message, link);
                await _emailService.SendEmailAsync(user.Email, title, emailBody);
            }

            // Send SMS notification if preferred
            if (user.PreferSmsNotification)
            {
                var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == user.Id);
                if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                {
                    var smsMessage = $"{title}: {message}";
                    await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                }
            }
        }

        private async Task SendAdminNotificationInternalAsync(
            string notificationType,
            string assistanceType,
            int relatedId,
            int? userId,
            string applicantName,
            string title,
            string message,
            string? link,
            string priority)
        {
            // Create and save admin notification
            var notification = new Notification
            {
                RecipientType = (int)NotificationRecipientType.Admin,
                UserId = userId,
                ApplicationType = assistanceType,
                ApplicationId = relatedId,
                ApplicantName = applicantName,
                Title = title,
                Message = message,
                Type = notificationType,
                Link = link ?? $"/{assistanceType}PendingStatus/{relatedId}",
                ProcessStage = "admin_notification",
                Priority = priority,
                CreatedAt = _dateTimeService.Now,
                EventTimestamp = _dateTimeService.Now,
                IsRead = false,
                IsArchived = false,
                SentViaInApp = true,
                NotificationIdentifier = $"admin_{notificationType}_{assistanceType}_{relatedId}_{_dateTimeService.Now.Ticks}",
                DisplayOrder = 0
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Get current unread count
            var unreadCount = await GetUnreadCountAsync(NotificationRecipientType.Admin);

            // Send real-time notification to all admin users via SignalR
            await _hubContext.Clients.Group("AdminUsers").SendAsync("ReceiveAdminNotification", new
            {
                id = notification.Id,
                notificationType = notificationType,
                assistanceType = assistanceType,
                relatedId = relatedId,
                userId = userId,
                applicantName = applicantName,
                title = title,
                message = message,
                link = notification.Link,
                priority = priority,
                createdAt = _dateTimeService.Now,
                unreadCount = unreadCount
            });

            _logger.LogInformation($"Admin notification sent: {notificationType} for {assistanceType}/{relatedId}");
        }

        public async Task SendStatusChangeNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            string status,
            int formId,
            string? comments = null,
            string? processedBy = null)
        {
            try
            {
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                var title = GetStatusTitle(status, formType);
                var message = GetStatusMessage(applicantName, formType, status, processedBy);
                var type = GetNotificationType(status);
                var link = GetStatusLink(status, formType, formId);
                var applicationType = MapFormTypeToApplicationType(formType);
                var processStage = GetProcessStage(status);

                // Create and save notification
                var notification = new Notification
                {
                    RecipientType = (int)NotificationRecipientType.User,
                    UserId = userId,
                    ApplicationType = applicationType,
                    ApplicationId = formId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    Status = status,
                    Comments = comments,
                    ProcessedBy = processedBy,
                    ProcessStage = processStage,
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    SentViaInApp = user.PreferInAppNotification,
                    SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                    SentViaSms = user.PreferSmsNotification,
                    NotificationIdentifier = $"{applicationType.ToLower()}_{formId}_{status.ToLower()}_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = GetDisplayOrder(status),
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Send via preferred channels
                await SendStatusViaChannelsAsync(user, notification, title, message, type, link, status, formType);

                _logger.LogInformation($"Status change notification sent for user {userId}, application {formId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending status change notification to user {userId}");
            }
        }

        private async Task SendStatusViaChannelsAsync(
            UserAccount user,
            Notification notification,
            string title,
            string message,
            string type,
            string link,
            string status,
            string formType)
        {
            // Send in-app notification via SignalR if preferred
            if (user.PreferInAppNotification)
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = title,
                    message = message,
                    type = type,
                    link = link,
                    createdAt = _dateTimeService.Now,
                    isRead = false
                });
            }

            // Skip email for statuses handled by Adminuser.cs with detailed templates
            var statusesHandledByAdmin = new[] { "Approve", "Disapprove", "Retake", "Processing" };
            if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email) && !statusesHandledByAdmin.Contains(status))
            {
                var emailBody = GenerateEmailBody(title, message, link);
                await _emailService.SendEmailAsync(user.Email, title, emailBody);
            }

            // Send SMS notification if preferred
            if (user.PreferSmsNotification)
            {
                var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == user.Id);
                if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                {
                    var formTypeDisplay = GetFormTypeDisplay(formType);
                    var smsMessage = FormatFormalSmsMessage(notification.ApplicantName ?? "Applicant", formTypeDisplay, title, message);
                    await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                }
            }
        }

        public async Task SendDelayNotificationAsync(
            int userId,
            string applicantName,
            string formType,
            string priority,
            DateTime submittedDate,
            int applicationId)
        {
            try
            {
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                var applicationType = MapFormTypeToApplicationType(formType);
                var formTypeDisplay = GetFormTypeDisplay(formType);
                var trackingLink = GetTrackingLink(formType);
                var hoursElapsed = (int)(_dateTimeService.Now - submittedDate).TotalHours;

                var title = priority == "Critical Delay"
                    ? "Critical Delay - Application Processing"
                    : "Standard Delay - Application Processing";

                var message = priority == "Critical Delay"
                    ? $"Your {formTypeDisplay} application has been pending for {hoursElapsed}+ hours. We sincerely apologize for the delay. Our team has prioritized your application and is working to process it as soon as possible."
                    : $"Your {formTypeDisplay} application has been pending for over an hour. Please don't worry - it's in our queue and will be reviewed shortly. Thank you for your patience.";

                // Create and save notification
                var notification = new Notification
                {
                    RecipientType = (int)NotificationRecipientType.User,
                    UserId = userId,
                    ApplicationType = applicationType,
                    ApplicationId = applicationId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = "delay",
                    Link = trackingLink,
                    ProcessStage = "delayed",
                    Priority = priority,
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    SentViaInApp = user.PreferInAppNotification,
                    SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                    SentViaSms = user.PreferSmsNotification,
                    NotificationIdentifier = $"{applicationType.ToLower()}_{applicationId}_delay_{priority.Replace(" ", "")}_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = 1,
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Send via channels
                await SendDelayViaChannelsAsync(user, notification, title, message, priority, trackingLink, applicantName, formTypeDisplay, submittedDate, hoursElapsed);

                _logger.LogInformation($"Delay notification ({priority}) sent for user {userId}, application {applicationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending delay notification to user {userId}");
            }
        }

        private async Task SendDelayViaChannelsAsync(
            UserAccount user,
            Notification notification,
            string title,
            string message,
            string priority,
            string trackingLink,
            string applicantName,
            string formTypeDisplay,
            DateTime submittedDate,
            int hoursElapsed)
        {
            if (user.PreferInAppNotification)
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = title,
                    message = message,
                    type = "delay",
                    priority = priority,
                    link = trackingLink,
                    createdAt = _dateTimeService.Now,
                    isRead = false
                });
            }

            if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email))
            {
                var emailBody = GenerateDelayEmailBody(title, message, applicantName, formTypeDisplay, submittedDate, hoursElapsed, trackingLink);
                await _emailService.SendEmailAsync(user.Email, title, emailBody);
            }

            if (user.PreferSmsNotification)
            {
                var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == user.Id);
                if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                {
                    var smsMessage = priority == "Critical Delay"
                        ? $"LingapDVO: URGENT - Dear {applicantName}, your {formTypeDisplay} has been delayed {hoursElapsed}+ hours. We apologize and are prioritizing your request."
                        : $"LingapDVO: Dear {applicantName}, your {formTypeDisplay} is in queue (1+ hour). It will be reviewed shortly. Thank you.";
                    await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                }
            }
        }

        public async Task SendAdminNotificationAsync(
            string notificationType,
            string assistanceType,
            int relatedId,
            int? userId,
            string applicantName,
            string title,
            string message,
            string? link = null,
            string priority = "normal")
        {
            await SendAdminNotificationInternalAsync(notificationType, assistanceType, relatedId, userId, applicantName, title, message, link, priority);
        }

        // ═══════════════════════════════════════════════════════════════
        // READ STATUS MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<int> GetUnreadCountAsync(NotificationRecipientType recipientType, int? userId = null)
        {
            var query = _context.Notifications
                .Where(n => n.RecipientType == (int)recipientType && !n.IsRead && !n.IsArchived);

            if (recipientType == NotificationRecipientType.User && userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(
            NotificationRecipientType recipientType,
            int? userId = null,
            int limit = 50)
        {
            var query = _context.Notifications
                .Where(n => n.RecipientType == (int)recipientType && !n.IsArchived);

            if (recipientType == NotificationRecipientType.User && userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            return await query
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, NotificationRecipientType recipientType)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientType == (int)recipientType);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = _dateTimeService.Now;
                await _context.SaveChangesAsync();

                // Update unread count via SignalR
                if (recipientType == NotificationRecipientType.Admin)
                {
                    var unreadCount = await GetUnreadCountAsync(NotificationRecipientType.Admin);
                    await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateAdminNotificationCount", unreadCount);
                }
                else if (notification.UserId.HasValue)
                {
                    var unreadCount = await GetUnreadCountAsync(NotificationRecipientType.User, notification.UserId);
                    await _hubContext.Clients.User(notification.UserId.Value.ToString()).SendAsync("UpdateNotificationCount", unreadCount);
                }
            }
        }

        public async Task MarkAllAsReadAsync(NotificationRecipientType recipientType, int? userId = null)
        {
            var query = _context.Notifications
                .Where(n => n.RecipientType == (int)recipientType && !n.IsRead && !n.IsArchived);

            if (recipientType == NotificationRecipientType.User && userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            var unreadNotifications = await query.ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = _dateTimeService.Now;
                }

                await _context.SaveChangesAsync();

                // Update unread count via SignalR
                if (recipientType == NotificationRecipientType.Admin)
                {
                    await _hubContext.Clients.Group("AdminUsers").SendAsync("UpdateAdminNotificationCount", 0);
                }
                else if (userId.HasValue)
                {
                    await _hubContext.Clients.User(userId.Value.ToString()).SendAsync("UpdateNotificationCount", 0);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        private string MapFormTypeToApplicationType(string formType)
        {
            return formType switch
            {
                "HospitalAssistance" => "HospitalAssistance",
                "HospitalBill" => "HospitalAssistance",
                "OtherAssistance" => "OtherAssistance",
                "Medical" => "OtherAssistance",
                "FuneralAssistance" => "FuneralAssistance",
                "Funeral" => "FuneralAssistance",
                _ => "System"
            };
        }

        private string GetFormTypeDisplay(string formType)
        {
            return formType switch
            {
                "HospitalAssistance" => "Hospital Assistance",
                "HospitalBill" => "Hospital Assistance",
                "OtherAssistance" => "Medical and Laboratory Assistance",
                "MedicalLab" => "Medical and Laboratory Assistance",
                "Medical" => "Medical and Laboratory Assistance",
                "FuneralAssistance" => "Funeral Assistance",
                "Funeral" => "Funeral Assistance",
                _ => "Assistance"
            };
        }

        private string GetStatusLink(string status, string formType, int formId)
        {
            if (status == "Retake")
            {
                return formType switch
                {
                    "HospitalAssistance" => $"/Dashboard/HospitalAssistanceEdit/{formId}",
                    "OtherAssistance" => $"/Dashboard/OtherAssistanceedit/{formId}",
                    "FuneralAssistance" => $"/Dashboard/FuneralAssistanceEdit/{formId}",
                    _ => "/Applicationtracking"
                };
            }

            var tabParam = formType switch
            {
                "HospitalAssistance" => "hospital",
                "OtherAssistance" => "medical",
                "FuneralAssistance" => "funeral",
                _ => ""
            };
            return string.IsNullOrEmpty(tabParam) ? "/Applicationtracking" : $"/Applicationtracking?tab={tabParam}";
        }

        private string GetTrackingLink(string formType)
        {
            var tabParam = formType switch
            {
                "HospitalAssistance" => "hospital",
                "OtherAssistance" => "medical",
                "FuneralAssistance" => "funeral",
                _ => ""
            };
            return string.IsNullOrEmpty(tabParam) ? "/Applicationtracking" : $"/Applicationtracking?tab={tabParam}";
        }

        private string GetProcessStage(string status)
        {
            return status switch
            {
                "Pending" => "submitted",
                "Processing" => "processing",
                "Approve" => "result",
                "Disapprove" => "result",
                "Retake" => "retake",
                "Claimed" => "claimed",
                _ => "submitted"
            };
        }

        private int GetDisplayOrder(string status)
        {
            return status switch
            {
                "Pending" => 0,
                "Processing" => 1,
                "Retake" => 2,
                "Approve" => 3,
                "Disapprove" => 3,
                "Claimed" => 4,
                _ => 0
            };
        }

        private string GetStatusTitle(string status, string formType)
        {
            var formTypeDisplay = GetFormTypeDisplay(formType);
            var normalizedStatus = NormalizeStatus(status);

            return normalizedStatus switch
            {
                "Pending" => $"Application Submitted - {formTypeDisplay}",
                "Processing" => $"Application Under Review - {formTypeDisplay}",
                "Approve" => $"Application Approved - {formTypeDisplay}",
                "Disapprove" => $"Application Not Approved - {formTypeDisplay}",
                "Retake" => $"Action Required - Document Resubmission",
                "Resubmitted" => $"Documents Resubmitted - {formTypeDisplay}",
                "Claimed" => $"Assistance Released - {formTypeDisplay}",
                _ => $"Status Update - {formTypeDisplay}"
            };
        }

        private string GetStatusMessage(string applicantName, string formType, string status, string? processedBy = null)
        {
            var formTypeDisplay = GetFormTypeDisplay(formType);
            var normalizedStatus = NormalizeStatus(status);

            return normalizedStatus switch
            {
                "Pending" => $"Your {formTypeDisplay} application has been submitted successfully and is now in our processing queue.",
                "Processing" => !string.IsNullOrWhiteSpace(processedBy)
                    ? $"Your {formTypeDisplay} application is now being reviewed by {processedBy}."
                    : $"Your {formTypeDisplay} application is now being reviewed by our team.",
                "Approve" => $"Congratulations! Your {formTypeDisplay} application has been approved. Please visit our office to claim your assistance.",
                "Disapprove" => $"We regret to inform you that your {formTypeDisplay} application was not approved. Please contact our office for details.",
                "Retake" => $"Your {formTypeDisplay} application requires document corrections. Please check and upload the corrected documents.",
                "Resubmitted" => $"Your {formTypeDisplay} documents have been resubmitted successfully. Our team will review them shortly.",
                "Claimed" => $"Your {formTypeDisplay} has been successfully released. Thank you for using LingapDVO services.",
                _ => $"Your {formTypeDisplay} application status has been updated."
            };
        }

        private string GetNotificationType(string status)
        {
            return NormalizeStatus(status) switch
            {
                "Pending" => "application_submitted",
                "Processing" => "application_processing",
                "Approve" => "application_approved",
                "Disapprove" => "application_disapproved",
                "Retake" => "application_retake",
                "Resubmitted" => "application_resubmitted",
                "Claimed" => "application_claimed",
                _ => "status_change"
            };
        }

        private string NormalizeStatus(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Pending",
                "processing" => "Processing",
                "approve" => "Approve",
                "disapprove" => "Disapprove",
                "retake" => "Retake",
                "resubmitted" => "Resubmitted",
                "claimed" => "Claimed",
                _ => status ?? ""
            };
        }

        private string FormatFormalSmsMessage(string applicantName, string formTypeDisplay, string title, string message)
        {
            // Keep SMS concise due to character limits
            return $"LingapDVO: Dear {applicantName}, {title}. Check your application for details.";
        }

        private string GenerateEmailBody(string title, string message, string? link)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc143c; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #dc143c; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{title}</h2>
        </div>
        <div class='content'>
            <p>{message}</p>
            {(string.IsNullOrEmpty(link) ? "" : $"<a href='https://lingap.online{link}' class='button'>View Details</a>")}
        </div>
        <div class='footer'>
            <p>LingapDVO - City Social Welfare and Development Office</p>
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateDelayEmailBody(string title, string message, string applicantName, string formTypeDisplay, DateTime submittedDate, int hoursElapsed, string trackingLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ff9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .details {{ background-color: #fff; padding: 15px; margin: 15px 0; border-left: 4px solid #ff9800; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #dc143c; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{title}</h2>
        </div>
        <div class='content'>
            <p>Dear {applicantName},</p>
            <p>{message}</p>
            <div class='details'>
                <p><strong>Application Type:</strong> {formTypeDisplay}</p>
                <p><strong>Submitted:</strong> {submittedDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                <p><strong>Time Elapsed:</strong> {hoursElapsed} hours</p>
            </div>
            <a href='https://lingap.online{trackingLink}' class='button'>Track Application</a>
        </div>
        <div class='footer'>
            <p>LingapDVO - City Social Welfare and Development Office</p>
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
