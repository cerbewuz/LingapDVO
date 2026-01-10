using System;
using System.Collections.Generic;

namespace LingapDVO.TempVerify;

public partial class Superadminaccount
{
    public int Id { get; set; }

    public string Fullname { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;
}
