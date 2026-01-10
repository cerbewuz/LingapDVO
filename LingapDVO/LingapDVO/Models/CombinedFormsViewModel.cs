namespace LingapDVO.Models
{
    public class CombinedFormsViewModel
    {
        // Active applications (less than 1 month old)
        public List<HospitalAssistance> HospitalAssistance { get; set; } = new();
        public List<OtherAssistance> OtherAssistance { get; set; } = new();
        public List<FuneralAssistance> FuneralAssistance { get; set; } = new();

        // Archived applications (1 month or older)
        public List<HospitalAssistance> ArchivedHospitalAssistance { get; set; } = new();
        public List<OtherAssistance> ArchivedOtherAssistance { get; set; } = new();
        public List<FuneralAssistance> ArchivedFuneralAssistance { get; set; } = new();

        public List<UserAccount> UserAccount { get; set; } = new();
        public List<Adminaccount> Adminaccount{ get; set; } = new();

        public List<VerifiedAccount> VerifiedAccount { get; set; } = new();
    }
}
