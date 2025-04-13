using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{

    [Index("Fullname", IsUnique = true)]
    [Index("Username", IsUnique = true)]
    [Index("Email", IsUnique = true)]
    [Index("Phonenumber", IsUnique = true)]
    public class Register
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Fullname { get; set; } = "";

        [MaxLength(100)]
        public string Username{ get; set; } = "";

        [MaxLength(100)]
        public string Email { get; set; } = "";

        [MaxLength(100)] 
        public string Phonenumber { get; set; } = "";

        [MaxLength(16)]
        public string Password { get; set; } = "";

        [MaxLength(100)]
        public string Dateofbirth { get; set; } = "";

        [MaxLength(100)]
        public string Gender { get; set; } = "";

        [MaxLength(100)]
        public string Address { get; set; } = "";

        [MaxLength(100)]    
        public string ImageFilename { get; set; } = "";

        [MaxLength(100)]
        public string SecurityQuestions { get; set; } = "";
        [MaxLength(100)]
        public string Securityanswer { get; set; } = "";



    }
}
