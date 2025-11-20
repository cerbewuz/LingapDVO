using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LingapDVO.Models
{
    /// <summary>
    /// Stores which notifications have been marked as read by users
    /// Ensures read status persists across sessions and browser refreshes
    /// </summary>
    [Table("NotificationReadStatus")]
    public class NotificationReadStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// User ID who marked the notification as read
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Notification identifier (e.g., "hospital_123_pending", "welcome_verified_456")
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string NotificationId { get; set; }

        /// <summary>
        /// When the notification was marked as read
        /// </summary>
        [Required]
        public DateTime MarkedReadAt { get; set; }

        /// <summary>
        /// Composite index for fast lookups by UserId and NotificationId
        /// </summary>
        [NotMapped]
        public const string IndexName = "IX_NotificationReadStatus_UserId_NotificationId";
    }
}
