using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class Feedback
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Name { get; set; }

    public string? Office { get; set; }

    public string? ServiceAvailed { get; set; }

    public string? Contact { get; set; }

    public string? Sex { get; set; }

    public string? TypeOfClient { get; set; }

    public string? AssistanceType { get; set; }

    public int? AssistanceId { get; set; }

    public string? Q1Ccknowledge { get; set; }

    public string? Q2Ccvisibility { get; set; }

    public string? Q3Cchelpfulness { get; set; }

    public int? R1ServiceSatisfaction { get; set; }

    public int? R2TimeSpent { get; set; }

    public int? R3ProcessFollowed { get; set; }

    public int? R4ProcessSimplicity { get; set; }

    public int? R5InformationAccess { get; set; }

    public int? R6FairPayment { get; set; }

    public int? R7Fairness { get; set; }

    public int? R8EmployeeCourtesy { get; set; }

    public string? Commendation { get; set; }

    public string? Suggestion { get; set; }

    public string? Request { get; set; }

    public string? Complaint { get; set; }

    public string? Signature { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string? IpAddress { get; set; }
}
