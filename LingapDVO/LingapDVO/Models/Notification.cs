using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LingapDVO.Models
{
    /// <summary>
    /// Unified notification system that stores ALL notifications and status changes
    /// Synchronized across Homepage and Application Tracking pages
    /// Replaces NotificationReadStatus and provides complete audit trail
    /// </summary>
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// User ID who receives this notification (for user notifications)
        /// Null for admin notifications (sent to all admins)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Recipient type: 1 = User notification, 2 = Admin notification
        /// Allows single table to handle both user and admin notifications
        /// </summary>
        [Required]
        public int RecipientType { get; set; } = 1;

        /// <summary>
        /// Type of application (HospitalAssistance, OtherAssistance, FuneralAssistance, System)
        /// System type is for non-application notifications like welcome messages
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ApplicationType { get; set; } = string.Empty;

        /// <summary>
        /// Application ID from respective table (null for system notifications)
        /// </summary>
        public int? ApplicationId { get; set; }

        /// <summary>
        /// Applicant/User full name for display
        /// </summary>
        [MaxLength(300)]
        public string ApplicantName { get; set; } = string.Empty;

        /// <summary>
        /// Notification title
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification message body
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Notification type for UI rendering
        /// (submitted, processing, approved, disapproved, retake, claimed, delay_alert, welcome, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Link/URL associated with notification (for navigation)
        /// </summary>
        [MaxLength(500)]
        public string? Link { get; set; }

        /// <summary>
        /// Primary status (Pending, Processing, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? Status { get; set; }

        /// <summary>
        /// Secondary status (Approve, Disapprove, Retake, etc.) - Status2 field
        /// </summary>
        [MaxLength(50)]
        public string? Status2 { get; set; }

        /// <summary>
        /// Tertiary status (Claimed, etc.) - Status3 field
        /// </summary>
        [MaxLength(50)]
        public string? Status3 { get; set; }

        /// <summary>
        /// Admin comments or reason for status change
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// Admin user who processed the application
        /// </summary>
        [MaxLength(200)]
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// Process stage (submitted, processing, result, claimed, delayed)
        /// Used for timeline rendering in Application Tracking
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ProcessStage { get; set; } = string.Empty;

        /// <summary>
        /// For delay notifications - priority level (medium, high, critical)
        /// </summary>
        [MaxLength(20)]
        public string? Priority { get; set; }

        /// <summary>
        /// Timestamp when notification was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of the actual event (ProcessAt, Result, ClaimedAt from application)
        /// </summary>
        public DateTime? EventTimestamp { get; set; }

        /// <summary>
        /// For Retake workflows - iteration number (1, 2, 3, etc.)
        /// Tracks how many times application has been through retake
        /// </summary>
        public int? RetakeIteration { get; set; }

        /// <summary>
        /// Indicates if this notification is part of a retake workflow
        /// </summary>
        public bool IsRetake { get; set; } = false;

        /// <summary>
        /// Indicates if notification is permanent (like welcome messages)
        /// Permanent notifications are always shown
        /// </summary>
        public bool IsPermanent { get; set; } = false;

        /// <summary>
        /// Indicates if user has read this notification
        /// Replaces NotificationReadStatus table functionality
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Timestamp when user marked notification as read (null if unread)
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Indicates if notification was sent via in-app
        /// </summary>
        public bool SentViaInApp { get; set; } = false;

        /// <summary>
        /// Indicates if notification was sent via email
        /// </summary>
        public bool SentViaEmail { get; set; } = false;

        /// <summary>
        /// Indicates if notification was sent via SMS
        /// </summary>
        public bool SentViaSms { get; set; } = false;

        /// <summary>
        /// Unique identifier for notification (used for client-side tracking)
        /// Format: {type}_{applicationId}_{stage}_{timestamp} or custom
        /// Example: "hospital_123_processing_20251125", "welcome_verified_456"
        /// </summary>
        [MaxLength(255)]
        public string? NotificationIdentifier { get; set; }

        /// <summary>
        /// Additional metadata in JSON format for extensibility
        /// Can store custom data like delay hours, previous status, etc.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Indicates if notification is archived (for cleanup purposes)
        /// Archived notifications can be hidden from active view but preserved in database
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Order/sequence number for displaying notifications in correct order
        /// Lower numbers appear first in timeline
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
    }
}
