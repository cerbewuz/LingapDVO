namespace LingapDVO.Models
{
    public class CombinedFormsViewModel
    {
        public List<FillupformHospitalBill> HospitalBills { get; set; }
        public List<Medicalandlabform> MedicalLabForms { get; set; }
        public List<Funeralburialform> Funeralburialform { get; set; }

        public List<Register> Register { get; set; }
        public List<Adminaccount> Adminaccount{ get; set; }
    }
}
