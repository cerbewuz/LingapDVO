using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class AdminloginDto
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [Required, MaxLength(16)]
        public string Password { get; set; } = "";

    }
}
