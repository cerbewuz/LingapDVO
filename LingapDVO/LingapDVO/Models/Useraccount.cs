using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LingapDVO.Models
{
    [Index("Email", IsUnique = true)]
    [Index("Username", IsUnique = true)]
    [Table("UserAccount")]
    public class UserAccount
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Email { get; set; } = "";

        [MaxLength(250)]
        public string Password { get; set; } = "";

        [MaxLength(100)]
        public string Username { get; set; } = "";

        public string? Status { get; set; } = "";

        [MaxLength(500)]
        public string? Profilepicture { get; set; } = "";

        // Notification Preferences
        public bool PreferEmailNotification { get; set; } = true;
        public bool PreferSmsNotification { get; set; } = true;
        public bool PreferInAppNotification { get; set; } = true;
    }
}
