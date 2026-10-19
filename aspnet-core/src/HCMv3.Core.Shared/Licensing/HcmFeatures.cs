namespace HCMv3.Licensing;

/// <summary>
/// HCM's own ASP.NET Zero edition features. Declared by <c>HcmFeatureProvider</c>; ASP.NET Zero's own features stay
/// in <c>AppFeatures</c>, which is a template file and is not touched.
///
/// <para>
/// Only the host sets these, on the edition or per tenant (Zero's Editions and Tenants screens). That is what
/// replaces legacy's encrypted <c>LicenseConfiguration</c> record: the values live in the host database and a
/// tenant administrator has no screen to change them.
/// </para>
/// </summary>
public static class HcmFeatures
{
    // Module on/off (legacy LicenseConfigurationDetail.ModuleStatus). Every one defaults to ON so that an
    // upgrade switches nothing off; the host turns off what was not sold.
    public const string ModuleEss = "HCM.Module.Ess";
    public const string ModuleMobile = "HCM.Module.Mobile";
    public const string ModulePerformance = "HCM.Module.Performance";
    public const string ModuleRecruitment = "HCM.Module.Recruitment";

    /// <summary>
    /// Legacy licence line "Active Employees". 0 means unlimited. Checked when an employee becomes active
    /// (hire, reactivation, rejoin); employees who are already active are never touched.
    /// </summary>
    public const string MaxActiveEmployees = "HCM.MaxActiveEmployees";

    /// <summary>
    /// Per-user module licences (Option B). The successor of legacy <c>ManageSecuritySettings.EnableLicenseCheck</c>,
    /// moved from a tenant-editable security setting into a host-only edition feature. Off by default: a tenant
    /// without it works exactly as before, with nobody needing a licence assigned.
    /// </summary>
    public const string Seats = "HCM.Seats";

    // Seat counts per module (legacy LicenseConfigurationDetail.TotalLicenses / LicenseCategory). 0 means
    // unlimited -- legacy "Unlimited" -- but a user still needs the module assigned while Seats is on.
    public const string SeatsAdmin = "HCM.Seats.Admin";
    public const string SeatsEss = "HCM.Seats.Ess";
    public const string SeatsMobile = "HCM.Seats.Mobile";
    public const string SeatsPerformance = "HCM.Seats.Performance";
    public const string SeatsRecruitment = "HCM.Seats.Recruitment";
}
