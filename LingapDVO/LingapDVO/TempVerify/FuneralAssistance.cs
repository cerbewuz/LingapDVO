using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class FuneralAssistance
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Lastname { get; set; } = null!;

    public string Firstname { get; set; } = null!;

    public string Middlename { get; set; } = null!;

    public string Suffix { get; set; } = null!;

    public string BlkLotStreet { get; set; } = null!;

    public string SubVill { get; set; } = null!;

    public string Brgy { get; set; } = null!;

    public string District { get; set; } = null!;

    public string Sex { get; set; } = null!;

    public string PhilHealth { get; set; } = null!;

    public string? PhilHealthNo { get; set; }

    public string Dateofbirth { get; set; } = null!;

    public string Age { get; set; } = null!;

    public string? Rlastname { get; set; }

    public string? Rfirstname { get; set; }

    public string? Rmiddlename { get; set; }

    public string? Rsuffix { get; set; }

    public string? RblkLotStreet { get; set; }

    public string? RsubVill { get; set; }

    public string? Rbrgy { get; set; }

    public string? Rdistrict { get; set; }

    public string? RelationshipPatient { get; set; }

    public string? ContactNo { get; set; }

    public string Typeassistance { get; set; } = null!;

    public string? ForCmopersonnel { get; set; }

    public string Validfrontimage { get; set; } = null!;

    public string ValidBackimage { get; set; } = null!;

    public string DoctorPrescription { get; set; } = null!;

    public string DeathCertificate { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

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

    public string? BurialCremationDate { get; set; }

    public string? BurialCremationTime { get; set; }

    public string? BurialCremationType { get; set; }

    public string? CauseOfDeath { get; set; }

    public string? DateOfDeath { get; set; }

    public string? DeceasedPersonName { get; set; }

    public string? FuneralHomeAddress { get; set; }

    public string? FuneralHomeName { get; set; }

    public string? RelationshipToDeceased { get; set; }

    public string? TimeOfDeath { get; set; }
}
