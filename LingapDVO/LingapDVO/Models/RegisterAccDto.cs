using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class RegisterAccDto
    {

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(100)]
        public string Phonenumber { get; set; } = "";

        [Required, MaxLength(250)]
        public string Password { get; set; } = "";
    }
}
