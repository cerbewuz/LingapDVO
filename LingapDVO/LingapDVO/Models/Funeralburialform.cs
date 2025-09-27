using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class Funeralburialform
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
        public string District { get; set; } = "";

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
        public string? RDistrict { get; set; } = "";

        [MaxLength(100)]
        public string? RelationshipPatient { get; set; } = "";

        [MaxLength(100)]
        public string? ContactNo { get; set; } = "";

        //Requestor's Details


        [MaxLength(100)]
        public string Typeassistance { get; set; } = "";

        [MaxLength(100)]
        public string? ForCMOPERSONNEL { get; set; } = "";


        [MaxLength(100)]
        public string Validfrontimage { get; set; } = "";

        [MaxLength(100)]
        public string ValidBackimage { get; set; } = "";


        [MaxLength(100)]
        public string DoctorPrescription { get; set; } = "";

        [MaxLength(100)]
        public string DeathCertificate { get; set; } = "";


        public DateTime CreatedAt { get; set; }


        public DateTime ProcessAt { get; set; }


        [MaxLength(100)]
        public string Status { get; set; } = "";

        [MaxLength(100)]
        public string Processby { get; set; } = "";

        public string? Comments { get; set; } = "";
    }
}
