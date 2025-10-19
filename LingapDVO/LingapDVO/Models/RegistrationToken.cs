using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    /// <summary>
    /// Registration Token - Prevents backend manipulation and force entry
    /// Each token is valid for ONE registration attempt only
    /// </summary>
    public class RegistrationToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string IpAddress { get; set; } = "";

        [Required]
        [MaxLength(500)]
        public string UserAgent { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        [MaxLength(100)]
        public string? UsedByEmail { get; set; }

        public bool IsRevoked { get; set; } = false;
    }
}
