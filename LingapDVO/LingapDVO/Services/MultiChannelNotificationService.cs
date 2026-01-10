using LingapDVO.Hubs;
using LingapDVO.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    public interface IMultiChannelNotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string type, string? link = null);
        Task SendStatusChangeNotificationAsync(int userId, string applicantName, string formType, string status, int formId, string? comments = null, string? processedBy = null);
        Task SendDelayNotificationAsync(int userId, string applicantName, string formType, string priority, DateTime submittedDate, int applicationId);
    }

    public class MultiChannelNotificationService : IMultiChannelNotificationService
    {
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MultiChannelNotificationService> _logger;
        private readonly IDateTimeService _dateTimeService;

        public MultiChannelNotificationService(
            ISmsService smsService,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context,
            ILogger<MultiChannelNotificationService> logger,
            IDateTimeService dateTimeService)
        {
            _smsService = smsService;
            _emailService = emailService;
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
            _dateTimeService = dateTimeService;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type, string? link = null)
        {
            try
            {
                // Get user preferences
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Get user's full name
                var verifyAccount = await _context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == userId);
                var userName = verifyAccount != null
                    ? $"{verifyAccount.Firstname} {verifyAccount.Middlename} {verifyAccount.Lastname}".Trim()
                    : user.Username ?? "User";

                // Save notification to database FIRST
                var notification = new Notification
                {
                    UserId = userId,
                    ApplicationType = "System", // Generic system notification
                    ApplicationId = null,
                    ApplicantName = userName,
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

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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
                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"{title}: {message}";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel notification sent and saved to database for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel notification to user {userId}");
            }
        }

        public async Task SendStatusChangeNotificationAsync(int userId, string applicantName, string formType, string status, int formId, string? comments = null, string? processedBy = null)
        {
            var title = GetStatusTitle(status, formType);
            var message = GetStatusMessage(applicantName, formType, status, processedBy);
            var type = GetNotificationType(status);

            // Generate appropriate link based on status and form type
            string link;
            if (status == "Retake")
            {
                // For retake status, link to the specific edit form
                link = formType switch
                {
                    "HospitalAssistance" => $"/Dashboard/HospitalAssistanceEdit/{formId}",
                    "OtherAssistance" => $"/Dashboard/OtherAssistanceedit/{formId}",
                    "FuneralAssistance" => $"/Dashboard/FuneralAssistanceEdit/{formId}",
                    _ => "/Applicationtracking"
                };
            }
            else
            {
                // For other statuses, link to application tracking with the appropriate tab
                var tabParam = formType switch
                {
                    "HospitalAssistance" => "hospital",
                    "OtherAssistance" => "medical",
                    "FuneralAssistance" => "funeral",
                    _ => ""
                };
                link = string.IsNullOrEmpty(tabParam) ? "/Applicationtracking" : $"/Applicationtracking?tab={tabParam}";
            }

            // Special handling for "Approved" status to include nearby offices link
            if (status == "Approve")
            {
                await SendApprovedNotificationAsync(userId, applicantName, title, message, type, link, formType, formId, comments, processedBy);
            }
            // Special handling for "Claimed" status to include feedback link
            else if (status == "Claimed")
            {
                await SendClaimedNotificationAsync(userId, applicantName, title, message, type, link, formType, formId, comments, processedBy);
            }
            // Special handling for "Retake" status to include edit form link
            else if (status == "Retake")
            {
                await SendRetakeNotificationAsync(userId, applicantName, title, message, type, link, formType, formId, comments, processedBy);
            }
            else
            {
                await SendStatusNotificationAsync(userId, applicantName, title, message, type, link, formType, formId, status, comments, processedBy);
            }
        }

        private async Task SendStatusNotificationAsync(int userId, string applicantName, string title, string message, string type, string link, string formType, int formId, string status, string? comments, string? processedBy)
        {
            try
            {
                // Get user preferences
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Map formType to ApplicationType
                var applicationType = MapFormTypeToApplicationType(formType);

                // Get process stage based on status
                var processStage = GetProcessStage(status);

                // **SAVE NOTIFICATION TO DATABASE FIRST**
                var notification = new Notification
                {
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

                // Get assistance type display
                var formTypeDisplay = formType switch
                {
                    "HospitalAssistance" => "Hospital Assistance",
                    "OtherAssistance" => "Other Assistance",
                    "FuneralAssistance" => "Funeral Assistance",
                    _ => "Financial Assistance"
                };

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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
                // NOTE: Skip email for statuses that are handled by Adminuser.cs with detailed templates
                // This prevents duplicate emails from being sent
                var statusesHandledByAdmin = new[] { "Approve", "Disapprove", "Retake", "Processing" };
                if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email) && !statusesHandledByAdmin.Contains(status))
                {
                    var emailBody = GenerateEmailBody(title, message, link);
                    await _emailService.SendEmailAsync(user.Email, title, emailBody);
                }

                // Send SMS notification if preferred (formal and concise automated message)
                if (user.PreferSmsNotification)
                {
                    var verifyAccount = await _context.VerifiedAccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        // Format formal, concise SMS message
                        var smsMessage = FormatFormalSmsMessage(applicantName, formTypeDisplay, title, message);
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel status notification sent and saved to database for user {userId}, application {formId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel status notification to user {userId}");
            }
        }

        private async Task SendApprovedNotificationAsync(int userId, string applicantName, string title, string message, string type, string link, string formType, int formId, string? comments, string? processedBy)
        {
            try
            {
                // Get user preferences
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Map formType to ApplicationType
                var applicationType = MapFormTypeToApplicationType(formType);

                // **SAVE NOTIFICATION TO DATABASE FIRST**
                var notification = new Notification
                {
                    UserId = userId,
                    ApplicationType = applicationType,
                    ApplicationId = formId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    Status = "Approved",
                    Status2 = "Approve",
                    Comments = comments,
                    ProcessedBy = processedBy,
                    ProcessStage = "result",
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    SentViaInApp = user.PreferInAppNotification,
                    SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                    SentViaSms = user.PreferSmsNotification,
                    NotificationIdentifier = $"{applicationType.ToLower()}_{formId}_approved_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = 3,
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Get assistance type display
                var formTypeDisplay = formType switch
                {
                    "HospitalAssistance" => "Hospital Assistance",
                    "OtherAssistance" => "Other Assistance",
                    "FuneralAssistance" => "Funeral Assistance",
                    _ => "Financial Assistance"
                };

                var nearbyOfficesUrl = "https://lingap.online/Nearbyoffices";

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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

                // NOTE: Email notification is handled by Adminuser.cs with detailed templates
                // Skipping generic email here to avoid duplicate emails being sent

                // Send SMS notification with nearby offices link if preferred (formal and concise)
                if (user.PreferSmsNotification)
                {
                    var verifyAccount = await _context.VerifiedAccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"LingapDVO: Dear {applicantName}, your {formTypeDisplay} request is approved! Please visit our office to get your help. Locate nearby offices: {nearbyOfficesUrl}";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel approved notification sent and saved to database for user {userId}, application {formId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel approved notification to user {userId}");
            }
        }

        private async Task SendClaimedNotificationAsync(int userId, string applicantName, string title, string message, string type, string link, string formType, int formId, string? comments, string? processedBy)
        {
            try
            {
                // Get user preferences
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Map formType to ApplicationType
                var applicationType = MapFormTypeToApplicationType(formType);

                // **SAVE NOTIFICATION TO DATABASE FIRST**
                var notification = new Notification
                {
                    UserId = userId,
                    ApplicationType = applicationType,
                    ApplicationId = formId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    Status = "Claimed",
                    Status3 = "Claimed",
                    Comments = comments,
                    ProcessedBy = processedBy,
                    ProcessStage = "claimed",
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    SentViaInApp = user.PreferInAppNotification,
                    SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                    SentViaSms = user.PreferSmsNotification,
                    NotificationIdentifier = $"{applicationType.ToLower()}_{formId}_claimed_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = 4,
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Get assistance type display
                var formTypeDisplay = formType switch
                {
                    "HospitalAssistance" => "Hospital Assistance",
                    "OtherAssistance" => "Other Assistance",
                    "FuneralAssistance" => "Funeral Assistance",
                    _ => "Financial Assistance"
                };

                var feedbackUrl = "https://lingap.online/Feedback";

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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

                // NOTE: Email notification for Claimed status is handled by Adminuser.cs with detailed templates
                // Skipping generic email here to avoid duplicate emails being sent

                // Send SMS notification with feedback link if preferred (formal and concise)
                if (user.PreferSmsNotification)
                {
                    var verifyAccount = await _context.VerifiedAccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"LingapDVO: Dear {applicantName}, your {formTypeDisplay} has been received. Thank you for using our service. Please tell us what you think: {feedbackUrl}";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel claimed notification sent and saved to database for user {userId}, application {formId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel claimed notification to user {userId}");
            }
        }

        private async Task SendRetakeNotificationAsync(int userId, string applicantName, string title, string message, string type, string link, string formType, int formId, string? comments, string? processedBy)
        {
            try
            {
                // Get user preferences
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Map formType to ApplicationType
                var applicationType = MapFormTypeToApplicationType(formType);

                // Get retake iteration count
                var retakeCount = await _context.Notifications
                    .Where(n => n.UserId == userId && n.ApplicationId == formId && n.IsRetake)
                    .CountAsync();

                // **SAVE NOTIFICATION TO DATABASE FIRST**
                var notification = new Notification
                {
                    UserId = userId,
                    ApplicationType = applicationType,
                    ApplicationId = formId,
                    ApplicantName = applicantName,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    Status = "Retake",
                    Comments = comments,
                    ProcessedBy = processedBy,
                    ProcessStage = "retake",
                    CreatedAt = _dateTimeService.Now,
                    EventTimestamp = _dateTimeService.Now,
                    RetakeIteration = retakeCount + 1,
                    IsRetake = true,
                    SentViaInApp = user.PreferInAppNotification,
                    SentViaEmail = user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email),
                    SentViaSms = user.PreferSmsNotification,
                    NotificationIdentifier = $"{applicationType.ToLower()}_{formId}_retake_{retakeCount + 1}_{_dateTimeService.Now.Ticks}",
                    DisplayOrder = 2,
                    IsRead = false
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                // Get assistance type display
                var formTypeDisplay = formType switch
                {
                    "HospitalAssistance" => "Hospital Assistance",
                    "OtherAssistance" => "Other Assistance",
                    "FuneralAssistance" => "Funeral Assistance",
                    _ => "Assistance"
                };

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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

                // NOTE: Email notification is handled by Adminuser.cs with detailed templates
                // Skipping generic email here to avoid duplicate emails being sent

                // Send SMS notification with application tracking link if preferred
                if (user.PreferSmsNotification)
                {
                    var verifyAccount = await _context.VerifiedAccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"LingapDVO: Hello {applicantName}, we need you to send some papers again for your {formTypeDisplay} request. Please check your application at lingap.online and upload the needed papers.";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel retake notification sent and saved to database for user {userId}, application {formId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel retake notification to user {userId}");
            }
        }

        // Send delay notification for applications experiencing delays
        public async Task SendDelayNotificationAsync(int userId, string applicantName, string formType, string priority, DateTime submittedDate, int applicationId)
        {
            try
            {
                var user = await _context.UserAccount.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Map formType to ApplicationType
                var applicationType = MapFormTypeToApplicationType(formType);

                var formTypeDisplay = formType switch
                {
                    "HospitalAssistance" => "Hospital Assistance",
                    "OtherAssistance" => "Other Assistance",
                    "FuneralAssistance" => "Funeral Assistance",
                    _ => "Financial Assistance"
                };

                // Generate link with tab parameter based on form type
                var tabParam = formType switch
                {
                    "HospitalAssistance" => "hospital",
                    "OtherAssistance" => "medical",
                    "FuneralAssistance" => "funeral",
                    _ => ""
                };
                var trackingLink = string.IsNullOrEmpty(tabParam) ? "/Applicationtracking" : $"/Applicationtracking?tab={tabParam}";

                var hoursElapsed = (int)(_dateTimeService.Now - submittedDate).TotalHours;
                var title = priority == "Critical Delay" 
                    ? "⚠️ Critical Delay - Application Processing" 
                    : "⏰ Standard Delay - Application Processing";
                    
                var message = priority == "Critical Delay"
                    ? $"Your {formTypeDisplay} application has been pending for {hoursElapsed}+ hours. We sincerely apologize for the delay. Our team has prioritized your application and is working to process it as soon as possible."
                    : $"Your {formTypeDisplay} application has been pending for over an hour. Please don't worry - it's in our queue and will be reviewed shortly. Thank you for your patience.";

                // **SAVE NOTIFICATION TO DATABASE FIRST**
                var notification = new Notification
                {
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

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
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

                // Send email notification with HTML design if preferred
                if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email))
                {
                    var emailBody = GenerateDelayEmailBody(title, message, applicantName, formTypeDisplay, submittedDate, hoursElapsed, trackingLink);
                    await _emailService.SendEmailAsync(user.Email, title, emailBody);
                }

                // Send SMS notification if preferred
                if (user.PreferSmsNotification)
                {
                    var verifyAccount = await _context.VerifiedAccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = priority == "Critical Delay"
                            ? $"LingapDVO: URGENT - Dear {applicantName}, your {formTypeDisplay} has been delayed {hoursElapsed}+ hours. We apologize and are prioritizing your request. Contact us if urgent."
                            : $"LingapDVO: Dear {applicantName}, your {formTypeDisplay} is in queue (1+ hour). It will be reviewed shortly. Thank you for your patience.";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Delay notification ({priority}) sent and saved to database for user {userId}, application {applicationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending delay notification to user {userId}");
            }
        }

        // Helper method to map form type to ApplicationType
        private string MapFormTypeToApplicationType(string formType)
        {
            return formType switch
            {
                "HospitalAssistance" => "HospitalAssistance",
                "OtherAssistance" => "OtherAssistance",
                "FuneralAssistance" => "FuneralAssistance",
                _ => "System"
            };
        }

        // Helper method to get process stage based on status
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

        // Helper method to get display order based on status (for timeline rendering)
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
            var formTypeDisplay = formType switch
            {
                "HospitalAssistance" => "Hospital Assistance",
                "HospitalBill" => "Hospital Assistance",
                "OtherAssistance" => "Medical and Laboratory Assistance",
                "MedicalLab" => "Medical and Laboratory Assistance",
                "Other" => "Medical and Laboratory Assistance",
                "FuneralAssistance" => "Funeral Assistance",
                "Funeral" => "Funeral Assistance",
                _ => "Assistance"
            };

            // Normalize status to handle case variations
            var normalizedStatus = status?.ToLower() switch
            {
                "pending" => "Pending",
                "processing" => "Processing",
                "approve" => "Approve",
                "disapprove" => "Disapprove",
                "retake" => "Retake",
                "resubmitted" => "Resubmitted",
                "claimed" => "Claimed",
                _ => status
            };

            return normalizedStatus switch
            {
                "Pending" => $"Application Sent - {formTypeDisplay}",
                "Processing" => "Your Application is being Reviewed",
                "Approve" => "Good News! Your Application is Approved",
                "Disapprove" => "Application Not Approved",
                "Retake" => "Please Review Your Application",
                "Resubmitted" => "✅ Application Resubmitted Successfully",
                "Claimed" => "Assistance Claimed",
                _ => "Application Status Update"
            };
        }

        private string GetStatusMessage(string applicantName, string formType, string status, string? processedBy = null)
        {
            var formTypeDisplay = formType switch
            {
                "HospitalAssistance" => "Hospital Assistance",
                "HospitalBill" => "Hospital Assistance",
                "OtherAssistance" => "Medical and Laboratory Assistance",
                "MedicalLab" => "Medical and Laboratory Assistance",
                "Other" => "Medical and Laboratory Assistance",
                "FuneralAssistance" => "Funeral Assistance",
                "Funeral" => "Funeral Assistance",
                _ => "Assistance"
            };

            // Normalize status to handle case variations
            var normalizedStatus = status?.ToLower() switch
            {
                "pending" => "Pending",
                "processing" => "Processing",
                "approve" => "Approve",
                "disapprove" => "Disapprove",
                "retake" => "Retake",
                "resubmitted" => "Resubmitted",
                "claimed" => "Claimed",
                _ => status
            };

            return normalizedStatus switch
            {
                "Pending" => $"Your application has been submitted and is now in queue. You will receive an update within an hour.",
                "Processing" => !string.IsNullOrWhiteSpace(processedBy) 
                    ? $"Your {formTypeDisplay} application is being reviewed by {processedBy}."
                    : $"Your {formTypeDisplay} application is now being reviewed.",
                "Approve" => $"Your {formTypeDisplay} request is approved! Please visit our office to get your help.",
                "Disapprove" => $"We are sorry, but your {formTypeDisplay} request is not approved. You can contact us for more information.",
                "Retake" => $"We need you to send some papers again for your {formTypeDisplay} request. Please check your application and upload the needed papers.",
                "Resubmitted" => $"Thank you for resubmitting your {formTypeDisplay} application with the updated documents. Our team will review it shortly and get back to you soon.",
                "Claimed" => $"You have received your {formTypeDisplay}. Thank you for using LingapDVO.",
                _ => $"Your {formTypeDisplay} application status has been updated."
            };
        }

        private string GetNotificationType(string status)
        {
            return status switch
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
            {(string.IsNullOrEmpty(link) ? "" : $"<a href='{link}' class='button'>View Details</a>")}
        </div>
        <div class='footer'>
            <p>This is an automated message from LingapDVO. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateApprovedEmailBody(string title, string message, string link, string nearbyOfficesUrl)
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
        .button {{ display: inline-block; padding: 10px 20px; background-color: #dc143c; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; margin-right: 10px; }}
        .office-button {{ display: inline-block; padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .office-section {{ margin-top: 20px; padding: 15px; background-color: #e8f8e8; border-left: 4px solid #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{title}</h2>
        </div>
        <div class='content'>
            <p>{message}</p>
            {(string.IsNullOrEmpty(link) ? "" : $"<a href='{link}' class='button'>View Details</a>")}

            <div class='office-section'>
                <h3>Next Steps</h3>
                <p>Please visit our office to claim your approved assistance. Find the nearest office location:</p>
                <a href='{nearbyOfficesUrl}' class='office-button'>Find Nearby Offices</a>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message from LingapDVO. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateClaimedEmailBody(string title, string message, string link, string feedbackUrl)
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
        .button {{ display: inline-block; padding: 10px 20px; background-color: #dc143c; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; margin-right: 10px; }}
        .feedback-button {{ display: inline-block; padding: 10px 20px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .feedback-section {{ margin-top: 20px; padding: 15px; background-color: #e8f4f8; border-left: 4px solid #0066cc; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{title}</h2>
        </div>
        <div class='content'>
            <p>{message}</p>
            {(string.IsNullOrEmpty(link) ? "" : $"<a href='{link}' class='button'>View Details</a>")}

            <div class='feedback-section'>
                <h3>We Value Your Feedback!</h3>
                <p>We want to know what you think. Please tell us about your experience with our service.</p>
                <a href='{feedbackUrl}' class='feedback-button'>Give Feedback</a>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message from LingapDVO. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateRetakeEmailBody(string title, string message, string link)
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
        .button {{ display: inline-block; padding: 10px 20px; background-color: #ff9800; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .retake-section {{ margin-top: 20px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ff9800; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{title}</h2>
        </div>
        <div class='content'>
            <p>{message}</p>

            <div class='retake-section'>
                <h3>What to do next:</h3>
                <ol>
                    <li>Go to your Application Tracking page</li>
                    <li>Find your application</li>
                    <li>Upload the papers we need</li>
                    <li>Send your application again</li>
                </ol>
            </div>

            {(string.IsNullOrEmpty(link) ? "" : $"<a href='{link}' class='button'>Go to My Applications</a>")}
        </div>
        <div class='footer'>
            <p>This is an automatic message from LingapDVO. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateDelayEmailBody(string title, string message, string applicantName, string formType, DateTime submittedDate, int hoursElapsed, string link)
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
        .delay-section {{ margin-top: 20px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107; }}
        .info-box {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #dc143c; }}
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

            <div class='info-box'>
                <strong>Application Details:</strong><br>
                Type: <strong>{formType}</strong><br>
                Submitted: <strong>{submittedDate:MMMM dd, yyyy}</strong><br>
                Processing Time: <strong>{hoursElapsed}+ hours</strong>
            </div>

            <div class='delay-section'>
                <h3>We Apologize for the Delay</h3>
                <p>We understand the importance of your application and are actively working to expedite the processing. Your patience and understanding are greatly appreciated.</p>
                <p>If you have urgent concerns or need immediate assistance, please contact our office directly.</p>
            </div>

            {(string.IsNullOrEmpty(link) ? "" : $"<a href='{link}' class='button'>View Application Status</a>")}
        </div>
        <div class='footer'>
            <p>This is an automated message from LingapDVO. Please do not reply to this email.</p>
            <p>For inquiries, please contact our office during business hours.</p>
        </div>
    </div>
</body>
</html>";
        }

        // Format formal and concise SMS message
        private string FormatFormalSmsMessage(string applicantName, string formType, string title, string message)
        {
            // Create formal, concise automated SMS message
            return $"LingapDVO: Dear {applicantName}, your {formType} application status has been updated. {message} For inquiries, please contact our office.";
        }
    }
}
