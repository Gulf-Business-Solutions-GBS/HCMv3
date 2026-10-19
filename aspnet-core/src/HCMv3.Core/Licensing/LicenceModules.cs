using System.Collections.Generic;
using System.Linq;
using HCMv3.Authorization;

namespace HCMv3.Licensing;

/// <summary>
/// What each licensed module means in HCMv3 terms: the edition feature that switches it on, the edition feature
/// that holds its seat count, and the permission trees it covers.
///
/// <para>
/// A permission tree here is a permission name and everything below it. Trees nest on purpose: <c>Pages</c> is the
/// whole administration application (seat <see cref="LicenceModule.Admin"/>), and <c>Pages.Recruitment</c> inside it
/// is also Recruitment -- so a recruiter needs the module switched on, an Admin seat and a Recruitment seat. The
/// mobile permissions sit inside <c>Ess</c> the same way.
/// </para>
/// </summary>
public static class LicenceModules
{
    public sealed class Definition
    {
        public Definition(LicenceModule module, string moduleFeature, string seatFeature, string displayNameKey, params string[] permissionRoots)
        {
            Module = module;
            ModuleFeature = moduleFeature;
            SeatFeature = seatFeature;
            DisplayNameKey = displayNameKey;
            PermissionRoots = permissionRoots;
        }

        public LicenceModule Module { get; }

        /// <summary>The on/off edition feature, or null for a module that is always part of the product.</summary>
        public string ModuleFeature { get; }

        public string SeatFeature { get; }

        public string DisplayNameKey { get; }

        public IReadOnlyList<string> PermissionRoots { get; }
    }

    public static readonly IReadOnlyList<Definition> All = new List<Definition>
    {
        // The administration application has no on/off switch: it is the product.
        new(LicenceModule.Admin, null, HcmFeatures.SeatsAdmin, "LicenceModuleAdmin", AppPermissions.Pages),
        new(LicenceModule.Ess, HcmFeatures.ModuleEss, HcmFeatures.SeatsEss, "LicenceModuleEss", AppPermissions.Ess),
        new(LicenceModule.Mobile, HcmFeatures.ModuleMobile, HcmFeatures.SeatsMobile, "LicenceModuleMobile", AppPermissions.Ess_GeoPunch, AppPermissions.Ess_MyMobileDevices),
        new(LicenceModule.Performance, HcmFeatures.ModulePerformance, HcmFeatures.SeatsPerformance, "LicenceModulePerformance", AppPermissions.Pages_PerformanceManagement),
        new(LicenceModule.Recruitment, HcmFeatures.ModuleRecruitment, HcmFeatures.SeatsRecruitment, "LicenceModuleRecruitment", AppPermissions.Pages_Recruitment)
    };

    public static Definition Get(LicenceModule module)
    {
        return All.First(d => d.Module == module);
    }

    /// <summary>
    /// The modules ESS and Mobile seats are given out for automatically when a user is linked to an employee, as
    /// legacy did when it created the employee's user.
    /// </summary>
    public static readonly IReadOnlyList<LicenceModule> AssignedOnEmployeeLink = new[] { LicenceModule.Ess, LicenceModule.Mobile };
}
