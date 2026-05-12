using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class HospitalAssistanceDto
    {

        [Required]
        [StringLength(100, ErrorMessage = "Lastname cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-\.]+$", ErrorMessage = "Lastname contains invalid characters")]
        public string Lastname { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "Firstname cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-\.]+$", ErrorMessage = "Firstname contains invalid characters")]
        public string Firstname { get; set; } = "";


        [Required]
        [StringLength(100, ErrorMessage = "Middlename cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-\.]+$", ErrorMessage = "Middlename contains invalid characters")]
        public string Middlename { get; set; } = "";


        [Required]
        public string Suffix { get; set; } = "";

        [Required]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string BlkLotStreet { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "Subdivision/Village cannot exceed 100 characters")]
        public string SubVill { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "Barangay cannot exceed 100 characters")]
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

        // Additional Information Fields - Will be encrypted before saving
        [Required(ErrorMessage = "Hospital/Medical Facility Name is required")]
        public string HospitalFacilityName { get; set; } = "";

        [Required(ErrorMessage = "Hospital/Medical Facility Address is required")]
        public string HospitalFacilityAddress { get; set; } = "";

        [Required(ErrorMessage = "Diagnosis/Medical Condition is required")]
        public string DiagnosisMedicalCondition { get; set; } = "";

        [Required(ErrorMessage = "Hospital Bill or Cost is required")]
        public string HospitalBillCost { get; set; } = "";

        [Required(ErrorMessage = "Admission Date is required")]
        public string AdmissionDate { get; set; } = "";

        [Required(ErrorMessage = "Discharge Date is required")]
        public string DischargeDate { get; set; } = "";

        [Required(ErrorMessage = "Ward/Room Type is required")]
        public string WardRoomType { get; set; } = "";


        [Required]
        public IFormFile? IdFrontimage { get; set; }

        [Required]
        public IFormFile? IdBackimage { get; set; }


        [Required]
        public IFormFile? HospitalAssistanceDocument { get; set; }


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
