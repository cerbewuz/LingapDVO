using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    [Index("Fullname", IsUnique = true)]
    [Index("Username", IsUnique = true)]
    public class Superadminaccount
    {
       
            public int Id { get; set; }

            [MaxLength(100)]
            public string Fullname { get; set; } = "";

            [MaxLength(100)]
            public string Username { get; set; } = "";

            [MaxLength(100)]
            public string Password { get; set; } = "";

            [MaxLength(100)]
            public string Email { get; set; } = "";

    }
}
