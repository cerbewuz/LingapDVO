using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    /// <summary>
    /// Form Submission Token - Prevents cloning and unauthorized duplication
    /// Each token is valid for ONE form submission only
    /// </summary>
    public class FormSubmissionToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = "";

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

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        public int? SubmittedFormId { get; set; }

        public bool IsRevoked { get; set; } = false;
    }
}
