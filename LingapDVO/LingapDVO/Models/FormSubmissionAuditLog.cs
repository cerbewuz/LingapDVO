using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    /// <summary>
    /// Audit Log for Form Submissions - Tracks all submission attempts
    /// Detects cloning, duplication, and suspicious activity
    /// </summary>
    public class FormSubmissionAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FormType { get; set; } = ""; // "HospitalBill", "MedicalLab", "Funeral"

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string IpAddress { get; set; } = "";

        [MaxLength(500)]
        public string UserAgent { get; set; } = "";

        [MaxLength(200)]
        public string? PatientName { get; set; }

        [MaxLength(200)]
        public string? RequestorName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = ""; // "ATTEMPT", "SUCCESS", "BLOCKED", "FAILED", "DUPLICATE"

        [Required]
        [MaxLength(100)]
        public string Source { get; set; } = ""; // "WEB_FORM", "API", "CLONED", "UNKNOWN"

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(500)]
        public string? SubmissionToken { get; set; }

        public bool HasValidToken { get; set; } = false;

        public bool SuspiciousActivity { get; set; } = false;

        [MaxLength(1000)]
        public string? SuspiciousReasons { get; set; }

        [MaxLength(500)]
        public string? FormDataHash { get; set; } // SHA-256 hash of form data for duplicate detection

        [Required]
        public DateTime AttemptedAt { get; set; }

        public int? SubmittedFormId { get; set; }

        public bool IsDuplicate { get; set; } = false;

        [MaxLength(1000)]
        public string? DuplicateDetails { get; set; }
    }
}
