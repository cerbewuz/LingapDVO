using System;
using System.Collections.Generic;

namespace LingapDVO.TempModels;

public partial class HospitalAssistance
{
    public int Id { get; set; }

    public string Validfrontimage { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Age { get; set; } = null!;

    public string BlkLotStreet { get; set; } = null!;

    public string Brgy { get; set; } = null!;

    public string? ContactNo { get; set; }

    public string Dateofbirth { get; set; } = null!;

    public string District { get; set; } = null!;

    public string DoctorPrescription { get; set; } = null!;

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string Middlename { get; set; } = null!;

    public string PhilHealth { get; set; } = null!;

    public string? PhilHealthNo { get; set; }

    public string? RblkLotStreet { get; set; }

    public string? Rbrgy { get; set; }

    public string? Rdistrict { get; set; }

    public string? Rfirstname { get; set; }

    public string? Rlastname { get; set; }

    public string? Rmiddlename { get; set; }

    public string? RsubVill { get; set; }

    public string? Rsuffix { get; set; }

    public string? RelationshipPatient { get; set; }

    public string Sex { get; set; } = null!;

    public string SubVill { get; set; } = null!;

    public string Suffix { get; set; } = null!;

    public string Typeassistance { get; set; } = null!;

    public string ValidBackimage { get; set; } = null!;

    public string? ForCmopersonnel { get; set; }

    public int UserId { get; set; }

    public string DeathCertificate { get; set; } = null!;

    public DateTime ProcessAt { get; set; }

    public string Status { get; set; } = null!;

    public string Processby { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime Result { get; set; }

    public string Status2 { get; set; } = null!;

    public DateTime ClaimedAt { get; set; }

    public string Status3 { get; set; } = null!;

    public bool IsArchived { get; set; }

    public bool IsRetakeApplication { get; set; }

    public string? RetakeReason { get; set; }

    public DateTime? RetakeRequestedAt { get; set; }

    public string? AdmissionDate { get; set; }

    public string? DiagnosisMedicalCondition { get; set; }

    public string? DischargeDate { get; set; }

    public string? HospitalBillCost { get; set; }

    public string? HospitalFacilityAddress { get; set; }

    public string? HospitalFacilityName { get; set; }

    public string? WardRoomType { get; set; }
}
