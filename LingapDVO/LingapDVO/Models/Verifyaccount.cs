using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class Verifyaccount
    {

        public int Id { get; set; }

        [MaxLength(100)]
        public string IDtype { get; set; } = "";

        [MaxLength(100)]
        public string IDnumber { get; set; } = "";

        [MaxLength(100)]
        public string FrontID { get; set; } = "";

        [MaxLength(100)]
        public string BackID { get; set; } = "";



        [MaxLength(100)]
        public string Firstname { get; set; } = "";

        [MaxLength(100)]
        public string Middlename { get; set; } = "";

        [MaxLength(100)]
        public string Lastname { get; set; } = "";

        [MaxLength(100)]
        public string Suffix { get; set; } = "";

        [MaxLength(100)]
        public string Gender { get; set; } = "";

        [MaxLength(100)]
        public string Dateofbirth { get; set; } = "";



        [MaxLength(100)]
        public string BlkLotStreet { get; set; } = "";

        [MaxLength(100)]
        public string SubVill { get; set; } = "";

        [MaxLength(100)]
        public string Barangay { get; set; } = "";

        [MaxLength(100)]
        public string District { get; set; } = "";



        [MaxLength(100)]
        public string SecurityQuestions { get; set; } = "";
        [MaxLength(100)]
        public string Securityanswer { get; set; } = "";



    }
}
