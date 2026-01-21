namespace LingapDVO.Models
{
    /// <summary>
    /// Request DTO for updating username
    /// </summary>
    public class UpdateUsernameRequest
    {
        public string Username { get; set; } = "";
    }

    /// <summary>
    /// Request DTO for changing password
    /// </summary>
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
