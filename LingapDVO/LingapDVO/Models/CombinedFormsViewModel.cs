namespace LingapDVO.Models
{
    public class CombinedFormsViewModel
    {
        public List<FillupformHospitalBill> HospitalBills { get; set; }
        public List<Medicalandlabform> MedicalLabForms { get; set; }
        public List<Funeralburialform> Funeralburialform { get; set; }

        public List<RegisterAcc> RegisterAcc { get; set; }
        public List<Adminaccount> Adminaccount{ get; set; }

        public List<Verifyaccount> Verifyaccount { get; set; }
    }
}
