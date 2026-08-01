using System.Threading.Tasks;

namespace HCMv3.Careers;

/// <summary>
/// Resolves which tenant a public (anonymous) careers-portal request belongs to, from the request's
/// subdomain.
///
/// Why this exists instead of relying on <c>AbpSession.TenantId</c>: the careers portal has no
/// authenticated session, so <c>AbpSession.TenantId</c> is <c>null</c> on every request. A <c>null</c>
/// session tenant means ABP treats the caller as a *host* user, which **disables** the
/// <c>MustHaveTenant</c> data filter -- so an unguarded query on a public endpoint would return every
/// tenant's vacancies. Every careers app-service method therefore resolves the tenant explicitly through
/// this interface and fails closed when it cannot.
/// </summary>
public interface ICareersTenantResolver
{
    /// <summary>
    /// Returns the id of the tenant that owns the current request's subdomain, or <c>null</c> when the
    /// tenant cannot be determined.
    ///
    /// Returning <c>null</c> NEVER means "host" and callers must never treat it as such -- it means the
    /// request must be rejected. Specifically this returns <c>null</c> when: no careers domain format is
    /// configured, there is no ambient HTTP request, the host name does not match the configured format,
    /// the extracted subdomain is empty or <c>www</c>, no tenant has that tenancy name, or the tenant
    /// exists but is deactivated.
    /// </summary>
    Task<int?> ResolveTenantIdOrNullAsync();
}
