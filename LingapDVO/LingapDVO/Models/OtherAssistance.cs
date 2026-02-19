using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LingapDVO.Models
{
    [Table("OtherAssistance")]
    public class OtherAssistance
    {
        //Patient's Details
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(100)]
        public string Lastname { get; set; } = "";

        [MaxLength(100)]
        public string Firstname { get; set; } = "";


        [MaxLength(100)]
        public string Middlename { get; set; } = "";


        [MaxLength(100)]
        public string Suffix { get; set; } = "";

        [MaxLength(100)]
        public string BlkLotStreet { get; set; } = "";

        [MaxLength(100)]
        public string SubVill { get; set; } = "";

        [MaxLength(100)]
        public string Brgy { get; set; } = "";

        [MaxLength(100)]
        public string Sex { get; set; } = "";

        [MaxLength(100)]
        public string PhilHealth { get; set; } = "";


        [MaxLength(100)]
        public string? PhilHealthNo { get; set; } = "";

        [MaxLength(100)]
        public string Dateofbirth { get; set; } = "";

        [MaxLength(100)]
        public string Age { get; set; } = "";

        //Patient's Details


        //Requestor's Details
        [MaxLength(100)]
        public string? RLastname { get; set; } = "";

        [MaxLength(100)]
        public string? RFirstname { get; set; } = "";


        [MaxLength(100)]
        public string? RMiddlename { get; set; } = "";


        [MaxLength(100)]
        public string? RSuffix { get; set; } = "";

        [MaxLength(100)]
        public string? RBlkLotStreet { get; set; } = "";

        [MaxLength(100)]
        public string? RSubVill { get; set; } = "";

        [MaxLength(100)]
        public string? RBrgy { get; set; } = "";

        [MaxLength(100)]
        public string? RelationshipPatient { get; set; } = "";

        [MaxLength(100)]
        public string? ContactNo { get; set; } = "";

        //Requestor's Details


        [MaxLength(100)]
        public string Typeassistance { get; set; } = "";

        [MaxLength(100)]
        public string? ForCMOPERSONNEL { get; set; } = "";

        // Additional Information - Encrypted Fields (Medicines)
        [MaxLength(300)]
        public string? MedicineName { get; set; } = "";

        [MaxLength(200)]
        public string? MedicineQuantity { get; set; } = "";

        [MaxLength(200)]
        public string? MedicineCost { get; set; } = "";

        [MaxLength(300)]
        public string? PrescribingDoctor { get; set; } = "";

        [MaxLength(200)]
        public string? DoctorContactDetail { get; set; } = "";

        // Additional Information - Encrypted Fields (Laboratory)
        [MaxLength(500)]
        public string? LaboratoryCenterName { get; set; } = "";

        [MaxLength(500)]
        public string? LaboratoryCenterAddress { get; set; } = "";

        [MaxLength(300)]
        public string? TestName { get; set; } = "";

        [MaxLength(200)]
        public string? TestCost { get; set; } = "";

        [MaxLength(500)]
        public string? TestOtherInfo { get; set; } = "";

        // Additional Information - Encrypted Fields (Therapy)
        [MaxLength(500)]
        public string? TherapyFacilityName { get; set; } = "";

        [MaxLength(500)]
        public string? TherapyFacilityAddress { get; set; } = "";

        [MaxLength(200)]
        public string? TherapyFacilityContact { get; set; } = "";

        [MaxLength(300)]
        public string? TherapyType { get; set; } = "";

        // Additional Information - Encrypted Fields (Medical Equipment/Apparatus)
        [MaxLength(300)]
        public string? EquipmentName { get; set; } = "";

        [MaxLength(200)]
        public string? EquipmentBrand { get; set; } = "";

        [MaxLength(200)]
        public string? EquipmentCategory { get; set; } = "";

        [MaxLength(200)]
        public string? EquipmentQuantity { get; set; } = "";

        [MaxLength(200)]
        public string? EquipmentCost { get; set; } = "";


        [MaxLength(100)]
        public string Validfrontimage { get; set; } = "";

        [MaxLength(100)]
        public string ValidBackimage { get; set; } = "";


        [MaxLength(100)]
        public string DoctorPrescription { get; set; } = "";

        [MaxLength(100)]
        public string DeathCertificate { get; set; } = "";

        [MaxLength(100)]
        public string MedCertificate { get; set; } = "";


        public DateTime CreatedAt { get; set; }


        public DateTime ProcessAt { get; set; }


        [MaxLength(100)]
        public string Status { get; set; } = "";

        [MaxLength(100)]
        public string Processby { get; set; } = "";

        public string? Comments { get; set; } = "";

        public DateTime Result { get; set; }

        [MaxLength(100)]
        public string Status2 { get; set; } = "";

        public DateTime ClaimedAt { get; set; }

        [MaxLength(100)]
        public string Status3 { get; set; } = "";

        // Retake Application fields
        public string? RetakeReason { get; set; } = "";
        public DateTime? RetakeRequestedAt { get; set; }
        public bool IsRetakeApplication { get; set; } = false;

        // Archiving: Applications older than 1 month are automatically archived
        public bool IsArchived { get; set; } = false;

        // Navigation property for eager loading
        [JsonIgnore]
        public virtual UserAccount? User { get; set; }
    }
}
