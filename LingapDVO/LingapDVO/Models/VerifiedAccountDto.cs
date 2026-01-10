using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class VerifiedAccountDto
    {

        public int Id { get; set; }


        // IDtype is auto-populated by the ID Analyzer API after verification (readonly field)
        // [Required] removed - server-side validation is done manually in the controller
        [MaxLength(100)]
        public string IDtype { get; set; } = "";

        // IDnumber is auto-populated by the ID Analyzer API after verification
        // Server-side validation is done manually in the controller
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z0-9\-]*$", ErrorMessage = "ID number can only contain letters, numbers, and dashes")]
        public string IDnumber { get; set; } = "";

        [Required]
        public IFormFile? ValidFrontID { get; set; }

        // Back ID is only required for National ID (phil-id), not for Driver's License or UMID
        public IFormFile? ValidBackID { get; set; }



        [Required, MaxLength(100)]
        public string Firstname { get; set; } = "";

        [MaxLength(100)]
        public string Middlename { get; set; } = "";

        [Required, MaxLength(100)]
        public string Lastname { get; set; } = "";

        [MaxLength(100)]
        public string? Suffix { get; set; }

        [Required, MaxLength(100)]
        public string Gender { get; set; } = "";

        [Required, MaxLength(100)]
        public string Dateofbirth { get; set; } = "";



        [Required, MaxLength(100)]
        public string BlkLotStreet { get; set; } = "";

        [MaxLength(100)]
        public string SubVill { get; set; } = "";

        [Required, MaxLength(100)]
        public string Barangay { get; set; } = "";


        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be 11 digits (e.g., 09123456789)")]
        public string Phonenumber { get; set; } = "";

        [Required, MaxLength(100)]
        public string CivilStatus { get; set; } = "";
    }
}
