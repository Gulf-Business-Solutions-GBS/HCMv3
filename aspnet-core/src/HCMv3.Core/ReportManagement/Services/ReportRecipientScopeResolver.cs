using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings.Services;

namespace HCMv3.ReportManagement.Services;

/// <inheritdoc cref="IReportRecipientScopeResolver"/>
public class ReportRecipientScopeResolver : HCMv3DomainServiceBase, IReportRecipientScopeResolver
{
    private readonly IRepository<SysUserOrgUnitData> _sysUserOrgUnitDataRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRlsService _rlsService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public ReportRecipientScopeResolver(
        IRepository<SysUserOrgUnitData> sysUserOrgUnitDataRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Employee> employeeRepository,
        IRlsService rlsService,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _sysUserOrgUnitDataRepository = sysUserOrgUnitDataRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _userRoleRepository = userRoleRepository;
        _employeeRepository = employeeRepository;
        _rlsService = rlsService;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<HashSet<int>> GetVisibleEmployeeIdsForUserAsync(long userId)
    {
        if (!await IsRowLevelSecurityEnabledAsync())
        {
            return null; // null == unrestricted; see the interface remarks on why this is not an empty set.
        }

        var result = new HashSet<int>();

        // Half 1: the employee behind this user, plus everyone under them in the manager hierarchy.
        var employeeId = _employeeRepository.GetAll()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefault();

        if (employeeId.HasValue)
        {
            result.UnionWith(await _rlsService.GetEmployeeIdsUnderManagerAsync(employeeId.Value));
        }

        // Half 2: everything the user's ROLES scope them to.
        var roleIds = _userRoleRepository.GetAll()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToList();

        result.UnionWith(await GetEmployeeIdsForRolesAsync(roleIds));

        return result;
    }

    public async Task<HashSet<int>> GetVisibleEmployeeIdsForEmployeeAsync(int employeeId)
    {
        if (!await IsRowLevelSecurityEnabledAsync())
        {
            return null;
        }

        var result = new HashSet<int>(await _rlsService.GetEmployeeIdsUnderManagerAsync(employeeId));

        // An employee-identified recipient may still hold scoping roles, either through a linked user
        // account or through SysUserOrgUnitData rows written directly against the employee.
        var userId = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.UserId)
            .FirstOrDefault();

        if (userId.HasValue)
        {
            var roleIds = _userRoleRepository.GetAll()
                .Where(ur => ur.UserId == userId.Value)
                .Select(ur => ur.RoleId)
                .ToList();

            result.UnionWith(await GetEmployeeIdsForRolesAsync(roleIds));
        }

        var directOrgUnitIds = _sysUserOrgUnitDataRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => r.OrganizationUnitId)
            .ToList();

        if (directOrgUnitIds.Count > 0)
        {
            result.UnionWith(await GetEmployeeIdsInOrgUnitsAsync(directOrgUnitIds, includeUnassigned: false));
        }

        return result;
    }

    private async Task<bool> IsRowLevelSecurityEnabledAsync()
    {
        var securitySettings = await _sysSettingsAccessor.GetSecuritySettingsAsync();
        return securitySettings != null && securitySettings.EnableRowLevelSecurity;
    }

    /// <summary>
    /// Employee ids implied by a set of roles, via their <see cref="SysUserOrgUnitData"/> scoping rows.
    /// Mirrors <c>RlsService.GetEmployeeIdsUnderOrgUnitForCurrentUserAsync</c>, including its rule that a
    /// Level0 (root) scope also grants visibility of employees with NO org assignment at all -- without
    /// that, new joiners awaiting assignment silently vanish from every scoped report.
    /// </summary>
    private async Task<HashSet<int>> GetEmployeeIdsForRolesAsync(List<int> roleIds)
    {
        if (roleIds == null || roleIds.Count == 0)
        {
            return new HashSet<int>();
        }

        var scopeRows = _sysUserOrgUnitDataRepository.GetAllIncluding(r => r.OrgLevel)
            .Where(r => roleIds.Contains(r.RoleId))
            .Select(r => new { OrgLevelCode = r.OrgLevel.Code, r.OrganizationUnitId })
            .ToList();

        if (scopeRows.Count == 0)
        {
            return new HashSet<int>();
        }

        var hasRootScope = scopeRows.Any(r => r.OrgLevelCode == OrganizationUnitType.Level0Code);
        var orgUnitIds = scopeRows.Select(r => r.OrganizationUnitId).Distinct().ToList();

        return await GetEmployeeIdsInOrgUnitsAsync(orgUnitIds, hasRootScope);
    }

    private async Task<HashSet<int>> GetEmployeeIdsInOrgUnitsAsync(List<long> orgUnitIds, bool includeUnassigned)
    {
        var expanded = new HashSet<long>();
        foreach (var id in orgUnitIds)
        {
            expanded.UnionWith(await _rlsService.GetOrgUnitDescendantIdsAsync(id));
        }

        var assignments = _employeeOrgAssignmentRepository.GetAll();

        var result = new HashSet<int>(assignments
            .Where(a => a.OrganizationUnitId.HasValue && expanded.Contains(a.OrganizationUnitId.Value))
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToList());

        if (includeUnassigned)
        {
            result.UnionWith(assignments
                .Where(a => !a.OrganizationUnitId.HasValue)
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList());
        }

        return result;
    }
}
