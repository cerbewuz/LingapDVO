using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class SuperadminaccountDto
    {

        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Fullname { get; set; } = "";

        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [Required, MaxLength(100)]
        public string Password { get; set; } = "";

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";
    }
}
