using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class RegisterAccDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(100)]
        public string MiddleName { get; set; } = "";

        [Required, MaxLength(100)]
        public string LastName { get; set; } = "";

        [MaxLength(50)]
        public string? Suffix { get; set; } = "";

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [Required, MaxLength(250)]
        public string Password { get; set; } = "";

        // ═══════════════════════════════════════════════════════════════
        // 🔒 ANTI-MANIPULATION TOKEN
        // ═══════════════════════════════════════════════════════════════
        [Required(ErrorMessage = "Security token is required. Please refresh the page and try again.")]
        [MaxLength(500)]
        public string RegistrationToken { get; set; } = "";
    }
}
