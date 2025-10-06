using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class VerifyaccountDto
    {

        public int Id { get; set; }


        [Required, MaxLength(100)]
        public string IDtype { get; set; } = "";

        [Required, MaxLength(100)]
        public string IDnumber { get; set; } = "";

        [Required]
        public IFormFile? ValidFrontID { get; set; }

        [Required]
        public IFormFile? ValidBackID { get; set; }



        [Required, MaxLength(100)]
        public string Firstname { get; set; } = "";

        [Required, MaxLength(100)]
        public string Middlename { get; set; } = "";

        [Required, MaxLength(100)]
        public string Lastname { get; set; } = "";

        [Required, MaxLength(100)]
        public string Suffix { get; set; } = "";

        [Required, MaxLength(100)]
        public string Gender { get; set; } = "";

        [Required, MaxLength(100)]
        public string Dateofbirth { get; set; } = "";



        [Required, MaxLength(100)]
        public string BlkLotStreet { get; set; } = "";

        [Required, MaxLength(100)]
        public string SubVill { get; set; } = "";

        [Required, MaxLength(100)]
        public string Barangay { get; set; } = "";

        [Required, MaxLength(100)]
        public string District { get; set; } = "";



        [Required, MaxLength(100)]
        public string SecurityQuestions { get; set; } = "";

        [Required, MaxLength(100)]
        public string Securityanswer { get; set; } = "";

        [Required, MaxLength(100)]
        public string Phonenumber { get; set; } = "";
    }
}
