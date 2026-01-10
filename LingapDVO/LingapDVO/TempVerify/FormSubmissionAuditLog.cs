using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class FormSubmissionAuditLog
{
    public int Id { get; set; }

    public string FormType { get; set; } = null!;

    public int UserId { get; set; }

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public string? PatientName { get; set; }

    public string? RequestorName { get; set; }

    public string Action { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? Reason { get; set; }

    public string? SubmissionToken { get; set; }

    public bool HasValidToken { get; set; }

    public bool SuspiciousActivity { get; set; }

    public string? SuspiciousReasons { get; set; }

    public string? FormDataHash { get; set; }

    public DateTime AttemptedAt { get; set; }

    public int? SubmittedFormId { get; set; }

    public bool IsDuplicate { get; set; }

    public string? DuplicateDetails { get; set; }
}
