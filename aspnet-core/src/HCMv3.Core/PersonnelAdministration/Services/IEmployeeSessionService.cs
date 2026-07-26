using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Current-user/session-to-Employee resolution helpers (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.SysService</c>'s user/session-related members). See
/// <see cref="EmployeeSessionService"/> remarks for adaptations.
/// </summary>
public interface IEmployeeSessionService
{
    /// <summary>
    /// Resolves the currently logged-in ABP user (<c>AbpSession.UserId</c>) to their <see cref="Employee"/>
    /// row via <c>Employee.LoginID == user.UserName</c>. Ported from legacy <c>SysService.GetLoggedinUser</c>
    /// (which matched on <c>DevExpress.ExpressApp.SecuritySystem.CurrentUserName</c> instead of an ABP
    /// session). Returns <c>null</c> if there is no current session user or no matching Employee.
    /// </summary>
    Task<Employee> GetCurrentEmployeeAsync();

    /// <summary>
    /// Resolves the display Employee <c>Code</c> for a given legacy "SysCreatedBy" login name, preferring
    /// an active employee if more than one shares the login (ported near-verbatim from legacy
    /// <c>SysService.GetSysCreatedBy</c>). Falls back to <paramref name="sysCreator"/> itself if no
    /// matching employee is found.
    /// </summary>
    Task<string> GetSysCreatedByAsync(string sysCreator);

    /// <summary>
    /// Returns whether the current ABP user holds one of this app's admin-equivalent roles. ADAPTED from
    /// legacy <c>SysService.IsAdminRole</c>: legacy checked a hardcoded list of XAF permission-policy role
    /// names (<c>AdminRolesList</c>: "Client Administrator", "Administrators", "SuperAdminRole", "Payroll
    /// Admin") that don't exist in this port's role model (only <c>StaticRoleNames.Tenants.Admin</c>/
    /// <c>Host.Admin</c> ="Admin" exist -- see <see cref="EmployeeSessionService"/> remarks). Simplified to
    /// check the current user against those two.
    /// </summary>
    Task<bool> IsCurrentUserInAdminRoleAsync();
}
