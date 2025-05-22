using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string SecurityQuestion { get; set; }

        [Required]
        public string SecurityAnswer { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
