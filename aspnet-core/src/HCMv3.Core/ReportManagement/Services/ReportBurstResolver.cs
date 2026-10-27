using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.Organizations;
using HCMv3.Authorization.Users;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;

namespace HCMv3.ReportManagement.Services;

/// <inheritdoc cref="IReportBurstResolver"/>
public class ReportBurstResolver : HCMv3DomainServiceBase, IReportBurstResolver
{
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SysUserOrgUnitData> _sysUserOrgUnitDataRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly HCMv3.SystemSecurity.Services.IRlsService _rlsService;

    public ReportBurstResolver(
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<Employee> employeeRepository,
        IRepository<SysUserOrgUnitData> sysUserOrgUnitDataRepository,
        IRepository<User, long> userRepository,
        IRepository<UserRole, long> userRoleRepository,
        HCMv3.SystemSecurity.Services.IRlsService rlsService)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeRepository = employeeRepository;
        _sysUserOrgUnitDataRepository = sysUserOrgUnitDataRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _rlsService = rlsService;
    }

    public Task<List<ReportBurstSlice>> ResolveSlicesAsync(ReportSchedule schedule)
    {
        if (schedule == null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        var slices = schedule.BurstDimension switch
        {
            ReportBurstDimension.None => BuildSingleSlice(),
            ReportBurstDimension.OrganizationUnit => BuildOrgUnitSlices(schedule),
            ReportBurstDimension.Manager => BuildManagerSlices(),
            ReportBurstDimension.Sponsor => BuildSponsorSlices(),
            ReportBurstDimension.PayrollArea => BuildPayrollAreaSlices(),
            _ => BuildSingleSlice(),
        };

        foreach (var slice in slices)
        {
            slice.Recipients = ResolveRecipients(schedule, slice);
        }

        // Drop keys nobody receives. Rendering a file with no recipient is wasted work, and logging it
        // as a success would misreport the run.
        return Task.FromResult(slices.Where(s => s.Recipients.Count > 0).ToList());
    }

    private static List<ReportBurstSlice> BuildSingleSlice()
    {
        return new List<ReportBurstSlice> { new ReportBurstSlice() };
    }

    private List<ReportBurstSlice> BuildOrgUnitSlices(ReportSchedule schedule)
    {
        // Only units of the chosen LEVEL become slices. Bursting "by department" and "by division" are
        // the same mechanism pointed at a different OrganizationUnitType.
        var profiles = _orgUnitProfileRepository.GetAllIncluding(p => p.OrganizationUnit)
            .WhereIf(
                schedule.BurstOrganizationUnitTypeId.HasValue,
                p => p.OrganizationUnitTypeId == schedule.BurstOrganizationUnitTypeId.Value)
            .Select(p => new { p.OrganizationUnitId, p.OrganizationUnit.DisplayName })
            .ToList();

        return profiles
            .Select(p => new ReportBurstSlice
            {
                Key = p.OrganizationUnitId.ToString(),
                Display = p.DisplayName,
                OrganizationUnitId = p.OrganizationUnitId,
                EmployeeIds = GetEmployeeIdsInOrgUnitSubtree(p.OrganizationUnitId),
            })
            .ToList();
    }

    private List<ReportBurstSlice> BuildManagerSlices()
    {
        var today = Clock.Now.Date;

        var managerIds = _employeeManagerRepository.GetAll()
            .Where(m => m.StartDate <= today && m.EndDate >= today)
            .Select(m => m.ManagerId)
            .Distinct()
            .ToList();

        var managers = _employeeRepository.GetAll()
            .Where(e => managerIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FullNameEn, e.Code })
            .ToList();

        return managers
            .Select(m => new ReportBurstSlice
            {
                Key = m.Id.ToString(),
                Display = string.IsNullOrWhiteSpace(m.FullNameEn) ? m.Code : m.FullNameEn,
                ManagerEmployeeId = m.Id,
                EmployeeIds = GetEmployeeIdsUnderManager(m.Id),
            })
            .ToList();
    }

    private List<ReportBurstSlice> BuildSponsorSlices()
    {
        var today = Clock.Now.Date;

        return _employeeSponsorRepository.GetAll()
            .Where(es => es.StartDate <= today && es.EndDate >= today && es.Sponsor != null)
            .Select(es => new { es.SponsorId, es.Sponsor.NameEn })
            .Distinct()
            .ToList()
            .Select(s => new ReportBurstSlice
            {
                Key = s.SponsorId.ToString(),
                Display = s.NameEn,
                EmployeeIds = GetEmployeeIdsForSponsor(s.SponsorId),
            })
            .ToList();
    }

    private List<ReportBurstSlice> BuildPayrollAreaSlices()
    {
        return _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.PayrollAreaId.HasValue && a.PayrollArea != null)
            .Select(a => new { a.PayrollAreaId, a.PayrollArea.Name })
            .Distinct()
            .ToList()
            .Select(p => new ReportBurstSlice
            {
                Key = p.PayrollAreaId.Value.ToString(),
                Display = p.Name,
                EmployeeIds = GetEmployeeIdsForPayrollArea(p.PayrollAreaId.Value),
            })
            .ToList();
    }

    // ---- the burst key, expressed as employees -----------------------------------------------------
    //
    // Each slice resolves to the SET OF EMPLOYEES it covers. That is what lets the burst key reach the
    // report without any report knowing about scheduling: grid reports already filter on an employee id
    // set, so the executor simply intersects this with the recipient's entitlement and publishes one
    // set. See ReportBurstSlice.EmployeeIds.

    private HashSet<int> GetEmployeeIdsInOrgUnitSubtree(long organizationUnitId)
    {
        // The whole SUBTREE, not just the unit itself. Bursting "by department" has to include the
        // sections beneath it, or a head receives a file missing most of their own people.
        var descendants = new HashSet<long>(
            _rlsService.GetOrgUnitDescendantIdsAsync(organizationUnitId).GetAwaiter().GetResult());

        return new HashSet<int>(_employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.OrganizationUnitId.HasValue && descendants.Contains(a.OrganizationUnitId.Value))
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToList());
    }

    private HashSet<int> GetEmployeeIdsUnderManager(int managerEmployeeId)
    {
        // Includes the manager themselves, matching how the interactive reports scope a manager.
        return new HashSet<int>(
            _rlsService.GetEmployeeIdsUnderManagerAsync(managerEmployeeId).GetAwaiter().GetResult());
    }

    private HashSet<int> GetEmployeeIdsForSponsor(int sponsorId)
    {
        var today = Clock.Now.Date;

        return new HashSet<int>(_employeeSponsorRepository.GetAll()
            .Where(es => es.SponsorId == sponsorId && es.StartDate <= today && es.EndDate >= today)
            .Select(es => es.EmployeeId)
            .Distinct()
            .ToList());
    }

    private HashSet<int> GetEmployeeIdsForPayrollArea(int payrollAreaId)
    {
        return new HashSet<int>(_employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.PayrollAreaId == payrollAreaId)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToList());
    }

    /// <summary>
    /// Applies every recipient RULE on the schedule to one slice, de-duplicating the result: a person
    /// who is both the department head and a member of the HR team receives one copy, not two.
    /// </summary>
    private List<ReportSliceRecipient> ResolveRecipients(ReportSchedule schedule, ReportBurstSlice slice)
    {
        var byKey = new Dictionary<string, ReportSliceRecipient>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in schedule.Recipients ?? Enumerable.Empty<ReportScheduleRecipient>())
        {
            foreach (var resolved in ResolveRule(rule, slice))
            {
                if (string.IsNullOrWhiteSpace(resolved.Email))
                {
                    continue; // no address, nothing to deliver to
                }

                if (byKey.TryGetValue(resolved.Email, out var existing))
                {
                    // If ANY rule says this copy must be scoped, it is scoped. The safe reading wins,
                    // so adding a broad unscoped rule can never silently widen an existing recipient.
                    existing.ApplyRecipientScope = existing.ApplyRecipientScope || resolved.ApplyRecipientScope;
                    continue;
                }

                byKey[resolved.Email] = resolved;
            }
        }

        return byKey.Values.ToList();
    }

    private IEnumerable<ReportSliceRecipient> ResolveRule(ReportScheduleRecipient rule, ReportBurstSlice slice)
    {
        switch (rule.Rule)
        {
            case ReportRecipientRule.Explicit:
                if (!string.IsNullOrWhiteSpace(rule.EmailAddress))
                {
                    yield return new ReportSliceRecipient
                    {
                        Email = rule.EmailAddress.Trim(),
                        DisplayName = rule.EmailAddress.Trim(),
                        // Only an explicit rule may opt out of scoping, and only when the app service
                        // verified the author holds the elevated permission.
                        ApplyRecipientScope = !rule.BypassRecipientScope,
                        ResolvedByRule = ReportRecipientRule.Explicit,
                    };
                }

                break;

            case ReportRecipientRule.RoleScoped:
                foreach (var r in ResolveRoleScoped(rule, slice))
                {
                    yield return r;
                }

                break;

            case ReportRecipientRule.OrgUnitHead:
                foreach (var r in ResolveOrgUnitHead(slice))
                {
                    yield return r;
                }

                break;

            case ReportRecipientRule.Manager:
                if (slice.ManagerEmployeeId.HasValue)
                {
                    foreach (var r in ToRecipients(new[] { slice.ManagerEmployeeId.Value }, ReportRecipientRule.Manager))
                    {
                        yield return r;
                    }
                }

                break;
        }
    }

    /// <summary>
    /// "Everyone holding role R", optionally narrowed to the slice's own org unit.
    ///
    /// The narrowing is what makes "the Department Secretary" mean the secretary OF THIS DEPARTMENT.
    /// Without it the rule would mail every department's slice to every department's secretary, which is
    /// both noise and a data leak.
    /// </summary>
    private IEnumerable<ReportSliceRecipient> ResolveRoleScoped(ReportScheduleRecipient rule, ReportBurstSlice slice)
    {
        if (!rule.RoleId.HasValue)
        {
            return Enumerable.Empty<ReportSliceRecipient>();
        }

        if (rule.ScopeToBurstUnit && slice.OrganizationUnitId.HasValue)
        {
            var employeeIds = _sysUserOrgUnitDataRepository.GetAll()
                .Where(r => r.RoleId == rule.RoleId.Value
                            && r.OrganizationUnitId == slice.OrganizationUnitId.Value
                            && r.EmployeeId.HasValue)
                .Select(r => r.EmployeeId.Value)
                .Distinct()
                .ToList();

            return ToRecipients(employeeIds, ReportRecipientRule.RoleScoped);
        }

        // Tenant-wide: every user holding the role. This is the right behaviour for a central team --
        // the whole payroll team receiving every slice -- and the wrong one for a per-unit role, which
        // is why ScopeToBurstUnit exists and is set on those rules.
        var userIds = _userRoleRepository.GetAll()
            .Where(ur => ur.RoleId == rule.RoleId.Value)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToList();

        return ToRecipientsFromUsers(userIds, ReportRecipientRule.RoleScoped);
    }

    /// <summary>
    /// The head of the slice's org unit.
    ///
    /// HCMv3 has no head/manager column on the org unit, so the head is whoever is mapped to that unit
    /// in <see cref="SysUserOrgUnitData"/>. That table is the only place the "who is responsible for
    /// this unit" relationship exists.
    /// </summary>
    private IEnumerable<ReportSliceRecipient> ResolveOrgUnitHead(ReportBurstSlice slice)
    {
        if (!slice.OrganizationUnitId.HasValue)
        {
            return Enumerable.Empty<ReportSliceRecipient>();
        }

        var employeeIds = _sysUserOrgUnitDataRepository.GetAll()
            .Where(r => r.OrganizationUnitId == slice.OrganizationUnitId.Value && r.EmployeeId.HasValue)
            .Select(r => r.EmployeeId.Value)
            .Distinct()
            .ToList();

        return ToRecipients(employeeIds, ReportRecipientRule.OrgUnitHead);
    }

    private List<ReportSliceRecipient> ToRecipients(IReadOnlyCollection<int> employeeIds, ReportRecipientRule rule)
    {
        if (employeeIds == null || employeeIds.Count == 0)
        {
            return new List<ReportSliceRecipient>();
        }

        return _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.UserId, e.Email, e.FullNameEn, e.Code })
            .ToList()
            .Select(e => new ReportSliceRecipient
            {
                EmployeeId = e.Id,
                UserId = e.UserId,
                Email = e.Email,
                DisplayName = string.IsNullOrWhiteSpace(e.FullNameEn) ? e.Code : e.FullNameEn,
                ApplyRecipientScope = true,
                ResolvedByRule = rule,
            })
            .ToList();
    }

    private List<ReportSliceRecipient> ToRecipientsFromUsers(IReadOnlyCollection<long> userIds, ReportRecipientRule rule)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return new List<ReportSliceRecipient>();
        }

        var users = _userRepository.GetAll()
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.Id, u.EmailAddress, u.Name, u.Surname })
            .ToList();

        var employeeByUser = _employeeRepository.GetAll()
            .Where(e => e.UserId.HasValue && userIds.Contains(e.UserId.Value))
            .Select(e => new { e.Id, e.UserId })
            .ToList()
            .ToDictionary(e => e.UserId.Value, e => e.Id);

        return users
            .Select(u => new ReportSliceRecipient
            {
                UserId = u.Id,
                EmployeeId = employeeByUser.TryGetValue(u.Id, out var empId) ? empId : (int?)null,
                Email = u.EmailAddress,
                DisplayName = $"{u.Name} {u.Surname}".Trim(),
                ApplyRecipientScope = true,
                ResolvedByRule = rule,
            })
            .ToList();
    }
}
