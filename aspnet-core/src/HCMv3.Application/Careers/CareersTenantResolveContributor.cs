using Abp.Dependency;
using Abp.MultiTenancy;

namespace HCMv3.Careers;

/// <summary>
/// Makes ABP's ambient tenant resolution understand careers subdomains, so that candidates can sign in
/// through the STOCK <c>/api/TokenAuth/Authenticate</c> endpoint instead of a bespoke careers login.
///
/// WHY THIS EXISTS. <c>TokenAuthController.Authenticate</c> picks the tenant to authenticate against from
/// <c>AbpSession.TenantId</c> (<c>TokenAuthController.cs:174</c> -&gt; <c>GetTenancyNameOrNull()</c> at
/// <c>:982-990</c>). On a careers subdomain nothing sets that today: ABP's global
/// <c>MultiTenancy.DomainFormat</c> is wired to <c>App:ServerRootAddress</c> in
/// <c>HCMv3WebHostModule.PreInitialize</c>, which carries no <c>{TENANCY_NAME}</c> placeholder at all. So a
/// candidate posting valid credentials from <c>acme.careers.example.com</c> would be authenticated as a
/// HOST user -- i.e. their tenant account would simply not be found, and any host account with the same
/// name would be. This contributor closes that gap. The alternative -- a second, careers-specific login
/// endpoint minting its own tokens -- would have re-implemented lockout, two-factor and session handling,
/// which is the kind of parallel auth path wave R2 exists to remove, not add.
///
/// WHY IT IS NOT A SECOND RESOLUTION PATH. The subdomain-to-tenancy-name extraction is not reimplemented
/// here; it delegates to <see cref="CareersTenantResolver.ExtractTenancyNameOrNull"/>, the R1 logic that is
/// unit-tested in <c>CareersTenantResolver_Tests</c>. This class only adapts that one string to ABP's
/// synchronous <see cref="ITenantResolveContributor"/> shape, using the cached
/// <see cref="ITenantStore"/> for the name-to-id step (the same thing ABP's own
/// <c>DomainTenantResolveContributor</c> does) because tenant resolution runs before any unit of work.
///
/// BLAST RADIUS. This is the one piece of R2 that changes GLOBAL wiring: it is consulted for every inbound
/// request, not just careers ones. It is safe because it is inert unless the host name matches the
/// configured <c>App:CareersDomainFormat</c>, and that key is unset today, so it returns <c>null</c> for
/// every request and ABP falls through to the existing contributors exactly as before. When it is
/// configured, the only hosts it can affect are the careers subdomains themselves, and on those it changes
/// the ambient tenant from "host" (wrong, and the more permissive answer) to the subdomain's tenant.
/// </summary>
public class CareersTenantResolveContributor : ITenantResolveContributor, ITransientDependency
{
    private readonly CareersTenantResolver _careersTenantResolver;
    private readonly ITenantStore _tenantStore;

    public CareersTenantResolveContributor(
        CareersTenantResolver careersTenantResolver,
        ITenantStore tenantStore)
    {
        _careersTenantResolver = careersTenantResolver;
        _tenantStore = tenantStore;
    }

    public int? ResolveTenantId()
    {
        var tenancyName = _careersTenantResolver.ExtractTenancyNameOrNull();
        if (tenancyName == null)
        {
            return null;
        }

        var tenant = _tenantStore.Find(tenancyName);
        if (tenant == null)
        {
            return null;
        }

        // NOTE on deactivated tenants: ITenantStore.Find returns a TenantInfo, which carries no IsActive
        // flag, so this contributor cannot refuse them here the way
        // CareersTenantResolver.ResolveTenantIdOrNullAsync does for the read surface. It does not need to:
        // the only thing downstream of ambient tenant resolution that matters for candidates is login, and
        // AbpLogInManager checks tenant.IsActive itself and fails with TenantIsNotActive. R1's resolver
        // still gates every careers data endpoint on IsActive independently.
        return tenant.Id;
    }
}
