using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class RegisterDto
    {

        public int Id { get; set; }

        [Required,MaxLength(100)]
        public string Fullname { get; set; } = "";

        [Required, MaxLength(100)]
        public string Username { get; set; } = "";

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(100)]
        public string Phonenumber { get; set; } = "";
        [Required, MaxLength(250)]
        public string Password { get; set; } = "";

        [Required, MaxLength(100)]
        public string Dateofbirth { get; set; } = "";

        [Required, MaxLength(100)]
        public string Gender { get; set; } = "";

        [Required, MaxLength(100)]
        public string Address { get; set; } = "";

        [Required]
        public IFormFile? ImageFile{ get; set; } 

        [Required, MaxLength(100)]
        public string SecurityQuestions { get; set; } = "";

        [Required, MaxLength(100)]
        public string Securityanswer { get; set; } = "";

    }
}
