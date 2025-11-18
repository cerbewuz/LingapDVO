namespace LingapDVO.Models
{
    public class CombinedFormsViewModel
    {
        // Active applications (less than 1 month old)
        public List<HospitalAssistance> HospitalAssistance { get; set; }
        public List<OtherAssistance> OtherAssistance { get; set; }
        public List<FuneralAssistance> FuneralAssistance { get; set; }

        // Archived applications (1 month or older)
        public List<HospitalAssistance> ArchivedHospitalAssistance { get; set; }
        public List<OtherAssistance> ArchivedOtherAssistance { get; set; }
        public List<FuneralAssistance> ArchivedFuneralAssistance { get; set; }

        public List<RegisterAcc> RegisterAcc { get; set; }
        public List<Adminaccount> Adminaccount{ get; set; }

        public List<Verifyaccount> Verifyaccount { get; set; }
    }
}
