using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class OtherAssistanceDto
    {
        [Required]
        public string Lastname { get; set; } = "";

        [Required]
        public string Firstname { get; set; } = "";


        [Required]
        public string Middlename { get; set; } = "";


        [Required]
        public string Suffix { get; set; } = "";

        [Required]
        public string BlkLotStreet { get; set; } = "";

        [Required]
        public string SubVill { get; set; } = "";

        [Required]
        public string Brgy { get; set; } = "";

        [Required]
        public string Sex { get; set; } = "";

        [Required]
        public string PhilHealth { get; set; } = "";



        public string PhilHealthNo { get; set; } = "";

        [Required]
        public string Dateofbirth { get; set; } = "";

        [Required]
        public string Age { get; set; } = "";

        //Patient's Details


        //Requestor's Details

        public string? RLastname { get; set; } = "";


        public string? RFirstname { get; set; } = "";



        public string? RMiddlename { get; set; } = "";



        public string? RSuffix { get; set; } = "";


        public string? RBlkLotStreet { get; set; } = "";

        public string? RSubVill { get; set; } = "";


        public string? RBrgy { get; set; } = "";


        public string? RelationshipPatient { get; set; } = "";

        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must be 11 digits (e.g., 09123456789)")]
        public string? ContactNo { get; set; } = "";

        //Requestor's Details


        [Required]
        public string Typeassistance { get; set; } = "";


        public string? ForCMOPERSONNEL { get; set; } = "";

        // Additional Information Fields - Conditional based on assistance type
        // Only fields for selected assistance type need to be filled

        // Medicines Fields
        public string? MedicineName { get; set; } = "";
        public string? MedicineQuantity { get; set; } = "";
        public string? MedicineCost { get; set; } = "";
        public string? PrescribingDoctor { get; set; } = "";
        public string? DoctorContactDetail { get; set; } = "";

        // Laboratory Fields
        public string? LaboratoryCenterName { get; set; } = "";
        public string? LaboratoryCenterAddress { get; set; } = "";
        public string? TestName { get; set; } = "";
        public string? TestCost { get; set; } = "";
        public string? TestOtherInfo { get; set; } = "";

        // Therapy Fields
        public string? TherapyFacilityName { get; set; } = "";
        public string? TherapyFacilityAddress { get; set; } = "";
        public string? TherapyFacilityContact { get; set; } = "";
        public string? TherapyType { get; set; } = "";

        // Medical Equipment/Apparatus Fields
        public string? EquipmentName { get; set; } = "";
        public string? EquipmentBrand { get; set; } = "";
        public string? EquipmentCategory { get; set; } = "";
        public string? EquipmentQuantity { get; set; } = "";
        public string? EquipmentCost { get; set; } = "";


        [Required]
        public IFormFile? IdFrontimage { get; set; }

        [Required]
        public IFormFile? IdBackimage { get; set; }


        [Required]
        public IFormFile? OtherAssistanceDocument { get; set; }


        public DateTime CreatedAt { get; set; }


        public DateTime ProcessAt { get; set; }


        public string Status { get; set; } = "";

        public string Processby { get; set; } = "";

        public string? Comments { get; set; } = "";

        public DateTime Result { get; set; }

        [MaxLength(100)]
        public string Status2 { get; set; } = "";

        public DateTime ClaimedAt { get; set; }

        [MaxLength(100)]
        public string Status3 { get; set; } = "";

        public DateTime? RetakeRequestedAt { get; set; }

        // 🔒 SECURITY: Form submission token for anti-duplication protection
        public string? SubmissionToken { get; set; } = "";

    }
}
