using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    [Index("Email", IsUnique = true)]
    [Index("Phonenumber", IsUnique = true)]

    public class RegisterAcc
    {

        public int Id { get; set; }

        [MaxLength(100)]
        public string Email { get; set; } = "";

        [MaxLength(100)]
        public string Phonenumber { get; set; } = "";

        [MaxLength(250)]
        public string Password { get; set; } = "";

        [MaxLength(100)]
        public string Username { get; set; } = "";

        public string? Status { get; set; } = "";

    }
}
