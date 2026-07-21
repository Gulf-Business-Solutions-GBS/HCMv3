using System;
using System.Collections.Generic;

namespace HCMv3.Sessions.Dto;

public class ApplicationInfoDto
{
    public string Version { get; set; }

    public DateTime ReleaseDate { get; set; }

    public string Currency { get; set; }

    public string CurrencySign { get; set; }

    public bool AllowTenantsToChangeEmailSettings { get; set; }

    public bool UserDelegationIsEnabled { get; set; }

    public bool IsQrLoginEnabled { get; set; }

    public double TwoFactorCodeExpireSeconds { get; set; }

    public double PasswordlessLoginCodeExpireSeconds { get; set; }

    public Dictionary<string, bool> Features { get; set; }
}

