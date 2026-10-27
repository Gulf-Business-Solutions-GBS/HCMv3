using System;
using System.Collections.Generic;

namespace HCMv3.Sessions.Dto;

public class ApplicationInfoDto
{
    public string Version { get; set; }

    /// <summary>
    /// HCMv3's own version, base version plus build number (e.g. 3.0.531), which the build reads from git (see
    /// <c>ReleaseLog</c>) -- the one a tenant's installed version
    /// is compared against, and the one to show people. <see cref="Version"/> beside it is the ASP.NET Zero template's
    /// number, which moves when the template is upgraded and means nothing to a customer.
    /// </summary>
    public string ProductVersion { get; set; }

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

