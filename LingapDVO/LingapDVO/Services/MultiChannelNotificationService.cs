using LingapDVO.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    public interface IMultiChannelNotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string type, string link = null);
        Task SendStatusChangeNotificationAsync(int userId, string applicantName, string formType, string status, int formId);
    }

    public class MultiChannelNotificationService : IMultiChannelNotificationService
    {
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MultiChannelNotificationService> _logger;

        public MultiChannelNotificationService(
            ISmsService smsService,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context,
            ILogger<MultiChannelNotificationService> logger)
        {
            _smsService = smsService;
            _emailService = emailService;
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type, string link = null)
        {
            try
            {
                // Get user preferences
                var user = await _context.RegisterAcc.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        title = title,
                        message = message,
                        type = type,
                        link = link,
                        createdAt = DateTime.UtcNow
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
                    // Get phone number from Verifyaccount table
                    var verifyAccount = await _context.Verifyaccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"{title}: {message}";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel notification sent successfully to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel notification to user {userId}");
            }
        }

        public async Task SendStatusChangeNotificationAsync(int userId, string applicantName, string formType, string status, int formId)
        {
            var title = GetStatusTitle(status);
            var message = GetStatusMessage(applicantName, formType, status);
            var link = "/Uploads"; // Link to user's uploads page
            var type = GetNotificationType(status);

            // For email notifications, we need special handling for "Claimed" status to include feedback link
            if (status == "Claimed")
            {
                await SendClaimedNotificationAsync(userId, title, message, type, link, formType, formId);
            }
            else
            {
                await SendNotificationAsync(userId, title, message, type, link);
            }
        }

        private async Task SendClaimedNotificationAsync(int userId, string title, string message, string type, string link, string formType, int formId)
        {
            try
            {
                // Get user preferences
                var user = await _context.RegisterAcc.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return;
                }

                // Send in-app notification via SignalR if preferred
                if (user.PreferInAppNotification)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        title = title,
                        message = message,
                        type = type,
                        link = link,
                        createdAt = DateTime.UtcNow
                    });
                }

                // Send email notification with feedback link if preferred
                if (user.PreferEmailNotification && !string.IsNullOrEmpty(user.Email))
                {
                    var emailBody = GenerateClaimedEmailBody(title, message, link, userId, formType, formId);
                    await _emailService.SendEmailAsync(user.Email, title, emailBody);
                }

                // Send SMS notification if preferred
                if (user.PreferSmsNotification)
                {
                    // Get phone number from Verifyaccount table
                    var verifyAccount = await _context.Verifyaccount
                        .FirstOrDefaultAsync(v => v.UserId == userId);

                    if (verifyAccount != null && !string.IsNullOrEmpty(verifyAccount.Phonenumber))
                    {
                        var smsMessage = $"{title}: {message}";
                        await _smsService.SendSmsAsync(verifyAccount.Phonenumber, smsMessage);
                    }
                }

                _logger.LogInformation($"Multi-channel claimed notification sent successfully to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending multi-channel claimed notification to user {userId}");
            }
        }

        private string GetStatusTitle(string status)
        {
            return status switch
            {
                "Pending" => "Application Submitted",
                "Processing" => "Application Being Processed",
                "Approve" => "Application Approved",
                "Disapprove" => "Application Disapproved",
                "Claimed" => "Assistance Claimed",
                _ => "Application Status Update"
            };
        }

        private string GetStatusMessage(string applicantName, string formType, string status)
        {
            var formTypeDisplay = formType switch
            {
                "HospitalBill" => "Hospital Bill Assistance",
                "Medical" => "Medical and Lab Assistance",
                "Funeral" => "Funeral and Burial Assistance",
                _ => "Financial Assistance"
            };

            return status switch
            {
                "Pending" => $"Your {formTypeDisplay} application has been submitted and is pending review.",
                "Processing" => $"Your {formTypeDisplay} application is now being processed by our team.",
                "Approve" => $"Good news! Your {formTypeDisplay} application has been approved. Please visit the office for claiming.",
                "Disapprove" => $"We regret to inform you that your {formTypeDisplay} application has been disapproved. Please contact us for more details.",
                "Claimed" => $"Your {formTypeDisplay} has been successfully claimed. Thank you for using our service.",
                _ => $"Your {formTypeDisplay} application status has been updated to {status}."
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
                "Claimed" => "application_claimed",
                _ => "status_change"
            };
        }

        private string GenerateEmailBody(string title, string message, string link)
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

        private string GenerateClaimedEmailBody(string title, string message, string link, int userId, string formType, int formId)
        {
            var feedbackUrl = $"https://yourdomain.com/Dashboard/Feedback?userId={userId}&assistanceType={formType}&assistanceId={formId}";

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
                <p>Your opinion matters to us. Please take a moment to share your experience with our service.</p>
                <a href='{feedbackUrl}' class='feedback-button'>Submit Feedback</a>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message from LingapDVO. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
