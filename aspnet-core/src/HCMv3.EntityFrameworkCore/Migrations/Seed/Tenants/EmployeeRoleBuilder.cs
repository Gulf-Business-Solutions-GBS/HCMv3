using System.Linq;
using Abp.Authorization.Roles;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Creates the tenant's "Employee" role and grants it the Employee Self Service permissions.
///
/// <para>
/// This role is what makes a single application serve both HR and employees. It is granted <c>Ess.*</c> and
/// nothing else, so a user holding only this role sees none of the ~575 admin menu items -- they are
/// permission-filtered and simply do not render. Legacy HCM10 needed an entire separate ASP.NET project to
/// achieve the same separation.
/// </para>
///
/// <para>
/// Granted explicitly rather than via <c>grantAllPermissionsByDefault</c>, which would hand the role every
/// <c>Pages.*</c> administrative screen as well.
/// </para>
///
/// <para>
/// IDEMPOTENT and non-destructive: the role is created only if absent, and each permission row only if that
/// permission is not already recorded for the role. Re-running never revokes a grant a tenant admin removed
/// deliberately, and never duplicates one.
/// </para>
/// </summary>
public class EmployeeRoleBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    /// <summary>
    /// Everything an ordinary employee needs. Deliberately excludes <c>Ess.MyTeam</c>: that is a manager's
    /// view of their reports, granted per person rather than to everyone.
    /// </summary>
    private static readonly string[] EssPermissions =
    {
        AppPermissions.Ess,
        AppPermissions.Ess_Inbox,
        AppPermissions.Ess_MyRequests,
        AppPermissions.Ess_MyProfile,
        AppPermissions.Ess_MyLeave,
        AppPermissions.Ess_MyAssets,
        AppPermissions.Ess_MyDocuments,
        AppPermissions.Ess_MyLoans,
        AppPermissions.Ess_MyOvertime,
        AppPermissions.Ess_MyTickets,
        AppPermissions.Ess_MyAttendance,
        AppPermissions.Ess_MyBank,
        AppPermissions.Ess_MyTravelClaims
    };

    public EmployeeRoleBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var employeeRole = _context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Employee);

        if (employeeRole == null)
        {
            // IsDefault false: this role is assigned when a user is linked to an employee, not handed to
            // every new account.
            employeeRole = _context.Roles.Add(
                new Role(_tenantId, StaticRoleNames.Tenants.Employee, StaticRoleNames.Tenants.Employee)
                {
                    IsStatic = true,
                    IsDefault = false
                }).Entity;

            _context.SaveChanges();
        }

        var existingPermissions = _context.Permissions
            .IgnoreQueryFilters()
            .OfType<RolePermissionSetting>()
            .Where(p => p.TenantId == _tenantId && p.RoleId == employeeRole.Id)
            .Select(p => p.Name)
            .ToList();

        var added = false;

        foreach (var permission in EssPermissions.Where(p => !existingPermissions.Contains(p)))
        {
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = _tenantId,
                RoleId = employeeRole.Id,
                Name = permission,
                IsGranted = true
            });

            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
