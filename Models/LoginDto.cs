using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class LoginDto
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [Required, MaxLength(250)]
        public string Password { get; set; } = "";

    }
}
