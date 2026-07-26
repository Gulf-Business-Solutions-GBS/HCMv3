using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using Abp.Runtime.Session;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings.Services;

namespace HCMv3.SystemSecurity.Services;

/// <inheritdoc cref="IRlsService"/>
/// <remarks>
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so all queries here
/// use the plain synchronous <c>IQueryable</c> returned by <c>IRepository&lt;T&gt;.GetAll()</c> (same
/// pattern as <see cref="OrganizationManagement.Services.OrgHierarchyService"/>/
/// <see cref="GeneralMasterInfo.Services.CurrencyService"/>), wrapped in <c>Task.FromResult</c> for
/// interface consistency.
/// </remarks>
[UnitOfWork]
public class RlsService : HCMv3DomainServiceBase, IRlsService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<SysUserOrgUnitData> _sysUserOrgUnitDataRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IAbpSession _abpSession;

    public RlsService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<SysUserOrgUnitData> sysUserOrgUnitDataRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<UserRole, long> userRoleRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IEmployeeSessionService employeeSessionService,
        IAbpSession abpSession)
    {
        _employeeRepository = employeeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _sysUserOrgUnitDataRepository = sysUserOrgUnitDataRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _userRoleRepository = userRoleRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _employeeSessionService = employeeSessionService;
        _abpSession = abpSession;
    }

    public async Task<List<int>> GetRlsEmployeeIdsAsync()
    {
        var securitySettings = await _sysSettingsAccessor.GetSecuritySettingsAsync();
        if (securitySettings == null || !securitySettings.EnableRowLevelSecurity)
        {
            return new List<int>();
        }

        var result = new HashSet<int>();

        var currentEmployee = await _employeeSessionService.GetCurrentEmployeeAsync();
        if (currentEmployee != null)
        {
            result.UnionWith(await GetEmployeeIdsUnderManagerAsync(currentEmployee.Id));
        }

        result.UnionWith(await GetEmployeeIdsUnderOrgUnitForCurrentUserAsync());

        return result.ToList();
    }

    public async Task<List<string>> GetRlsEmployeeCodesAsync()
    {
        var employeeIds = await GetRlsEmployeeIdsAsync();

        return _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => e.Code)
            .ToList();
    }

    public Task<List<int>> GetEmployeeIdsUnderManagerAsync(int employeeId)
    {
        return Task.FromResult(GetEmployeeIdsUnderManager(employeeId, new HashSet<int>()));
    }

    private List<int> GetEmployeeIdsUnderManager(int employeeId, HashSet<int> visited)
    {
        if (employeeId <= 0 || visited.Contains(employeeId))
        {
            return new List<int>();
        }

        visited.Add(employeeId);
        var result = new List<int> { employeeId };

        var subordinateIds = _employeeManagerRepository.GetAll()
            .Where(r => r.ManagerId == employeeId &&
                        r.ManagerId != r.EmployeeId &&
                        r.StartDate <= System.DateTime.Today &&
                        r.EndDate >= System.DateTime.Today)
            .Select(r => r.EmployeeId)
            .ToList();

        foreach (var subordinateId in subordinateIds)
        {
            result.AddRange(GetEmployeeIdsUnderManager(subordinateId, visited));
        }

        return result;
    }

    public Task<List<int>> GetEmployeeIdsUnderOrgUnitForCurrentUserAsync()
    {
        var result = new HashSet<int>();

        if (!_abpSession.UserId.HasValue)
        {
            return Task.FromResult(result.ToList());
        }

        var roleIds = _userRoleRepository.GetAll()
            .Where(ur => ur.UserId == _abpSession.UserId.Value)
            .Select(ur => ur.RoleId)
            .ToList();

        if (roleIds.Count == 0)
        {
            return Task.FromResult(result.ToList());
        }

        var sysUserOrgData = _sysUserOrgUnitDataRepository
            .GetAllIncluding(r => r.OrgLevel)
            .Where(r => roleIds.Contains(r.RoleId))
            .Select(r => new { OrgLevelCode = r.OrgLevel.Code, r.OrganizationUnitId })
            .ToList();

        var allOrgUnits = new HashSet<long>();
        var hasRootPermit = false;

        foreach (var data in sysUserOrgData)
        {
            allOrgUnits.UnionWith(GetOrgUnitDescendantIds(data.OrganizationUnitId));

            if (data.OrgLevelCode == OrganizationUnitType.Level0Code)
            {
                hasRootPermit = true;
            }
        }

        if (allOrgUnits.Count > 0)
        {
            var employeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Where(a => a.StartDate <= System.DateTime.Today &&
                            a.EndDate >= System.DateTime.Today &&
                            a.OrganizationUnitId != null &&
                            allOrgUnits.Contains(a.OrganizationUnitId.Value))
                .Select(a => a.EmployeeId)
                .ToList();

            result.UnionWith(employeeIds);
        }

        if (hasRootPermit)
        {
            var assignedEmployeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            var unassignedEmployeeIds = _employeeRepository.GetAll()
                .Where(e => !assignedEmployeeIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToList();

            result.UnionWith(unassignedEmployeeIds);
        }

        return Task.FromResult(result.ToList());
    }

    public Task<List<long>> GetOrgUnitDescendantIdsAsync(long organizationUnitId)
    {
        return Task.FromResult(GetOrgUnitDescendantIds(organizationUnitId));
    }

    private List<long> GetOrgUnitDescendantIds(long organizationUnitId)
    {
        var organizationUnit = _organizationUnitRepository.FirstOrDefault(organizationUnitId);
        if (organizationUnit == null)
        {
            return new List<long>();
        }

        return _organizationUnitRepository.GetAll()
            .Where(ou => ou.Code.StartsWith(organizationUnit.Code))
            .Select(ou => ou.Id)
            .ToList();
    }
}
