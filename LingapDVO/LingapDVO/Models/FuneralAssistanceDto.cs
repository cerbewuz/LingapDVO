using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class FuneralAssistanceDto
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

        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must be 11 digits (e.g., 09123456789)")]
        public string? ContactNo { get; set; } = "";

        //Requestor's Details


        // No Type of Assistance required for Funeral Assistance
        public string Typeassistance { get; set; } = "";


        public string? ForCMOPERSONNEL { get; set; } = "";

        // Additional Information Fields - Will be encrypted before saving
        [Required(ErrorMessage = "Name of Deceased Person is required")]
        public string DeceasedPersonName { get; set; } = "";

        [Required(ErrorMessage = "Relationship to Deceased is required")]
        public string RelationshipToDeceased { get; set; } = "";

        [Required(ErrorMessage = "Date of Death is required")]
        public string DateOfDeath { get; set; } = "";

        [Required(ErrorMessage = "Time of Death is required")]
        public string TimeOfDeath { get; set; } = "";

        [Required(ErrorMessage = "Cause of Death is required")]
        public string CauseOfDeath { get; set; } = "";

        [Required(ErrorMessage = "Funeral Home/Parlor Name is required")]
        public string FuneralHomeName { get; set; } = "";

        [Required(ErrorMessage = "Funeral Home/Parlor Address is required")]
        public string FuneralHomeAddress { get; set; } = "";

        [Required(ErrorMessage = "Date of Burial/Cremation is required")]
        public string BurialCremationDate { get; set; } = "";

        [Required(ErrorMessage = "Time of Burial/Cremation is required")]
        public string BurialCremationTime { get; set; } = "";

        [Required(ErrorMessage = "Type (Burial/Cremation) is required")]
        public string BurialCremationType { get; set; } = "";


        [Required]
        public IFormFile? IdFrontimage { get; set; }

        [Required]
        public IFormFile? IdBackimage { get; set; }


        [Required]
        public IFormFile? FuneralAssistanceDocument { get; set; }


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
