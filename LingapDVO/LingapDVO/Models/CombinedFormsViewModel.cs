namespace LingapDVO.Models
{
    public class CombinedFormsViewModel
    {
        public List<HospitalAssistance> HospitalBills { get; set; }
        public List<OtherAssistance> MedicalLabForms { get; set; }
        public List<FuneralAssistance> FuneralAssistance { get; set; }

        public List<RegisterAcc> RegisterAcc { get; set; }
        public List<Adminaccount> Adminaccount{ get; set; }

        public List<Verifyaccount> Verifyaccount { get; set; }
    }
}
