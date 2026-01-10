using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class VerifiedAccount
{
    public int Id { get; set; }

    public string Idtype { get; set; } = null!;

    public string Idnumber { get; set; } = null!;

    public string FrontId { get; set; } = null!;

    public string BackId { get; set; } = null!;

    public string Firstname { get; set; } = null!;

    public string Middlename { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string Suffix { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Dateofbirth { get; set; } = null!;

    public string BlkLotStreet { get; set; } = null!;

    public string SubVill { get; set; } = null!;

    public string Barangay { get; set; } = null!;

    public int UserId { get; set; }

    public string Phonenumber { get; set; } = null!;

    public string CivilStatus { get; set; } = null!;

    public string Userfacepicture { get; set; } = null!;

    public string? Decision { get; set; }

    public string? TransactionId { get; set; }
}
