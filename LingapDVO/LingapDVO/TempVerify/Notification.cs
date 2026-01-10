using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string ApplicationType { get; set; } = null!;

    public int? ApplicationId { get; set; }

    public string ApplicantName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Link { get; set; }

    public string? Status { get; set; }

    public string? Status2 { get; set; }

    public string? Status3 { get; set; }

    public string? Comments { get; set; }

    public string? ProcessedBy { get; set; }

    public string ProcessStage { get; set; } = null!;

    public string? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EventTimestamp { get; set; }

    public int? RetakeIteration { get; set; }

    public bool IsRetake { get; set; }

    public bool IsPermanent { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool SentViaInApp { get; set; }

    public bool SentViaEmail { get; set; }

    public bool SentViaSms { get; set; }

    public string? NotificationIdentifier { get; set; }

    public string? Metadata { get; set; }

    public bool IsArchived { get; set; }

    public int DisplayOrder { get; set; }

    public int RecipientType { get; set; }
}
