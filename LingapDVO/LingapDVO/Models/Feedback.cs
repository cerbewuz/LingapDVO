using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    /// <summary>
    /// Citizen Feedback Form Model - Stores feedback from users who claimed assistance
    /// </summary>
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        // User Information
        public int? UserId { get; set; }

        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? Office { get; set; }

        [MaxLength(50)]
        public string? ServiceAvailed { get; set; }

        [MaxLength(12)]
        public string? Contact { get; set; }

        [MaxLength(10)]
        public string? Sex { get; set; }

        [MaxLength(100)]
        public string? TypeOfClient { get; set; }

        // Application Information (for tracking)
        [MaxLength(50)]
        public string? AssistanceType { get; set; } // HospitalBill, Medical, Funeral

        public int? AssistanceId { get; set; } // ID of the claimed application

        // CC Questions
        [MaxLength(200)]
        public string? Q1_CCKnowledge { get; set; }

        [MaxLength(200)]
        public string? Q2_CCVisibility { get; set; }

        [MaxLength(200)]
        public string? Q3_CCHelpfulness { get; set; }

        // Rating Responses (1-8)
        public int? R1_ServiceSatisfaction { get; set; }
        public int? R2_TimeSpent { get; set; }
        public int? R3_ProcessFollowed { get; set; }
        public int? R4_ProcessSimplicity { get; set; }
        public int? R5_InformationAccess { get; set; }
        public int? R6_FairPayment { get; set; }
        public int? R7_Fairness { get; set; }
        public int? R8_EmployeeCourtesy { get; set; }

        // Remarks
        [MaxLength(200)]
        public string? Commendation { get; set; }

        [MaxLength(200)]
        public string? Suggestion { get; set; }

        [MaxLength(200)]
        public string? Request { get; set; }

        [MaxLength(200)]
        public string? Complaint { get; set; }

        [MaxLength(80)]
        public string? Signature { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }
    }
}
