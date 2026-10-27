using Abp.Domain.Services;

namespace HCMv3.Authorization.Users.Password;

/// <remarks>
/// CONFIRMED REDUNDANT WITH LEGACY: legacy XAF <c>HCM10.Module.BusinessObjects.Services.SysService
/// .AutoPasswordExpiry()</c> is NOT separately ported -- this service's
/// <see cref="ForcePasswordExpiredUsersToChangeTheirPassword"/> (run daily by
/// <c>PasswordExpirationBackgroundWorker</c>) already covers the same behavior: both compute an expiry
/// cutoff from "days since the user's last password change" (legacy: <c>PasswordHistory.PasswordChangedOn
/// + PasswordPolicy.PasswordExpireAfterDays</c> vs. this port's <c>Clock.Now - PasswordExpirationDayCount</c>
/// setting) and, once past it, force the affected user(s) to change their password on next login (legacy:
/// <c>PermissionPolicyUser.ChangePasswordOnFirstLogon = true</c> vs. this port's
/// <c>UpdateUsersToChangePasswordOnNextLogin</c>). No separate legacy-faithful port was needed.
/// </remarks>
public interface IPasswordExpirationService : IDomainService
{
    void ForcePasswordExpiredUsersToChangeTheirPassword();
}

