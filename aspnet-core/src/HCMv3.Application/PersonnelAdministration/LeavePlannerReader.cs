using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Organizations;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Reads what the Leave Planner shows for a set of employees -- shared by HR's planner and the self-service
/// team scheduler, so "approved", "pending" and "which unit" cannot mean different things on the two screens.
/// Who the employees are is the caller's decision; this only reads and lays out.
///
/// <para>
/// <b>What counts as what.</b> Approved = effective by the <c>ApprovedOnly</c> rule (no workflow status,
/// Approved or Closed). Pending = Open, i.e. with an approver. Drafts are the employee's own unfinished work and
/// never appear; rejected, withdrawn and cancelled rows are not anybody's plan any more. Legacy's "unapproved"
/// was simply <c>RequestStatus != Approved</c>, which also swept in rejected requests -- showing a refused
/// leave as somebody being away was not kept.
/// </para>
///
/// <para>
/// Hourly absences are left out, as on the leave dashboard: they are short permissions, not days away.
/// </para>
/// </summary>
public class LeavePlannerReader : ITransientDependency
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<LeavePlan> _leavePlanRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LeavePlannerReader(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<LeavePlan> leavePlanRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _leavePlanRepository = leavePlanRepository;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <param name="input">The month, the unit and text filters, and legacy's four switches.</param>
    /// <param name="scopeEmployeeIds">The employees the caller may show; null for everybody. Empty shows nobody.</param>
    /// <param name="today">For "the unit they sit in today" and the default month.</param>
    /// <param name="unassignedName">The group heading for employees in no unit.</param>
    public LeavePlannerDto Read(
        GetLeavePlannerInput input,
        IReadOnlyCollection<int> scopeEmployeeIds,
        DateTime today,
        string unassignedName)
    {
        input ??= new GetLeavePlannerInput();

        var (from, to) = LeavePlannerBuilder.MonthOf((input.Month ?? today).Date);

        var scopeIds = scopeEmployeeIds?.ToList();

        // ---- the in-scope employees and the unit each sits in today (the leave dashboard's rule)
        var employeeQuery = _employeeRepository.GetAll();
        if (scopeIds != null)
        {
            employeeQuery = employeeQuery.Where(e => scopeIds.Contains(e.Id));
        }

        var employeeRows = employeeQuery
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr })
            .ToList();

        var assignmentQuery = _orgAssignmentRepository.GetAll();
        if (scopeIds != null)
        {
            assignmentQuery = assignmentQuery.Where(a => scopeIds.Contains(a.EmployeeId));
        }

        var unitByEmployee = assignmentQuery
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId, a.StartDate })
            .ToList()
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(a => a.StartDate <= today).ThenByDescending(a => a.StartDate).First().OrganizationUnitId);

        var employees = employeeRows
            .Select(e => new LeavePlannerEmployee(
                e.Id,
                e.Code,
                e.FullNameEn,
                e.FullNameAr,
                unitByEmployee.TryGetValue(e.Id, out var unitId) ? unitId : null))
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        // Offered from the whole scope, before the filters below narrow it, so choosing one unit does not empty
        // the list of the others.
        var orgUnitOptions = tree.OptionsFor(employees.Select(e => e.OrganizationUnitId));

        // ---- the filters
        var filtered = employees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = tree.SelfAndDescendants(input.OrganizationUnitId.Value);
            filtered = filtered.Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value)).ToList();
        }

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var text = input.Filter.Trim();
            filtered = filtered
                .Where(e => Contains(e.Code, text) || Contains(e.FullNameEn, text) || Contains(e.FullNameAr, text))
                .ToList();
        }

        // Null = no restriction worth sending to the database.
        var idFilter = scopeIds == null && !input.OrganizationUnitId.HasValue && input.Filter.IsNullOrWhiteSpace()
            ? null
            : filtered.Select(e => e.Id).ToList();

        var facts = new List<LeavePlannerFact>();

        if (idFilter == null || idFilter.Count > 0)
        {
            facts.AddRange(ReadPlans(input, idFilter, from, to));
            facts.AddRange(ReadLeave(input, idFilter, from, to));
        }

        var dto = LeavePlannerBuilder.Build(from, to, filtered, facts, tree, unassignedName);
        dto.OrgUnitOptions = orgUnitOptions;

        // The caller says WHY these employees; this only knows how many there are.
        dto.Scope = new DashboardScopeDto { EmployeesInScope = employees.Count };

        return dto;
    }

    private IEnumerable<LeavePlannerFact> ReadPlans(GetLeavePlannerInput input, List<int> idFilter, DateTime from, DateTime to)
    {
        if (!input.IncludePlanned && !input.IncludePlannedPending)
        {
            return Array.Empty<LeavePlannerFact>();
        }

        // Pending plans are exactly what the ApprovedOnly filter hides, so it is off for the read and the
        // statuses are chosen here instead.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _leavePlanRepository.GetAll()
                .Where(p => p.StartDate <= to && p.EndDate >= from);

            if (idFilter != null)
            {
                query = query.Where(p => idFilter.Contains(p.EmployeeId));
            }

            var rows = query
                .Select(p => new { p.Id, p.EmployeeId, p.StartDate, p.EndDate, p.WorkflowStatus })
                .ToList();

            return rows
                .Select(p => (Row: p, Kind: KindOf(p.WorkflowStatus, LeavePlannerEntryKind.PlannedApproved, LeavePlannerEntryKind.PlannedPending)))
                .Where(p => p.Kind.HasValue
                            && (p.Kind == LeavePlannerEntryKind.PlannedApproved ? input.IncludePlanned : input.IncludePlannedPending))
                .Select(p => new LeavePlannerFact(p.Row.EmployeeId, p.Kind.Value, p.Row.Id, p.Row.StartDate, p.Row.EndDate))
                .ToList();
        }
    }

    private IEnumerable<LeavePlannerFact> ReadLeave(GetLeavePlannerInput input, List<int> idFilter, DateTime from, DateTime to)
    {
        if (!input.IncludeLeave && !input.IncludeLeavePending)
        {
            return Array.Empty<LeavePlannerFact>();
        }

        var types = _absenceTypeRepository.GetAll()
            .Select(t => new { t.Id, t.NameEn, t.NameAr })
            .ToList()
            .ToDictionary(t => t.Id);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _absenceRepository.GetAll()
                .Where(a => a.AbsenceHours == 0 && a.StartDate <= to && a.EndDate >= from);

            if (idFilter != null)
            {
                query = query.Where(a => idFilter.Contains(a.EmployeeId));
            }

            var rows = query
                .Select(a => new { a.Id, a.EmployeeId, a.AbsenceTypeId, a.StartDate, a.EndDate, a.WorkflowStatus })
                .ToList();

            return rows
                .Select(a => (Row: a, Kind: KindOf(a.WorkflowStatus, LeavePlannerEntryKind.LeaveApproved, LeavePlannerEntryKind.LeavePending)))
                .Where(a => a.Kind.HasValue
                            && (a.Kind == LeavePlannerEntryKind.LeaveApproved ? input.IncludeLeave : input.IncludeLeavePending))
                .Select(a =>
                {
                    types.TryGetValue(a.Row.AbsenceTypeId, out var type);
                    return new LeavePlannerFact(a.Row.EmployeeId, a.Kind.Value, a.Row.Id, a.Row.StartDate, a.Row.EndDate, type?.NameEn, type?.NameAr);
                })
                .ToList();
        }
    }

    /// <summary>Approved by the ApprovedOnly rule, pending while Open, and nothing at all otherwise.</summary>
    private static LeavePlannerEntryKind? KindOf(RequestStatus? status, LeavePlannerEntryKind approved, LeavePlannerEntryKind pending)
    {
        if (status == null || status == RequestStatus.Approved || status == RequestStatus.Closed)
        {
            return approved;
        }

        return status == RequestStatus.Open ? pending : null;
    }

    private static bool Contains(string value, string text) =>
        value != null && value.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
}
