using System.Collections.Generic;
using System.Linq;
using Abp.Authorization;

namespace HCMv3.Licensing;

/// <summary>
/// Ties every permission of a licensed module to its licence, without editing the permission definitions in
/// <c>AppAuthorizationProvider</c>. Runs after it and sets ABP's <see cref="Permission.FeatureDependency"/> on each
/// permission in the trees named by <see cref="LicenceModules"/>, and on everything below them.
///
/// <para>
/// <b>Every descendant, not just the root.</b> ABP's permission check looks at the dependency of the permission asked
/// for and does not walk up to its parents, so a dependency on <c>Pages.Recruitment</c> alone would leave
/// <c>Pages.Recruitment.Applicants.Create</c> open.
/// </para>
///
/// <para>
/// What this gives, with no other change: a module switched off in the tenant's edition, or a module the user holds
/// no licence for, makes its permissions not granted. ABP then refuses its app services (<c>[AbpAuthorize]</c>), leaves
/// them out of the permission list the Angular client receives -- so menu items and routes guarded by those
/// permissions disappear -- and leaves them out of the role and user permission trees.
/// </para>
///
/// <para>
/// Must be registered AFTER <c>AppAuthorizationProvider</c>, in <c>HCMv3ApplicationModule.PreInitialize</c>:
/// <code>Configuration.Authorization.Providers.Add&lt;HcmLicensingAuthorizationProvider&gt;();</code>
/// </para>
/// </summary>
public class HcmLicensingAuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        var modulesByRoot = LicenceModules.All
            .SelectMany(d => d.PermissionRoots.Select(root => new { Root = root, d.Module }))
            .GroupBy(x => x.Root)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Module).ToList());

        // A definition context only looks up top-level permissions by name, so the walk starts from the top-level
        // name of every tree ("Pages", "Ess") and finds the nested roots on the way down.
        var topLevelNames = modulesByRoot.Keys
            .Select(name => name.Split('.')[0])
            .Distinct();

        foreach (var name in topLevelNames)
        {
            var topLevel = context.GetPermissionOrNull(name);
            if (topLevel != null)
            {
                Attach(topLevel, new List<LicenceModule>(), modulesByRoot);
            }
        }
    }

    private static void Attach(
        Permission permission,
        List<LicenceModule> inherited,
        IReadOnlyDictionary<string, List<LicenceModule>> modulesByRoot)
    {
        var modules = inherited;
        if (modulesByRoot.TryGetValue(permission.Name, out var own))
        {
            modules = inherited.Union(own).ToList();
        }

        if (modules.Count > 0)
        {
            // Idempotent: a provider run twice wraps the ORIGINAL dependency again, never its own wrapper.
            var original = permission.FeatureDependency is HcmModuleLicenceDependency existing
                ? existing.Original
                : permission.FeatureDependency;

            permission.FeatureDependency = new HcmModuleLicenceDependency(modules, original);
        }

        foreach (var child in permission.Children)
        {
            Attach(child, modules, modulesByRoot);
        }
    }
}
