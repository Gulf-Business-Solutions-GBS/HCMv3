using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using HCMv3.Authorization.Roles;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.Dashboards;

/// <inheritdoc cref="IDashboardScopeService"/>
/// <remarks>
/// The two halves of the restricted case come from <see cref="IRlsService"/> unchanged, so a dashboard and a grid
/// report can never disagree about which org units or which team a user has.
///
/// <para>Deliberately ignores the row-level security on/off switch in Security Settings (user decision,
/// 2026-09-30): a dashboard shows only what is assigned. Switching row-level security off must not open every
/// dashboard to everybody holding its permission.</para>
/// </remarks>
public class DashboardScopeService : HCMv3DomainServiceBase, IDashboardScopeService
{
    private readonly IRlsService _rlsService;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<SysUserOrgUnitData> _sysUserOrgUnitDataRepository;
    private readonly IAbpSession _abpSession;

    public DashboardScopeService(
        IRlsService rlsService,
        IEmployeeSessionService employeeSessionService,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IRepository<SysUserOrgUnitData> sysUserOrgUnitDataRepository,
        IAbpSession abpSession)
    {
        _rlsService = rlsService;
        _employeeSessionService = employeeSessionService;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _sysUserOrgUnitDataRepository = sysUserOrgUnitDataRepository;
        _abpSession = abpSession;
    }

    public async Task<DashboardScope> GetScopeAsync()
    {
        if (!_abpSession.UserId.HasValue)
        {
            return DashboardScope.Restricted(new List<int>(), new List<string>(), false);
        }

        var userId = _abpSession.UserId.Value;
        var roleIds = _userRoleRepository.GetAll()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToList();

        var isAdmin = roleIds.Count > 0 && _roleRepository.GetAll()
            .Any(r => roleIds.Contains(r.Id) && r.Name == StaticRoleNames.Tenants.Admin);
        if (isAdmin)
        {
            return DashboardScope.Everyone(DashboardScopeReason.Admin);
        }

        var employeeIds = new HashSet<int>(await _rlsService.GetEmployeeIdsUnderOrgUnitForCurrentUserAsync());

        var orgUnitNames = roleIds.Count == 0
            ? new List<string>()
            : _sysUserOrgUnitDataRepository.GetAll()
                .Where(d => roleIds.Contains(d.RoleId))
                .Select(d => d.OrganizationUnit.DisplayName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

        var currentEmployee = await _employeeSessionService.GetCurrentEmployeeAsync();
        var includesOwnTeam = false;
        if (currentEmployee != null)
        {
            // The manager walk always includes the employee themselves.
            employeeIds.UnionWith(await _rlsService.GetEmployeeIdsUnderManagerAsync(currentEmployee.Id));
            includesOwnTeam = true;
        }

        return DashboardScope.Restricted(employeeIds, orgUnitNames, includesOwnTeam);
    }
}
