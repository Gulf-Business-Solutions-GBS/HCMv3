namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The set of employees a self-service (ESS) or manager screen is allowed to act on, relative to the
/// currently logged-in user's own <c>Employee</c> row.
///
/// This is deliberately NOT the same concept as admin Row-Level Security
/// (<c>HCMv3.SystemSecurity.Services.IRlsService</c>). RLS answers "which employees may this HR/admin user
/// see in an admin grid", is switched on/off tenant-wide by
/// <c>ManageSecuritySettings.EnableRowLevelSecurity</c>, and is driven by the user's roles'
/// <c>SysUserOrgUnitData</c> org scoping. <see cref="EmployeeScope"/> answers "whose records is this
/// employee allowed to touch in ESS", always applies regardless of that setting, and is driven purely by
/// the employee-manager hierarchy. The two compose: an admin screen may apply RLS, an ESS screen applies a
/// scope, and a manager screen that reads admin data may apply both.
/// </summary>
public enum EmployeeScope
{
    /// <summary>Only the current user's own employee record. The default for every ESS "my ..." screen.</summary>
    Self = 0,

    /// <summary>
    /// Employees whose current-dated <c>EmployeeManager</c> row names the current user as manager, one
    /// level down only. Excludes the current user. Dotted-line managers
    /// (<c>EmployeeDottedLineManager</c>) are deliberately NOT included -- in the legacy system the dotted
    /// line is informational and never carried approval authority; add a separate scope value if a screen
    /// ever genuinely needs it rather than widening this one.
    /// </summary>
    DirectReports = 1,

    /// <summary><see cref="Self"/> plus <see cref="DirectReports"/>. The usual scope for a team calendar or a
    /// manager's request inbox.</summary>
    Team = 2,

    /// <summary>
    /// The current user's full downward manager tree at any depth, including the current user. Resolved by
    /// <c>IRlsService.GetEmployeeIdsUnderManagerAsync</c>, which is cycle-safe. Necessarily materialised in
    /// memory (a recursive walk has no single-query EF Core equivalent), so prefer <see cref="Team"/> where
    /// one level is enough.
    /// </summary>
    Subordinates = 3,

    /// <summary>
    /// No employee restriction at all. For admin screens that do their own authorization; an ESS endpoint
    /// should never pass this.
    /// </summary>
    All = 4
}
