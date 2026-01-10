using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class RegistrationToken
{
    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public string? UsedByEmail { get; set; }

    public bool IsRevoked { get; set; }
}
