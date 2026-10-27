using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.Licensing;

/// <summary>
/// Answers "may this user use these modules" for per-user module licences (Option B). Read side only; changes go
/// through <see cref="IUserModuleLicenceManager"/>.
///
/// <para>
/// A user passes when any of these is true: per-user licences are off for the tenant (<see cref="HcmFeatures.Seats"/>),
/// the user holds the static Admin role (legacy never checked its system and super-admin users), or the user holds
/// every module asked for.
/// </para>
/// </summary>
public interface IUserModuleLicenceChecker
{
    Task<bool> IsEnforcedAsync(int tenantId);

    bool IsEnforced(int tenantId);

    /// <summary>
    /// For the CURRENT session user. Passes when there is no signed-in user (background jobs, anonymous calls) and
    /// during impersonation -- the impersonating administrator is the one acting, and must be able to see what the
    /// employee sees.
    /// </summary>
    Task<bool> SessionUserHoldsAsync(int tenantId, IReadOnlyCollection<LicenceModule> modules);

    /// <inheritdoc cref="SessionUserHoldsAsync"/>
    bool SessionUserHolds(int tenantId, IReadOnlyCollection<LicenceModule> modules);

    Task<bool> UserHoldsAsync(int tenantId, long userId, IReadOnlyCollection<LicenceModule> modules);

    bool UserHolds(int tenantId, long userId, IReadOnlyCollection<LicenceModule> modules);

    /// <summary>
    /// For an optional sign-in hook (legacy refused the login itself). Throws a user-friendly error when licences are
    /// enforced and the user holds neither an Admin nor an ESS licence, so would land in an empty application.
    /// Not called by anything in HCMv3 today: without it the user signs in and every unlicensed screen and API is
    /// refused, which is the enforcement that matters.
    /// </summary>
    Task EnsureCanSignInAsync(int? tenantId, long userId);

    Task<UserLicenceCacheItem> GetAsync(int tenantId, long userId);

    Task InvalidateAsync(int tenantId, long userId);
}
