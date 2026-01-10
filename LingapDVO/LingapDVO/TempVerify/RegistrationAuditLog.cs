using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class RegistrationAuditLog
{
    public int Id { get; set; }

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public string? Email { get; set; }

    public string? Username { get; set; }

    public string? FullName { get; set; }

    public string Action { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? Reason { get; set; }

    public string? RegistrationToken { get; set; }

    public bool HasValidToken { get; set; }

    public bool SuspiciousActivity { get; set; }

    public string? SuspiciousReasons { get; set; }

    public DateTime AttemptedAt { get; set; }

    public int? RegisteredUserId { get; set; }
}
