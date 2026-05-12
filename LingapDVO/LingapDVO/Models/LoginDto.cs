using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class LoginDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, hyphens, and periods")]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(250, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";

    }
}
