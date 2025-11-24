using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string SecurityQuestion { get; set; } = string.Empty;

        [Required]
        public string SecurityAnswer { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
