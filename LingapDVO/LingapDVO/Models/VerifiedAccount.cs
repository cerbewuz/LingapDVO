using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LingapDVO.Models
{


    [Table("VerifiedAccount")]
    public class VerifiedAccount
    {

        public int Id { get; set; }


        [ForeignKey("User")]
        public int UserId { get; set; } 

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
        public string Phonenumber { get; set; } = "";

        [MaxLength(100)]
        public string CivilStatus { get; set; } = "";

        [MaxLength(100)]
        public string userfacepicture { get; set; } = "";

        // ID Analyzer verification decision: "accept", "review", "reject"
        [MaxLength(20)]
        public string? decision { get; set; }

        // ID Analyzer transaction ID for polling status updates
        [MaxLength(200)]
        public string? TransactionId { get; set; }

    }
}
