using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>One employee the Time Engine screen's employee picker offers, and whether payroll has already run for it.</summary>
public class TimeEngineRunCandidate
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>
    /// A regular salary register exists for the payroll area's current period (legacy "Processed"): the month is
    /// already paid, so re-running its time changes what payroll will see as a retro difference.
    /// </summary>
    public bool IsProcessed { get; set; }
}

/// <summary>
/// Who the Time Engine screen's employee picker lists. Port of legacy <c>uscTimeEngine.BuildTimeEmployeePopupList</c>
/// (<c>HCM10.Web/UserControls/PayrollManagement/uscTimeEngine.ascx.cs</c> lines 581-650).
///
/// <para>This only GUIDES the operator: it decides what the picker shows, not what the engine runs. The engine
/// still selects by the codes it is sent (<c>TimeEngineService.GetEmployeeListAsync</c>).</para>
/// </summary>
public interface ITimeEngineRunCandidateSelector : IDomainService
{
    /// <param name="payrollAreaName">Matched against <c>PayrollArea.Name</c>, as everywhere else on this screen.</param>
    /// <param name="startDate">The Start Date on the screen, or null (legacy: today).</param>
    /// <param name="endDate">The End Date on the screen, or null (legacy: the start date).</param>
    /// <param name="today">The current date; a parameter so the rule is testable.</param>
    Task<List<TimeEngineRunCandidate>> GetCandidatesAsync(
        string payrollAreaName,
        DateTime? startDate,
        DateTime? endDate,
        DateTime today);

    /// <summary>Legacy <c>GetTimeEmployeePopupPeriodText</c>'s window: the start date or today, the end date or the start.</summary>
    (DateTime Start, DateTime End) ResolveWindow(DateTime? startDate, DateTime? endDate, DateTime today);
}

/// <inheritdoc cref="ITimeEngineRunCandidateSelector"/>
/// <remarks>
/// <para><b>Legacy rules reproduced:</b> nothing without a payroll area (line 590); employees whose org assignment in
/// that area OVERLAPS the window -- not "covers a date" (lines 601-614); one row per code, ordered by code; no status
/// or joining-date filter (legacy had none, unlike the payroll picker); Processed = a Regular salary register with
/// <c>ForPeriod == InPeriod == </c> the area's control record <c>CalendarMonthEnd</c> (lines 622-634), so an area
/// without a control record lists everyone as Pending.</para>
///
/// <para><b>The area match</b> uses the assignment's own <c>PayrollAreaId</c> and falls back to its org unit's
/// profile for rows that predate that column -- the same two paths as <c>IEmployeePayrollAreaResolver</c>, which this
/// cannot call directly because the resolver answers "as of a date" and legacy asks "at any time in the window".</para>
///
/// <para>Synchronous LINQ over <c>IRepository.GetAll()</c>, wrapped in a task, like
/// <c>PayrollRunCandidateSelector</c> (Core has no EF Core reference).</para>
/// </remarks>
[UnitOfWork]
public class TimeEngineRunCandidateSelector : HCMv3DomainServiceBase, ITimeEngineRunCandidateSelector
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;

    public TimeEngineRunCandidateSelector(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<SalaryRegister> salaryRegisterRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
    }

    public (DateTime Start, DateTime End) ResolveWindow(DateTime? startDate, DateTime? endDate, DateTime today)
    {
        // Legacy lines 593-594 (and 964-965 for the header text).
        var start = startDate.HasValue && startDate.Value != default(DateTime) ? startDate.Value.Date : today.Date;
        var end = endDate.HasValue && endDate.Value != default(DateTime) ? endDate.Value.Date : start;
        return (start, end);
    }

    public Task<List<TimeEngineRunCandidate>> GetCandidatesAsync(
        string payrollAreaName,
        DateTime? startDate,
        DateTime? endDate,
        DateTime today)
    {
        var none = new List<TimeEngineRunCandidate>();

        // Legacy line 590: no payroll area, no list.
        if (string.IsNullOrWhiteSpace(payrollAreaName))
        {
            return Task.FromResult(none);
        }

        var (start, end) = ResolveWindow(startDate, endDate, today);

        // Legacy lines 596-603: the area by name, the first one found.
        var payrollAreaId = _payrollAreaRepository.GetAll()
            .Where(a => a.Name == payrollAreaName)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .FirstOrDefault();

        if (payrollAreaId == 0)
        {
            return Task.FromResult(none);
        }

        // Legacy lines 605-608: an assignment in this area that overlaps the window.
        var employeeIds = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.PayrollAreaId == payrollAreaId && a.StartDate <= end && a.EndDate >= start)
            .Select(a => a.EmployeeId)
            .ToList();

        // Fallback for assignments that predate EmployeeOrgAssignment.PayrollAreaId (see remarks).
        var organizationUnitIds = _orgUnitProfileRepository.GetAll()
            .Where(p => p.PayrollAreaId == payrollAreaId)
            .Select(p => p.OrganizationUnitId)
            .ToList();

        if (organizationUnitIds.Any())
        {
            employeeIds.AddRange(_employeeOrgAssignmentRepository.GetAll()
                .Where(a => a.PayrollAreaId == null
                            && a.OrganizationUnitId != null
                            && organizationUnitIds.Contains(a.OrganizationUnitId.Value)
                            && a.StartDate <= end
                            && a.EndDate >= start)
                .Select(a => a.EmployeeId)
                .ToList());
        }

        employeeIds = employeeIds.Distinct().ToList();
        if (!employeeIds.Any())
        {
            return Task.FromResult(none);
        }

        // Legacy lines 609-616: one row per code, by code.
        var employees = _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr })
            .ToList()
            .GroupBy(e => e.Code)
            .Select(g => g.First())
            .OrderBy(e => e.Code, StringComparer.Ordinal)
            .ToList();

        // Legacy lines 622-625: the area's control record month end; none found leaves it unset.
        var payrollDate = _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollAreaId == payrollAreaId)
            .OrderBy(r => r.Id)
            .Select(r => r.CalendarMonthEnd)
            .FirstOrDefault();

        var ids = employees.Select(e => e.Id).ToList();

        // Legacy lines 629-634: a Regular register for that very period.
        var processed = payrollDate == default(DateTime)
            ? new HashSet<int>()
            : new HashSet<int>(_salaryRegisterRepository.GetAll()
                .Where(sr => ids.Contains(sr.EmployeeId)
                             && sr.PayrollRunType == PayrollRunType.Regular
                             && sr.ForPeriod == sr.InPeriod
                             && sr.ForPeriod == payrollDate)
                .Select(sr => sr.EmployeeId)
                .ToList());

        return Task.FromResult(employees
            .Select(e => new TimeEngineRunCandidate
            {
                EmployeeId = e.Id,
                EmployeeCode = e.Code,
                FullNameEn = e.FullNameEn,
                FullNameAr = e.FullNameAr,
                IsProcessed = processed.Contains(e.Id)
            })
            .ToList());
    }
}
