using System.ComponentModel.DataAnnotations;

namespace LingapDVO.Models
{
    public class FillupformHospitalBillDto
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
        public string District { get; set; } = "";

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

     
        public string? RDistrict { get; set; } = "";

  
        public string? RelationshipPatient { get; set; } = "";

        public string? ContactNo { get; set; } = "";

        //Requestor's Details


        [Required]
        public string Typeassistance { get; set; } = "";

 
        public string? ForCMOPERSONNEL { get; set; } = "";


        [Required]
        public IFormFile? IdFrontimage { get; set; } 

        [Required]
        public IFormFile? IdBackimage { get; set; } 


        [Required]
        public IFormFile? DoctorPrescriptionimage { get; set; }


        public IFormFile? DeathCertificateimage { get; set; }


        public DateTime CreatedAt { get; set; }


        public DateTime ProcessAt { get; set; }

 
        public string Status { get; set; } = "";

        public string Processby { get; set; } = "";

        public string? Comments { get; set; } = "";



    }
}
