using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Dependency;

namespace HCMv3.Licensing;

/// <summary>
/// The licensing condition attached to every permission inside a licensed module, through ABP's own
/// <see cref="Abp.Authorization.Permission.FeatureDependency"/> -- the extension point ABP evaluates inside every
/// permission check, for the menu, for <c>[AbpAuthorize]</c> and for the permission list the Angular client receives.
///
/// <para>Satisfied when, for the permission's tenant:</para>
/// <list type="number">
/// <item>every module feature it needs is on (Option A: <see cref="HcmFeatures.ModuleEss"/> and the rest);</item>
/// <item>the permission's own original feature dependency, if it had one, is satisfied;</item>
/// <item>the signed-in user holds a licence for every module it needs, when per-user licences are on (Option B).</item>
/// </list>
///
/// <para>
/// <b>Which user.</b> ABP gives a feature dependency the tenant, not the user, so the licence part is judged for the
/// session user (see <see cref="IUserModuleLicenceChecker.SessionUserHoldsAsync"/>). That is the user in every
/// ordinary check. A check made ON BEHALF of another user -- ABP filtering notification recipients by permission,
/// say -- sees the module part correctly and the licence part of the caller; with no signed-in caller (background
/// jobs) the licence part passes.
/// </para>
/// </summary>
public class HcmModuleLicenceDependency : IFeatureDependency
{
    public HcmModuleLicenceDependency(IReadOnlyCollection<LicenceModule> modules, IFeatureDependency original)
    {
        Modules = modules;
        ModuleFeatures = modules
            .Select(m => LicenceModules.Get(m).ModuleFeature)
            .Where(f => f != null)
            .Distinct()
            .ToArray();
        Original = original;
    }

    public IReadOnlyCollection<LicenceModule> Modules { get; }

    public IReadOnlyCollection<string> ModuleFeatures { get; }

    public IFeatureDependency Original { get; }

    public async Task<bool> IsSatisfiedAsync(IFeatureDependencyContext context)
    {
        if (!context.TenantId.HasValue)
        {
            return true;
        }

        var tenantId = context.TenantId.Value;

        foreach (var feature in ModuleFeatures)
        {
            if (!await context.FeatureChecker.IsEnabledAsync(tenantId, feature))
            {
                return false;
            }
        }

        if (Original != null && !await Original.IsSatisfiedAsync(context))
        {
            return false;
        }

        using (var checker = context.IocResolver.ResolveAsDisposable<IUserModuleLicenceChecker>())
        {
            return await checker.Object.SessionUserHoldsAsync(tenantId, Modules);
        }
    }

    public bool IsSatisfied(IFeatureDependencyContext context)
    {
        if (!context.TenantId.HasValue)
        {
            return true;
        }

        var tenantId = context.TenantId.Value;

        foreach (var feature in ModuleFeatures)
        {
            if (!context.FeatureChecker.IsEnabled(tenantId, feature))
            {
                return false;
            }
        }

        if (Original != null && !Original.IsSatisfied(context))
        {
            return false;
        }

        using (var checker = context.IocResolver.ResolveAsDisposable<IUserModuleLicenceChecker>())
        {
            return checker.Object.SessionUserHolds(tenantId, Modules);
        }
    }
}
