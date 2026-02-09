using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    /// <summary>
    /// Audit Log for Registration Attempts - Tracks all registration activity
    /// Used to detect suspicious patterns and backend manipulation
    /// </summary>
    public class RegistrationAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = "";

        [MaxLength(255)]
        public string UserAgent { get; set; } = "";

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Username { get; set; }

        [MaxLength(80)]
        public string? FullName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = ""; // "ATTEMPT", "SUCCESS", "BLOCKED", "FAILED"

        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = ""; // "WEB_FORM", "API", "DIRECT_DB", "UNKNOWN"

        [MaxLength(150)]
        public string? Reason { get; set; }

        [MaxLength(100)]
        public string? RegistrationToken { get; set; }

        public bool HasValidToken { get; set; } = false;

        public bool SuspiciousActivity { get; set; } = false;

        [MaxLength(200)]
        public string? SuspiciousReasons { get; set; }

        [Required]
        public DateTime AttemptedAt { get; set; }

        public int? RegisteredUserId { get; set; }
    }
}
