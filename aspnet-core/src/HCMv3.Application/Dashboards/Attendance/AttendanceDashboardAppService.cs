using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.Dashboards.Attendance.Dto;
using HCMv3.Dashboards.Dto;
using HCMv3.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Dashboards.Attendance;

/// <inheritdoc cref="IAttendanceDashboardAppService"/>
/// <remarks>
/// <para><b>Legacy sources.</b> ESS <c>DashboardController.TimeAttendance</c> / <c>RawTimeAttendance</c> /
/// <c>GetTimeAttendanceGrid</c> / <c>Skip</c>: absent, late-in and early-out counts, the worst employees, department
/// shares, the day grid and the skip-punch grid. Kept: those figures. Changed: only the stored Time Engine results
/// are read -- legacy's "raw" mode re-ran the engine's calculations in memory on every view, which could disagree
/// with payroll; and the department drill-downs that wrote into the wrong series and ordered early-outs by
/// late-in minutes are not reproduced. Absence follows the tenant's approval setting exactly as legacy and the
/// payroll engine do (approved absence when time-register approval is on).</para>
///
/// <para><b>Punch requests</b> (legacy "skip punch") are punches asked for through self service: in HCMv3 a
/// <c>TimeEvent</c> carrying a workflow request, counted whatever its status.</para>
///
/// <para>Queries are synchronous <c>ToList()</c> over <c>GetAll()</c>, as in the other dashboards, so the service can
/// be tested over plain lists.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Dashboards_Attendance)]
public class AttendanceDashboardAppService : HCMv3AppServiceBase, IAttendanceDashboardAppService
{
    /// <summary>A day's register holds a year-1 date where it has no punch.</summary>
    private const int NoPunchYearCeiling = 1900;

    /// <summary>Attendance is one row per employee per day, so the period is capped harder than elsewhere.</summary>
    private const int MaxPeriodYears = 2;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly AttendanceDashboardExcelExporter _excelExporter;

    public AttendanceDashboardAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<TransType> transTypeRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IDashboardScopeService dashboardScopeService,
        AttendanceDashboardExcelExporter excelExporter)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _timeEventRepository = timeEventRepository;
        _transTypeRepository = transTypeRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _dashboardScopeService = dashboardScopeService;
        _excelExporter = excelExporter;
    }

    [HttpPost]
    public async Task<AttendanceDashboardDto> GetDashboard(AttendanceDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        return BuildDashboard(context, input);
    }

    /// <summary>The days behind a figure, a bar or a clicked day; newest first.</summary>
    [HttpPost]
    public async Task<PagedResultDto<AttendanceExceptionRowDto>> GetExceptions(GetAttendanceExceptionsInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = ExceptionRows(context, input.Kind, input.Day);
        return new PagedResultDto<AttendanceExceptionRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    [HttpPost]
    public async Task<PagedResultDto<AttendancePunchRequestRowDto>> GetPunchRequests(GetAttendancePunchRequestsInput input)
    {
        var context = await BuildContextAsync(input);
        var rows = PunchRequestRows(context);
        return new PagedResultDto<AttendancePunchRequestRowDto>(rows.Count, rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportAttendanceDashboardInput input)
    {
        var context = await BuildContextAsync(input);
        var all = input.Part == AttendanceDashboardExportPart.All;

        return _excelExporter.Export(new AttendanceDashboardExportData
        {
            Part = input.Part,
            Dashboard = BuildDashboard(context, input),
            Exceptions = all || input.Part == AttendanceDashboardExportPart.Exceptions
                ? ExceptionRows(context, all ? AttendanceExceptionKind.Any : input.Kind, all ? null : input.Day)
                : new List<AttendanceExceptionRowDto>(),
            PunchRequests = all || input.Part == AttendanceDashboardExportPart.PunchRequests
                ? PunchRequestRows(context)
                : new List<AttendancePunchRequestRowDto>(),
            ScopeLabel = ScopeLabel(context.Scope),
            UseArabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
        });
    }

    // ---------------------------------------------------------------- building

    private AttendanceDashboardDto BuildDashboard(DashboardContext context, AttendanceDashboardInput input)
    {
        var calculator = context.Calculator;
        var days = context.Days;
        var kpis = calculator.Kpis(days);
        kpis.PunchRequests = context.PunchRequests.Count;
        kpis.PunchRequestsPending = context.PunchRequests.Count(p => p.Status == RequestStatus.Open);

        var selected = input.EmployeeId.HasValue
            ? context.ScopedEmployees.FirstOrDefault(e => e.Id == input.EmployeeId.Value)
            : null;

        return new AttendanceDashboardDto
        {
            Scope = new DashboardScopeDto
            {
                Reason = context.Scope.Reason,
                OrgUnitNames = context.Scope.OrgUnitNames.ToList(),
                IncludesOwnTeam = context.Scope.IncludesOwnTeam,
                EmployeesInScope = context.EmployeesInScope
            },
            FromDate = context.From,
            ToDate = context.To,
            EmployeeNameEn = selected?.NameEn,
            EmployeeNameAr = selected?.NameAr,
            Kpis = kpis,
            Granularity = calculator.Granularity,
            Trend = calculator.Trend(days),
            ByOrgUnit = calculator.ByOrgUnit(days, input.OrganizationUnitId),
            TopAbsent = calculator.TopAbsent(days),
            TopLate = calculator.TopLate(days),
            TopEarly = calculator.TopEarly(days),
            Weekdays = AttendanceDashboardCalculator.Weekdays(days),
            OrgUnitPath = context.Tree.PathTo(input.OrganizationUnitId),
            OrgUnitOptions = context.Tree.OptionsFor(context.ScopedEmployees.Select(e => e.OrganizationUnitId))
        };
    }

    private static List<AttendanceExceptionRowDto> ExceptionRows(DashboardContext context, AttendanceExceptionKind kind, DateTime? day) =>
        context.Days
            .Where(d => (!day.HasValue || d.DayDate.Date == day.Value.Date) && context.Calculator.Matches(d, kind))
            .Select(context.Calculator.ToRow)
            .OrderByDescending(r => r.DayDate)
            .ThenBy(r => r.EmployeeCode)
            .ToList();

    private static List<AttendancePunchRequestRowDto> PunchRequestRows(DashboardContext context) =>
        context.PunchRequests.OrderByDescending(p => p.PunchDate).ThenByDescending(p => p.PunchTime).ToList();

    private string ScopeLabel(DashboardScope scope)
    {
        switch (scope.Reason)
        {
            case DashboardScopeReason.Admin:
                return L("Dashboard_ScopeAllAdmin");
            case DashboardScopeReason.Nobody:
                return L("Dashboard_ScopeNobody");
        }

        var parts = new List<string>(scope.OrgUnitNames);
        if (scope.IncludesOwnTeam)
        {
            parts.Add(L("Dashboard_ScopeYourTeam"));
        }

        return string.Join(" + ", parts);
    }

    // ---------------------------------------------------------------- context

    private sealed class DashboardContext
    {
        public DashboardScope Scope { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
        public DashboardOrgTree Tree { get; init; }
        public AttendanceDashboardCalculator Calculator { get; init; }
        public List<AttendanceEmployee> ScopedEmployees { get; init; }
        public int EmployeesInScope { get; init; }

        /// <summary>After the unit and employee filters: what every tile counts.</summary>
        public List<AttendanceDay> Days { get; init; }

        public List<AttendancePunchRequestRowDto> PunchRequests { get; init; }
    }

    private async Task<DashboardContext> BuildContextAsync(AttendanceDashboardInput input)
    {
        var today = Clock.Now.Date;
        var (from, to) = ResolvePeriod(input, today);

        var scope = await _dashboardScopeService.GetScopeAsync();
        var scopeIds = scope.IsEveryone ? null : scope.EmployeeIds.ToList();

        var employeeQuery = _employeeRepository.GetAll();
        var assignmentQuery = _orgAssignmentRepository.GetAll();
        if (scopeIds != null)
        {
            employeeQuery = employeeQuery.Where(e => scopeIds.Contains(e.Id));
            assignmentQuery = assignmentQuery.Where(a => scopeIds.Contains(a.EmployeeId));
        }

        // Department = the assignment in force at the end of the period (latest already started).
        var unitByEmployee = assignmentQuery
            .Select(a => new { a.EmployeeId, a.OrganizationUnitId, a.StartDate })
            .ToList()
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartDate <= to).ThenByDescending(a => a.StartDate).First().OrganizationUnitId);

        var employeeRows = employeeQuery
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status })
            .ToList();

        var scopedEmployees = employeeRows
            .Select(e => new AttendanceEmployee(e.Id, e.Code, e.FullNameEn, e.FullNameAr,
                unitByEmployee.TryGetValue(e.Id, out var unit) ? unit : null))
            .ToList();

        var tree = new DashboardOrgTree(_organizationUnitRepository.GetAll()
            .Select(u => new DashboardOrgUnit(u.Id, u.ParentId, u.DisplayName))
            .ToList());

        IEnumerable<AttendanceEmployee> filtered = scopedEmployees;
        if (input.OrganizationUnitId.HasValue)
        {
            var units = tree.SelfAndDescendants(input.OrganizationUnitId.Value);
            filtered = filtered.Where(e => e.OrganizationUnitId.HasValue && units.Contains(e.OrganizationUnitId.Value));
        }

        if (input.EmployeeId.HasValue)
        {
            filtered = filtered.Where(e => e.Id == input.EmployeeId.Value);
        }

        var filteredEmployees = filtered.ToList();
        var filteredIds = filteredEmployees.Select(e => e.Id).ToList();
        var filterById = scopeIds != null || input.OrganizationUnitId.HasValue || input.EmployeeId.HasValue;

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var useApprovedAbsence = timeSettings?.EnableTimeRegisterApproval ?? false;

        var registerQuery = _timeRegisterRepository.GetAll().Where(r => r.DayDate >= from && r.DayDate <= to);
        if (filterById)
        {
            registerQuery = registerQuery.Where(r => filteredIds.Contains(r.EmployeeId));
        }

        var days = registerQuery
            .Select(r => new
            {
                r.EmployeeId, r.DayDate, r.PlannedWorkingDay, r.LeaveDay, r.PublicHoliday, r.WeeklyOff, r.RestDay,
                r.AbsentDay, r.ApprovedAbsentDay, r.LateIn, r.EarlyOut, r.ShiftStartTime1, r.ShiftEndTime1,
                r.ActualIn, r.ActualOut, r.PlannedWorkingMins, r.ActualWorkingMins
            })
            .ToList()
            .Select(r => new AttendanceDay(
                r.EmployeeId,
                r.DayDate,
                r.PlannedWorkingDay > 0,
                r.LeaveDay > 0,
                r.PublicHoliday > 0 || r.WeeklyOff > 0 || r.RestDay > 0,
                (useApprovedAbsence ? r.ApprovedAbsentDay : r.AbsentDay) > 0,
                r.LateIn,
                r.EarlyOut,
                AsPunch(r.ShiftStartTime1),
                AsPunch(r.ShiftEndTime1),
                AsPunch(r.ActualIn),
                AsPunch(r.ActualOut),
                r.PlannedWorkingMins,
                r.ActualWorkingMins))
            .ToList();

        var punchRequests = LoadPunchRequests(from, to, filterById ? filteredIds : null, scopedEmployees);

        return new DashboardContext
        {
            Scope = scope,
            From = from,
            To = to,
            Tree = tree,
            Calculator = new AttendanceDashboardCalculator(from, to, today, tree, scopedEmployees),
            ScopedEmployees = scopedEmployees,
            EmployeesInScope = employeeRows.Count(e => e.Status == EmployeeStatus.Active),
            Days = days,
            PunchRequests = punchRequests
        };
    }

    /// <summary>
    /// Punches asked for through self service, whatever their approval status -- so the ApprovedOnly filter, which
    /// hides pending ones from the Time Engine, is lifted for this one read.
    /// </summary>
    private List<AttendancePunchRequestRowDto> LoadPunchRequests(
        DateTime from, DateTime to, List<int> employeeIds, List<AttendanceEmployee> employees)
    {
        var types = _transTypeRepository.GetAll()
            .Select(t => new { t.Id, t.NameEn, t.NameAr })
            .ToList()
            .ToDictionary(t => t.Id);
        var people = employees.ToDictionary(e => e.Id);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _timeEventRepository.GetAll()
                .Where(t => t.WorkflowRequestId != null && t.TransDate >= from && t.TransDate <= to);
            if (employeeIds != null)
            {
                query = query.Where(t => employeeIds.Contains(t.EmployeeId));
            }

            return query
                .Select(t => new { t.EmployeeId, t.TransDate, t.TransTime, t.TransTypeId, t.WorkflowStatus })
                .ToList()
                .Where(t => people.ContainsKey(t.EmployeeId))
                .Select(t =>
                {
                    var person = people[t.EmployeeId];
                    types.TryGetValue(t.TransTypeId, out var type);
                    return new AttendancePunchRequestRowDto
                    {
                        EmployeeId = t.EmployeeId,
                        EmployeeCode = person.Code,
                        EmployeeNameEn = person.NameEn,
                        EmployeeNameAr = person.NameAr,
                        PunchDate = t.TransDate,
                        PunchTime = t.TransTime,
                        PunchTypeNameEn = type?.NameEn,
                        PunchTypeNameAr = type?.NameAr,
                        Status = t.WorkflowStatus
                    };
                })
                .ToList();
        }
    }

    private static DateTime? AsPunch(DateTime value) => value.Year <= NoPunchYearCeiling ? null : value;

    private static (DateTime From, DateTime To) ResolvePeriod(AttendanceDashboardInput input, DateTime today)
    {
        var from = input.FromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        var to = input.ToDate?.Date ?? today;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        var earliest = to.AddYears(-MaxPeriodYears).AddDays(1);
        return (from < earliest ? earliest : from, to);
    }
}
