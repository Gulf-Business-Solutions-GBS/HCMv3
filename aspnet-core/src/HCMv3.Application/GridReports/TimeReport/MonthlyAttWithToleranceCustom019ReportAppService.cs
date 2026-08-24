using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Organizations;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Custom019 Monthly Attendance With Tolerance grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance_Custom019.ascx(.cs)</c>,
/// method <c>Table1(monthStartDate, monthEndDate, employeeCode)</c> (lines 675-1740).
///
/// WHY THIS IS A SEPARATE SERVICE AND NOT A SUBCLASS OF
/// <see cref="MonthlyAttWithToleranceReportAppService"/>. It keeps the base's expensive machinery -- the
/// per-day recomputation from raw punches, the duty resolution, the diagnostic rows -- but REPLACES THE
/// THING THE REPORT EXISTS TO COMPUTE. The base accumulates DA (<c>PlannedMins - WorkingMins</c>) into MA
/// and flags <c>MA &gt; MonthlyAfterTolerance</c>. Custom019 throws that model out and builds a deduction
/// from LATE-IN AND EARLY-OUT MINUTES directly, run through three validation passes, then compares the
/// month's totals against two NEW policy fields. Every column downstream of that changes name, meaning or
/// both, so a shared base would share only the parts that were never the point.
///
/// THE FIVE SUBSTANTIVE CHANGES:
/// <list type="number">
/// <item><description><b>Missed-punch penalty (lines 776-802).</b> A day with a punch-out but no punch-in
/// is charged a flat <c>120</c>-minute LATE IN (and its Late Out is zeroed); a day with a punch-in but no
/// punch-out is charged a flat <c>120</c>-minute EARLY OUT (and its Early In is zeroed). The base had no
/// such concept.</description></item>
/// <item><description><b>Tolerance is a blanking pass, not a netting pass (lines 758-768).</b> The base
/// kept the raw minutes in the CELL and netted a separate variable for the deduction; Custom019 BLANKS the
/// cell itself when the value is within tolerance -- unless it is a missed-punch penalty, which is never
/// forgiven.</description></item>
/// <item><description><b>Excluded leave types (lines 823-847).</b> A partial leave whose
/// <c>AbsenceType.Code</c> is the hard-coded <c>"4"</c> ("Personal Outpass") or <c>"23"</c> ("Business
/// Meeting Leave") reduces the day's late-in and/or early-out by the leave's own minutes, depending on
/// whether the leave straddles the shift start or the shift end.</description></item>
/// <item><description><b>Two monthly tolerances and a monthly row (lines 1607-1630).</b> The month's
/// late-in and early-out totals are each compared against <c>TimePolicy.MonthlyLateInTolerance</c> /
/// <c>MonthlyEarlyOutTolerance</c>, and an extra ROW per employee reports them. The base's single
/// <c>MonthlyAfterTolerance</c> is still read into a local and then never used -- the boolean column it
/// fed is gone.</description></item>
/// <item><description><b>A Detail/Summary variant and three org filters</b> -- see
/// <see cref="GetMonthlyAttWithToleranceCustom019ReportInput"/>.</description></item>
/// </list>
///
/// LEGACY DEFECT REPRODUCED -- LATE OUT IS DEAD. The two lines computing <c>lateOutTsMins</c> are commented
/// out (lines 631-632) but the write is not, so the cell is 0 whenever the employee punched out late and
/// blank otherwise, and staying late no longer adds to Working Mins. Pinned by a test.
///
/// CORRECTED DEFECT (inherited from the base port): legacy leaks the previous day's -- or previous
/// EMPLOYEE's -- shift bounds into a day whose duty cannot be resolved. That is cross-row corruption, so
/// as in the base port the shift bounds are reset per day.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the ColumnChooser JSON persistence,
/// the callback-panel state machine, the PDF export button, and the commented-out <c>Table1_Old</c>
/// (line 1741+) together with the four superseded "Old" regions kept inline in <c>Table1</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_MonthlyAttWithToleranceCustom019)]
public class MonthlyAttWithToleranceCustom019ReportAppService
    : GridReportAppServiceBase, IMonthlyAttWithToleranceCustom019ReportAppService
{
    private const string ClockInTransTypeCode = "1";

    private const string ClockOutTransTypeCode = "2";

    /// <summary>
    /// Legacy <c>defaultPaneltyDedMin</c> (line 261, spelling theirs): the flat penalty in minutes charged
    /// for a missed punch. A hardcoded local, not a policy setting.
    /// </summary>
    private const int DefaultPenaltyDeductionMinutes = 120;

    /// <summary>
    /// Legacy lines 825-827: the two <c>AbsenceType.Code</c> literals whose partial leave is allowed to
    /// reduce the day's late-in/early-out. Hardcoded there ("Personal Outpass" and "Business Meeting Leave
    /// (Hours)" per the comments in the superseded block at line 866), hardcoded here.
    /// </summary>
    private static readonly string[] ExcludedLeaveTypeCodes = { "4", "23" };

    /// <summary>Legacy line 1626: the marker that identifies a monthly deduction row.</summary>
    public const string MonthlyDeductionRemarks = "Monthly LateIn/EarlyOut Deduction";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly ITimeDataService _timeDataService;
    private readonly IRosterService _rosterService;

    public MonthlyAttWithToleranceCustom019ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        ITimeDataService timeDataService,
        IRosterService rosterService)
    {
        _employeeRepository = employeeRepository;
        _timeEventRepository = timeEventRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _timeDataService = timeDataService;
        _rosterService = rosterService;
    }

    protected override string LegacyScreenName => "uscRptMonthlyAttWithTolerance_Custom019";

    protected override string ScreenCaption => "Monthly Attendance With Tolerance (Custom 019)";

    [HttpPost]
    public async Task<PagedResultDto<MonthlyAttWithToleranceCustom019ReportRowDto>>
        GetMonthlyAttWithToleranceCustom019Report(GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildToleranceParameterString(input),
            totalCount);

        return new PagedResultDto<MonthlyAttWithToleranceCustom019ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetMonthlyAttWithToleranceCustom019ReportToExcel(
        GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "MonthlyAttWithToleranceCustom019.xlsx",
            rows,
            GetExportColumns(input.Variant),
            BuildToleranceParameterString(input));
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr()</c> (lines 645-670), which logs the whole wider panel. The
    /// payroll area is on that list in legacy but has no effect on the data, so there is nothing to log.
    /// </summary>
    private string BuildToleranceParameterString(GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        var parts = new List<string>
        {
            "Month : " + (input.Month ?? Today.Month),
            "Year : " + (input.Year ?? Today.Year),
            "Varient : " + input.Variant
        };

        if (!input.DepartmentCode.IsNullOrWhiteSpace())
        {
            parts.Add("Department : " + input.DepartmentCode);
        }

        if (!input.DivisionCode.IsNullOrWhiteSpace())
        {
            parts.Add("Division : " + input.DivisionCode);
        }

        if (!input.SectionCode.IsNullOrWhiteSpace())
        {
            parts.Add("Section : " + input.SectionCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Single source of truth for filtering AND row generation, shared by the paged read and the export.
    /// The result is in-memory because the rows do not exist in the database: they are computed.
    /// </summary>
    private async Task<IQueryable<MonthlyAttWithToleranceCustom019ReportRowDto>> BuildQueryAsync(
        GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        var rows = await BuildRowsAsync(input);

        // Legacy lines 1642-1670: the Summary variant keeps ONLY the monthly deduction rows. (It also
        // removes columns from the DataTable; here that is the export column set and the grid's own column
        // visibility -- see GetExportColumns.)
        if (input.Variant == MonthlyAttWithToleranceCustom019Variant.Summary)
        {
            rows = rows.Where(r => r.SystemRemarks == MonthlyDeductionRemarks).ToList();
        }

        return rows.AsQueryable();
    }

    private async Task<List<MonthlyAttWithToleranceCustom019ReportRowDto>> BuildRowsAsync(
        GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        var rows = new List<MonthlyAttWithToleranceCustom019ReportRowDto>();

        var (startDate, endDate) = ResolveMonthWindow(input);

        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var today = Today;

        // Legacy lines 707-736: three org filters, each matched against an org assignment covering TODAY.
        var orgUnitIds = await ResolveOrgFilterUnitIdsAsync(input);
        if (orgUnitIds != null && orgUnitIds.Count == 0)
        {
            // A code that matches no org unit selects no employee.
            return rows;
        }

        // Legacy lines 693-694: the base's mandatory-employee-code guard is COMMENTED OUT here, so a blank
        // code box means "every active employee" rather than "no rows".
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(orgUnitIds != null, e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnitId != null
                          && orgUnitIds.Contains(o.OrganizationUnitId.Value)))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => new EmployeeInfo
            {
                Id = e.Id,
                Code = e.Code,
                FullName = useArabicNames ? e.FullNameAr : e.FullNameEn
            })
            .OrderBy(e => e.Code)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return rows;
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        var context = await LoadContextAsync(employeeIds, startDate, endDate, useArabicNames);

        foreach (var employee in employees)
        {
            rows.AddRange(await BuildEmployeeRowsAsync(
                employee, startDate, endDate, context, isRosterActive));
        }

        return rows;
    }

    /// <summary>
    /// Ports legacy lines 592-611: Month/Year (defaulting to today's) become the first and last day of that
    /// calendar month. Legacy caught a bad Month/Year and fell back to <c>default(DateTime)</c>, then
    /// returned an EMPTY table (line 696); the DTO's <c>[Range]</c> attributes make that unreachable here,
    /// so neither the fallback nor the guard is carried.
    /// </summary>
    private (DateTime StartDate, DateTime EndDate) ResolveMonthWindow(
        GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        return (
            new DateTime(year, month, 1),
            new DateTime(year, month, DateTime.DaysInMonth(year, month)));
    }

    /// <summary>
    /// MODEL ADAPTATION for the three org filters. Legacy matched <c>OrgAssignment.OrgLevelN.Code</c>
    /// against the combo value, where OrgLevel2/3/4 were three of seven discrete FK columns. HCMv3 collapses
    /// all seven into ONE <c>OrganizationUnitId</c> on the real hierarchical
    /// <c>Abp.Organizations.OrganizationUnit</c> tree (see <see cref="EmployeeOrgLevelNameResolver"/>), so
    /// "the employee's OrgLevel2" is the ancestor-or-self of their assigned unit classified as Level2.
    ///
    /// The equivalent filter is therefore "the assigned unit is at or under the unit whose OrgUnitProfile
    /// carries this code at this level". Zero's <c>OrganizationUnit.Code</c> is a dot-separated path, so
    /// descendant-of is a string prefix; the whole (small) unit table is read once and the descendant id
    /// set computed in memory, which keeps the employee query a plain <c>Contains</c>.
    ///
    /// Returns NULL when no org filter was supplied (meaning "do not filter"), and an EMPTY set when a code
    /// was supplied that matches nothing.
    /// </summary>
    private async Task<HashSet<long>> ResolveOrgFilterUnitIdsAsync(
        GetMonthlyAttWithToleranceCustom019ReportInput input)
    {
        var filters = new List<(string LevelCode, string BusinessCode)>();

        if (!input.DepartmentCode.IsNullOrWhiteSpace())
        {
            filters.Add((OrganizationUnitType.Level2Code, input.DepartmentCode.Trim()));
        }

        if (!input.DivisionCode.IsNullOrWhiteSpace())
        {
            filters.Add((OrganizationUnitType.Level3Code, input.DivisionCode.Trim()));
        }

        if (!input.SectionCode.IsNullOrWhiteSpace())
        {
            filters.Add((OrganizationUnitType.Level4Code, input.SectionCode.Trim()));
        }

        if (filters.Count == 0)
        {
            return null;
        }

        var profiles = await _orgUnitProfileRepository.GetAll()
            .Select(p => new
            {
                p.Code,
                LevelCode = p.OrganizationUnitType.Code,
                UnitCode = p.OrganizationUnit.Code
            })
            .ToListAsync();

        var units = await _organizationUnitRepository.GetAll()
            .Select(u => new { u.Id, u.Code })
            .ToListAsync();

        // Legacy AND-ed the three filters (three successive Where clauses), so an employee must sit under
        // ALL of the supplied levels. Intersecting the descendant sets reproduces that.
        HashSet<long> result = null;

        foreach (var (levelCode, businessCode) in filters)
        {
            var roots = profiles
                .Where(p => p.LevelCode == levelCode
                            && p.Code == businessCode
                            && p.UnitCode != null)
                .Select(p => p.UnitCode)
                .ToList();

            var descendants = units
                .Where(u => u.Code != null && roots.Any(r => u.Code.StartsWith(r)))
                .Select(u => u.Id)
                .ToHashSet();

            if (result == null)
            {
                result = descendants;
            }
            else
            {
                result.IntersectWith(descendants);
            }
        }

        return result;
    }

    /// <summary>
    /// Ports legacy's "Load Initial Data" region (lines 745-856): every collection the day loop needs,
    /// fetched ONCE for the whole employee set and month.
    /// </summary>
    private async Task<MonthlyToleranceContext> LoadContextAsync(
        List<int> employeeIds,
        DateTime startDate,
        DateTime endDate,
        bool useArabicNames)
    {
        // The window runs to EndDate + 1 day so a night shift starting on the last day can still find its
        // clock-out punch.
        var timeEventWindowEnd = endDate.AddDays(1);
        var rawTimeEvents = await _timeEventRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.TransDate.Date >= startDate
                        && t.TransDate.Date <= timeEventWindowEnd)
            .Select(t => new
            {
                t.EmployeeId,
                TransTypeCode = t.TransType.Code,
                t.TransDate,
                t.TransTime
            })
            .ToListAsync();

        var timeEvents = rawTimeEvents
            .Select(t => new PunchEvent
            {
                EmployeeId = t.EmployeeId,
                TransTypeCode = t.TransTypeCode,
                TransDate = t.TransDate.Date,
                // CUSTOM019 KEEPS THE SECONDS. The base trimmed TransTime to whole minutes (base line 700);
                // Custom019 selects TransTime verbatim and builds TransDateTime with AddSeconds
                // (lines 175-177), so a punch at 08:00:30 is 30 seconds late here and exactly on time on
                // the base report.
                TransTime = t.TransTime.TimeOfDay,
                TransDateTime = t.TransDate.Date
                    .AddHours(t.TransTime.Hour)
                    .AddMinutes(t.TransTime.Minute)
                    .AddSeconds(t.TransTime.Second)
            })
            .ToList();

        var dutyPeriods = await _employeeDutyPeriodRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeId))
            .Include(d => d.DutyPeriod).ThenInclude(p => p.SundayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.MondayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.TuesdayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.WednesdayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.ThursdayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.FridayDuty)
            .Include(d => d.DutyPeriod).ThenInclude(p => p.SaturdayDuty)
            .ToListAsync();

        // Legacy lines 782-806. Two fields are NEW here: MonthlyLateInTolerance and
        // MonthlyEarlyOutTolerance (lines 803-804).
        var timePolicies = await _employeeTimePolicyRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .Select(p => new TimePolicyInfo
            {
                EmployeeId = p.EmployeeId,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                ClockInLimit = p.TimePolicy.ClockInLimit,
                ClockOutLimit = p.TimePolicy.ClockOutLimit,
                TimeEventRule = p.TimePolicy.TimeEventRule,
                TimeMgmtStatus = p.TimePolicy.TimeMgmtStatus,
                LateInTolerance = p.TimePolicy.LateInTolerance,
                EarlyOutTolerance = p.TimePolicy.EarlyOutTolerance,
                MonthlyLateInTolerance = p.TimePolicy.MonthlyLateInTolerance,
                MonthlyEarlyOutTolerance = p.TimePolicy.MonthlyEarlyOutTolerance
            })
            .ToListAsync();

        // Legacy lines 808-836. Three fields are NEW: AbsenceTypeCode, StartTime and EndTime, all three
        // feeding the excluded-leave adjustment.
        var absences = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.StartDate <= endDate
                        && a.EndDate >= startDate)
            .Select(a => new AbsenceInfo
            {
                EmployeeId = a.EmployeeId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                AbsenceTypeName = useArabicNames ? a.AbsenceType.NameAr : a.AbsenceType.NameEn,
                AbsenceTypeCode = a.AbsenceType.Code,
                IsPaid = a.AbsenceType.IsPaid,
                HalfdayType = a.HalfdayType,
                AbsenceHours = a.AbsenceHours,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            })
            .ToListAsync();

        var substituteDuties = await _employeeSubstituteDutyRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId)
                        && s.SubstituteDate.Date >= startDate
                        && s.SubstituteDate.Date <= endDate)
            .Include(s => s.Duty)
            .ToListAsync();

        return new MonthlyToleranceContext
        {
            TimeEvents = timeEvents,
            DutyPeriods = dutyPeriods,
            TimePolicies = timePolicies,
            Absences = absences,
            SubstituteDuties = substituteDuties
        };
    }

    /// <summary>
    /// Ports the per-employee body of the day loop (lines 858-1637), including the three running totals and
    /// the monthly deduction row.
    /// </summary>
    private async Task<List<MonthlyAttWithToleranceCustom019ReportRowDto>> BuildEmployeeRowsAsync(
        EmployeeInfo employee,
        DateTime startDate,
        DateTime endDate,
        MonthlyToleranceContext context,
        bool isRosterActive)
    {
        var rows = new List<MonthlyAttWithToleranceCustom019ReportRowDto>();

        // Legacy lines 260-289. NOTE the change from the base: the daily tolerances are read RAW, without
        // the base's CalcLateInAfterTolerance / CalcEarlyOutAfterTolerance gates -- turning those flags off
        // no longer disables the tolerance on this variant.
        var lateInTolerance = 0;
        var earlyOutTolerance = 0;
        var monthlyLateInTolerance = 0;
        var monthlyEarlyOutTolerance = 0;

        var policyForMonthlyTolerance = context.TimePolicies.FirstOrDefault(
            p => p.EmployeeId == employee.Id && p.StartDate <= startDate && p.EndDate >= startDate);

        if (policyForMonthlyTolerance != null)
        {
            lateInTolerance = (int)policyForMonthlyTolerance.LateInTolerance;
            earlyOutTolerance = (int)policyForMonthlyTolerance.EarlyOutTolerance;
            monthlyLateInTolerance = (int)policyForMonthlyTolerance.MonthlyLateInTolerance;
            monthlyEarlyOutTolerance = (int)policyForMonthlyTolerance.MonthlyEarlyOutTolerance;
        }

        var runningDeductionMins = 0;
        var monthlyLateInDeduction = 0;
        var monthlyEarlyOutDeduction = 0;

        var publicHolidays = await _timeDataService.GetPublicHolidaysDatesListAsync(
            employee.Id, startDate, endDate);

        var employeeSubstituteDuties = context.SubstituteDuties
            .Where(s => s.EmployeeId == employee.Id)
            .ToList();

        for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
        {
            // Legacy lines 302-310: no time policy covering the day -> a diagnostic row carrying ONLY the
            // status cell, then skip the day. Note the `continue` also skips the monthly row below, so an
            // employee whose LAST day has no policy gets no monthly row at all. Reproduced.
            var dayPolicy = context.TimePolicies.FirstOrDefault(
                p => p.EmployeeId == employee.Id && p.StartDate <= dayDate && p.EndDate >= dayDate);

            if (dayPolicy == null)
            {
                rows.Add(new MonthlyAttWithToleranceCustom019ReportRowDto
                {
                    SystemRemarks = "Time policy not found on date : " + dayDate.ToString("dd/MM/yyyy")
                });
                continue;
            }

            var dutyPeriod = context.DutyPeriods
                .Where(d => d.EmployeeId == employee.Id && d.StartDate <= dayDate && d.EndDate >= dayDate)
                .Select(d => d.DutyPeriod)
                .FirstOrDefault();

            if (dutyPeriod == null)
            {
                rows.Add(new MonthlyAttWithToleranceCustom019ReportRowDto
                {
                    SystemRemarks = "Duty Period not found on date : " + dayDate.ToString("dd/MM/yyyy")
                });
                continue;
            }

            var currentDay = dayDate;

            var dayEvents = context.TimeEvents
                .Where(t => t.EmployeeId == employee.Id
                            && (t.TransDate == currentDay
                                || t.TransDate == currentDay.AddDays(1)
                                || t.TransDate == currentDay.AddDays(-1)))
                .ToList();

            var duty = isRosterActive
                ? await _rosterService.GetRosterDutyAsync(
                    employee.Id, dayDate, employeeSubstituteDuties, false)
                : employeeSubstituteDuties
                      .Where(s => s.SubstituteDate.Date == currentDay)
                      .Select(s => s.Duty)
                      .FirstOrDefault()
                  ?? GetDayOfWeekDuty(dutyPeriod, dayDate);

            rows.Add(BuildDayRow(
                employee,
                dayDate,
                duty,
                dayPolicy,
                dayEvents,
                context,
                publicHolidays,
                lateInTolerance,
                earlyOutTolerance,
                ref runningDeductionMins,
                ref monthlyLateInDeduction,
                ref monthlyEarlyOutDeduction));

            // Legacy lines 1605-1631: the extra monthly row, emitted on the LAST day of the month.
            if (dayDate == endDate)
            {
                // Each month's total counts only when it EXCEEDS its own monthly tolerance; otherwise the
                // whole month is forgiven and the cell reads 0.
                var monthlyLateIn = monthlyLateInDeduction > monthlyLateInTolerance ? monthlyLateInDeduction : 0;
                var monthlyEarlyOut = monthlyEarlyOutDeduction > monthlyEarlyOutTolerance
                    ? monthlyEarlyOutDeduction
                    : 0;

                rows.Add(new MonthlyAttWithToleranceCustom019ReportRowDto
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.FullName,
                    LateIn = monthlyLateIn,
                    EarlyOut = monthlyEarlyOut,
                    DailyDeduction = monthlyLateIn + monthlyEarlyOut,
                    // Legacy line 1621 is the self-assignment `runningLateInEarlyoutMins =
                    // runningLateInEarlyoutMins;` -- the monthly deduction is deliberately NOT folded into
                    // the running total, which just repeats the last day's value. Reproduced.
                    RunningDeduction = runningDeductionMins,
                    SystemRemarks = MonthlyDeductionRemarks
                });
            }
        }

        return rows;
    }

    private static Duty GetDayOfWeekDuty(DutyPeriod dutyPeriod, DateTime dayDate)
    {
        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDuty,
            DayOfWeek.Monday => dutyPeriod.MondayDuty,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDuty,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDuty,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDuty,
            DayOfWeek.Friday => dutyPeriod.FridayDuty,
            _ => dutyPeriod.SaturdayDuty
        };
    }

    /// <summary>
    /// Ports the body of one day (lines 400-1600): the full-day-leave branch and the normal attendance
    /// branch with its three validation passes.
    /// </summary>
    private MonthlyAttWithToleranceCustom019ReportRowDto BuildDayRow(
        EmployeeInfo employee,
        DateTime dayDate,
        Duty duty,
        TimePolicyInfo dayPolicy,
        List<PunchEvent> dayEvents,
        MonthlyToleranceContext context,
        List<(DateTime HolidayDate, string HolidayName)> publicHolidays,
        int lateInTolerance,
        int earlyOutTolerance,
        ref int runningDeductionMins,
        ref int monthlyLateInDeduction,
        ref int monthlyEarlyOutDeduction)
    {
        // CORRECTED DEFECT: reset per day -- see the class remarks.
        var shiftStart = duty?.StartTime ?? TimeSpan.Zero;
        var shiftEnd = duty?.EndTime ?? TimeSpan.Zero;
        var isNightShift = duty != null && duty.IsNightShift;
        var isWeekOff = duty != null && duty.IsWeeklyOff;
        var isRestDay = duty != null && duty.IsRestDay;

        var holidayIndex = publicHolidays.FindIndex(h => h.HolidayDate == dayDate);
        var isPublicHoliday = holidayIndex >= 0;
        var publicHolidayName = isPublicHoliday
            ? publicHolidays[holidayIndex].HolidayName ?? string.Empty
            : string.Empty;

        var row = new MonthlyAttWithToleranceCustom019ReportRowDto
        {
            // NEW IN CUSTOM019 (line 483): the Oid cell carries the EMPLOYEE id.
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            DayDate = dayDate,
            WeekDay = GetLocalizedWeekDayName(dayDate),
            ShiftStart = FormatShortTime(shiftStart),
            ShiftEnd = FormatShortTime(shiftEnd)
        };

        var fullDayLeave = context.Absences.FirstOrDefault(
            a => a.EmployeeId == employee.Id
                 && a.StartDate <= dayDate
                 && a.EndDate >= dayDate
                 && a.AbsenceHours == 0);

        if (fullDayLeave != null)
        {
            // Legacy lines 412-476: unchanged from the base except that the Deduction write is commented
            // out (line 465) and the status call lost its dailyDed argument.
            if (!isPublicHoliday)
            {
                row.PlannedMins = ComputePlannedMinutes(shiftStart, shiftEnd, isNightShift, false);
                row.WorkingMins = 0;
            }

            row.SystemRemarks = GetStatus(
                isWeekOff, isRestDay, isPublicHoliday, publicHolidayName, false, false,
                TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, fullDayLeave.AbsenceTypeName, false,
                HalfdayType.None);

            return row;
        }

        // ---- normal attendance branch (legacy lines 478-1600) ----

        var (punchIn, punchOut) = ResolvePunches(duty, dayDate, dayPolicy, dayEvents, isNightShift, isWeekOff);

        // Legacy lines 561-592: SignIn, EarlyIn, LateIn. NO 30-minute early-in cap and NO tolerance netting
        // here -- both were removed; the tolerance is applied later, as a blanking pass.
        var earlyInMins = 0;
        var lateInMins = 0;
        var lateInTs = TimeSpan.Zero;

        if (punchIn != null)
        {
            row.SignIn = FormatShortTime(punchIn.TransTime);

            if (!isWeekOff)
            {
                if (shiftStart > punchIn.TransTime)
                {
                    earlyInMins = MinutesOrZero(shiftStart - punchIn.TransTime);
                    row.EarlyIn = earlyInMins;
                }

                if (shiftStart < punchIn.TransTime)
                {
                    lateInTs = punchIn.TransTime - shiftStart;
                    lateInMins = MinutesOrZero(lateInTs);
                    row.LateIn = lateInMins;
                }
            }
        }

        // Legacy lines 594-640: SignOut, EarlyOut, LateOut.
        var earlyOutMins = 0;
        var earlyOutTs = TimeSpan.Zero;

        // LEGACY DEFECT REPRODUCED: the two lines computing lateOutMins are commented out (lines 631-632)
        // but the write is not, so this stays 0 and is written as 0 whenever the employee punched out late.
        var lateOutMins = 0;

        if (punchOut != null)
        {
            row.SignOut = FormatShortTime(punchOut.TransTime);

            if (!isWeekOff)
            {
                if (shiftEnd > punchOut.TransTime)
                {
                    earlyOutTs = shiftEnd - punchOut.TransTime;
                    earlyOutMins = MinutesOrZero(earlyOutTs);
                    row.EarlyOut = earlyOutMins;
                }

                if (shiftEnd < punchOut.TransTime)
                {
                    row.LateOut = lateOutMins;
                }
            }
        }

        // Legacy lines 642-680: partial leave. Three fields are new -- the type CODE and the leave's own
        // start/end times -- and the base's `isUnpaidLeaveApplied` scan is gone, because the daily deduction
        // no longer depends on it.
        var partialLeaves = context.Absences
            .Where(a => a.EmployeeId == employee.Id
                        && a.StartDate <= dayDate
                        && a.EndDate >= dayDate
                        && a.AbsenceHours != 0)
            .ToList();

        var partialLeaveMinutes = (int)Math.Round(
            partialLeaves.Sum(a => a.AbsenceHours) * 60.0, MidpointRounding.AwayFromZero);

        var leaveType = string.Empty;
        var leaveTypeCode = string.Empty;
        var isPartialLeave = false;
        var isPaidPartialLeave = false;
        var halfDayType = HalfdayType.None;
        var leaveStartTime = default(DateTime);
        var leaveEndTime = default(DateTime);

        var partialLeave = partialLeaves.FirstOrDefault();
        if (partialLeave != null)
        {
            leaveType = partialLeave.AbsenceTypeName;
            leaveTypeCode = partialLeave.AbsenceTypeCode;
            isPartialLeave = true;
            halfDayType = partialLeave.HalfdayType;
            isPaidPartialLeave = partialLeave.IsPaid;
            leaveStartTime = partialLeave.StartTime;
            leaveEndTime = partialLeave.EndTime;
        }

        if (partialLeaveMinutes > 0)
        {
            row.LeaveMins = partialLeaveMinutes;
        }

        // Legacy lines 766-774: THE STATUS IS COMPUTED HERE, BEFORE the three validation passes, so it
        // reflects the RAW late-in/early-out rather than the adjusted values the cells end up holding.
        // (The base computed it last, from the netted values.) Reproduced.
        row.SystemRemarks = GetStatus(
            isWeekOff, isRestDay, isPublicHoliday, publicHolidayName,
            !string.IsNullOrEmpty(row.SignIn), !string.IsNullOrEmpty(row.SignOut),
            lateInTs, earlyOutTs, TimeSpan.Zero, leaveType, isPartialLeave, halfDayType);

        // ---- VALIDATION 1: missed punch (legacy lines 776-802).
        var isMissedPunchIn = false;
        var isMissedPunchOut = false;

        if (punchIn == null && punchOut != null && !isWeekOff)
        {
            isMissedPunchIn = true;

            lateOutMins = 0;
            row.LateOut = lateOutMins;

            lateInMins = DefaultPenaltyDeductionMinutes;
            row.LateIn = lateInMins;
        }

        if (punchIn != null && punchOut == null && !isWeekOff)
        {
            isMissedPunchOut = true;

            earlyInMins = 0;
            row.EarlyIn = earlyInMins;

            earlyOutMins = DefaultPenaltyDeductionMinutes;
            row.EarlyOut = earlyOutMins;
        }

        // ---- VALIDATION 2: within-tolerance is BLANKED (legacy lines 758-768). A missed-punch penalty is
        // never forgiven, however small the tolerance comparison would make it look.
        if (lateInMins > 0 && lateInMins <= lateInTolerance && !isMissedPunchIn)
        {
            lateInMins = 0;
            row.LateIn = null;
        }

        if (earlyOutMins > 0 && earlyOutMins <= earlyOutTolerance && !isMissedPunchOut)
        {
            earlyOutMins = 0;
            row.EarlyOut = null;
        }

        // Legacy lines 786-812: planned and working minutes.
        var plannedMins = ComputePlannedMinutes(shiftStart, shiftEnd, isNightShift, isWeekOff);
        var workingMins = 0;

        // CUSTOM019: the condition is "not a week off" (line 794), where the base required BOTH punches to
        // exist. A day with no punches at all therefore now reports working minutes -- the planned shift
        // less whatever penalties the validation passes charged.
        if (!isWeekOff)
        {
            workingMins = plannedMins
                          + earlyInMins
                          + lateOutMins
                          - lateInMins
                          - earlyOutMins
                          + (isPaidPartialLeave ? partialLeaveMinutes : 0);
        }

        if (!isPublicHoliday)
        {
            // A zero is left BLANK, not written.
            if (plannedMins != 0)
            {
                row.PlannedMins = plannedMins;
            }

            if (workingMins != 0)
            {
                row.WorkingMins = workingMins;
            }
        }

        // ---- VALIDATION 3 + accumulation: the daily deduction (legacy lines 818-856).
        var dailyDeductionMins = 0;

        if (!isPublicHoliday)
        {
            var isExcludedLeave = leaveTypeCode != null
                                  && ExcludedLeaveTypeCodes.Any(
                                      c => c.Equals(leaveTypeCode, StringComparison.OrdinalIgnoreCase));

            if (isExcludedLeave)
            {
                // A leave that starts at or before the shift start excuses that much of the late arrival --
                // but never a missed-punch penalty.
                if (!isMissedPunchIn && leaveStartTime.TimeOfDay <= shiftStart)
                {
                    lateInMins = Math.Max(0, lateInMins - partialLeaveMinutes);
                    row.LateIn = lateInMins > 0 ? lateInMins : (int?)null;
                }

                // Symmetrically for an early departure. Note the OR: a leave that merely starts after the
                // shift start also qualifies, so an afternoon outpass excuses an early leave.
                if (!isMissedPunchOut
                    && (leaveEndTime.TimeOfDay >= shiftEnd || leaveStartTime.TimeOfDay >= shiftStart))
                {
                    earlyOutMins = Math.Max(0, earlyOutMins - partialLeaveMinutes);
                    row.EarlyOut = earlyOutMins > 0 ? earlyOutMins : (int?)null;
                }
            }

            dailyDeductionMins = lateInMins + earlyOutMins;

            // Accumulated ONCE, after every adjustment. A missed-punch penalty always accumulates; an
            // ordinary value only when it exceeds the daily tolerance.
            if (lateInMins > lateInTolerance || isMissedPunchIn)
            {
                monthlyLateInDeduction += lateInMins;
            }

            if (earlyOutMins > earlyOutTolerance || isMissedPunchOut)
            {
                monthlyEarlyOutDeduction += earlyOutMins;
            }
        }

        // Written unconditionally -- 0 on a public holiday, unlike the base's DA which was simply absent.
        row.DailyDeduction = dailyDeductionMins;

        runningDeductionMins += dailyDeductionMins;
        row.RunningDeduction = runningDeductionMins;

        return row;
    }

    /// <summary>
    /// Ports legacy lines 490-559 -- unchanged from the base: FlexiTime policies use the duty's flexi
    /// window and ignore the transaction type, everything else uses the shift time +/- the policy's
    /// ClockIn/ClockOut limit and matches on TransType code 1 / 2; a week-off duty with a 00:00-00:00 shift
    /// widens the window to the whole day. <c>TimeEventRule</c> then decides first/last in and out.
    /// </summary>
    private static (PunchEvent PunchIn, PunchEvent PunchOut) ResolvePunches(
        Duty duty,
        DateTime dayDate,
        TimePolicyInfo dayPolicy,
        List<PunchEvent> dayEvents,
        bool isNightShift,
        bool isWeekOff)
    {
        List<PunchEvent> inCandidates;
        List<PunchEvent> outCandidates;

        if (dayPolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            // Legacy dereferenced `duty` unguarded here; an unresolvable duty is treated as an empty flexi
            // window instead of a NullReferenceException.
            var flexiStart = duty?.FlexiStartTime ?? TimeSpan.Zero;
            var flexiEnd = duty?.FlexiEndTime ?? TimeSpan.Zero;

            var flexiStartWindow = dayDate.Add(flexiStart);
            var flexiEndWindow = isNightShift
                ? dayDate.Add(flexiEnd).AddDays(1)
                : dayDate.Add(flexiEnd);

            inCandidates = dayEvents
                .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                .ToList();
            outCandidates = inCandidates;
        }
        else
        {
            var shiftStart = duty?.StartTime ?? TimeSpan.Zero;
            var shiftEnd = duty?.EndTime ?? TimeSpan.Zero;

            var clockInStart = dayDate.Add(shiftStart).AddMinutes(-dayPolicy.ClockInLimit);
            var clockInEnd = dayDate.Add(shiftStart).AddMinutes(dayPolicy.ClockInLimit);

            var shiftEndAnchor = isNightShift ? dayDate.Add(shiftEnd).AddDays(1) : dayDate.Add(shiftEnd);
            var clockOutStart = shiftEndAnchor.AddMinutes(-dayPolicy.ClockOutLimit);
            var clockOutEnd = shiftEndAnchor.AddMinutes(dayPolicy.ClockOutLimit);

            if (isWeekOff && shiftStart == TimeSpan.Zero && shiftEnd == TimeSpan.Zero)
            {
                clockInStart = dayDate.Date;
                clockInEnd = dayDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                clockOutStart = clockInStart;
                clockOutEnd = clockInEnd;
            }

            inCandidates = dayEvents
                .Where(e => e.TransTypeCode == ClockInTransTypeCode
                            && clockInStart <= e.TransDateTime
                            && clockInEnd >= e.TransDateTime)
                .ToList();

            outCandidates = dayEvents
                .Where(e => e.TransTypeCode == ClockOutTransTypeCode
                            && clockOutStart <= e.TransDateTime
                            && clockOutEnd >= e.TransDateTime)
                .ToList();
        }

        // A policy with TimeEventRule.NA matches none of the four branches and resolves NO punches at all --
        // which on this variant means BOTH missed-punch penalties are skipped too, since they require one
        // punch to exist.
        return dayPolicy.TimeEventRule switch
        {
            TimeEventRule.FirstInFirstOut => (
                inCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                outCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault()),
            TimeEventRule.FirstInLastOut => (
                inCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                outCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault()),
            TimeEventRule.LastInFirstOut => (
                inCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                outCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault()),
            TimeEventRule.LastInLastOut => (
                inCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                outCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault()),
            _ => (null, null)
        };
    }

    /// <summary>
    /// The shift length, reversed for a night shift, and zero on a week-off in the normal branch. The
    /// full-day-leave branch does NOT apply the week-off guard, so a week-off day covered by a full-day
    /// leave reports the shift length while a plain week-off day reports zero. Reproduced from the base,
    /// where the same asymmetry exists.
    /// </summary>
    private static int ComputePlannedMinutes(
        TimeSpan shiftStart,
        TimeSpan shiftEnd,
        bool isNightShift,
        bool applyWeekOffGuard)
    {
        if (applyWeekOffGuard)
        {
            return 0;
        }

        return MinutesOrZero(!isNightShift ? shiftEnd - shiftStart : shiftStart - shiftEnd);
    }

    /// <summary>Legacy <c>MinutesOrZero</c> (lines 1743-1748).</summary>
    private static int MinutesOrZero(TimeSpan value)
    {
        return (int)Math.Round(value.TotalMinutes);
    }

    private static string FormatShortTime(TimeSpan value)
    {
        return default(DateTime).Add(value).ToString("t", CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// The weekday name through a hand-written English/Arabic ternary chain rather than the resource files.
    /// Reproduced verbatim so the exported text does not change.
    /// </summary>
    private string GetLocalizedWeekDayName(DateTime dayDate)
    {
        if (!UseArabicNames)
        {
            return dayDate.DayOfWeek.ToString();
        }

        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => "الأحد",
            DayOfWeek.Monday => "الإثنين",
            DayOfWeek.Tuesday => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday => "الخميس",
            DayOfWeek.Friday => "يوم الجمعة",
            _ => "يوم السبت"
        };
    }

    /// <summary>
    /// Verbatim port of Custom019's <c>GetStatus</c> (lines 1750-1810). FOUR DIFFERENCES from the base's:
    /// the <c>dailyDed</c> parameter and its "Extra time" verdict are gone (the DA it depended on no longer
    /// exists), a public holiday reads <c>"{name} (PH)"</c>, a leave type reads <c>"{name} (Leave)"</c>,
    /// and a day with neither punch reads "Missed In + Missed Out" where the base said "No Show". As in the
    /// base, every verdict is a hardcoded ENGLISH literal in legacy and is emitted as-is.
    /// </summary>
    private static string GetStatus(
        bool isWeekOff,
        bool isRestDay,
        bool isPublicHoliday,
        string phName,
        bool hasIn,
        bool hasOut,
        TimeSpan lateIn,
        TimeSpan earlyOut,
        TimeSpan overTime,
        string leaveType,
        bool isPartialLeave,
        HalfdayType halfDayType)
    {
        var isLate = lateIn != TimeSpan.Zero;
        var isEarly = earlyOut != TimeSpan.Zero;
        var leaveTypeName = string.IsNullOrEmpty(leaveType) ? string.Empty : leaveType + " (Leave)";

        // 1 : public holidays and weekends first (highest priority).
        if (isPublicHoliday)
        {
            return phName + " (PH)";
        }

        if (isWeekOff || isRestDay)
        {
            return "WeekEnd";
        }

        // 2 : approved absences and leave types.
        if (!isPartialLeave && leaveTypeName != string.Empty)
        {
            return leaveTypeName;
        }

        // 3 : partial leave.
        if (isPartialLeave && leaveTypeName != string.Empty)
        {
            if (halfDayType != HalfdayType.None)
            {
                return halfDayType == HalfdayType.StartOfDuty
                    ? "First Half Day Leave"
                    : "Second Half Day Leave";
            }

            if (isLate && isEarly)
            {
                return "Late + Early + " + leaveTypeName;
            }

            if (isLate)
            {
                return "Late + " + leaveTypeName;
            }

            if (isEarly)
            {
                return "Early + " + leaveTypeName;
            }

            return leaveTypeName;
        }

        // 4 : time-based conditions.
        if (isLate && isEarly)
        {
            return "Late + Early";
        }

        if (isLate)
        {
            return "Late";
        }

        if (isEarly)
        {
            return "Early";
        }

        if (overTime != TimeSpan.Zero)
        {
            return string.Empty;
        }

        // 5 : missed punch in/out.
        if (!hasIn || !hasOut)
        {
            if (!hasIn && !hasOut)
            {
                return "Missed In + Missed Out";
            }

            return !hasIn ? "Missed In" : "Missed Out";
        }

        return string.Empty;
    }

    /// <summary>
    /// The exported column set, in legacy DataTable order (lines 750-796).
    ///
    /// The Summary variant returns the reduced set legacy's <c>keepColumns</c> array leaves behind after it
    /// removes every other column from the DataTable (lines 1653-1670) -- note that it drops Date, WeekDay,
    /// the shift bounds, the sign in/out, Leave Mins, Working Mins and Planned Mins, but KEEPS Early In and
    /// Late Out even though the monthly row never fills them.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<MonthlyAttWithToleranceCustom019ReportRowDto>>
        GetExportColumns(MonthlyAttWithToleranceCustom019Variant variant)
    {
        if (variant == MonthlyAttWithToleranceCustom019Variant.Summary)
        {
            return new List<GridReportColumn<MonthlyAttWithToleranceCustom019ReportRowDto>>
            {
                new("Code", r => r.EmployeeCode),
                new("Employee", r => r.EmployeeName),
                new("EarlyIn", r => r.EarlyIn),
                new("LateIn", r => r.LateIn),
                new("EarlyOut", r => r.EarlyOut),
                new("LateOut", r => r.LateOut),
                new("DailyDeduction", r => r.DailyDeduction),
                new("RunningDeduction", r => r.RunningDeduction),
                new("Status", r => r.SystemRemarks)
            };
        }

        return new List<GridReportColumn<MonthlyAttWithToleranceCustom019ReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Date", r => r.DayDate),
            new("WeekDay", r => r.WeekDay),
            new("ShiftStart", r => r.ShiftStart),
            new("ShiftEnd", r => r.ShiftEnd),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("LeaveMins", r => r.LeaveMins),
            new("EarlyIn", r => r.EarlyIn),
            new("LateIn", r => r.LateIn),
            new("EarlyOut", r => r.EarlyOut),
            new("LateOut", r => r.LateOut),
            new("WorkingMins", r => r.WorkingMins),
            new("PlannedMins", r => r.PlannedMins),
            new("DailyDeduction", r => r.DailyDeduction),
            new("RunningDeduction", r => r.RunningDeduction),
            new("Status", r => r.SystemRemarks)
        };
    }

    // ---- in-memory shapes used by the day loop (legacy used anonymous types and MyTimeEvent) ----

    private class EmployeeInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string FullName { get; set; }
    }

    private class PunchEvent
    {
        public int EmployeeId { get; set; }

        public string TransTypeCode { get; set; }

        public DateTime TransDate { get; set; }

        /// <summary>Legacy <c>TransTime</c> -- WITH the seconds, unlike the base report.</summary>
        public TimeSpan TransTime { get; set; }

        public DateTime TransDateTime { get; set; }
    }

    private class TimePolicyInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double ClockInLimit { get; set; }

        public double ClockOutLimit { get; set; }

        public TimeEventRule TimeEventRule { get; set; }

        public TimeMgmtStatus TimeMgmtStatus { get; set; }

        public double LateInTolerance { get; set; }

        public double EarlyOutTolerance { get; set; }

        /// <summary>Custom019-only.</summary>
        public double MonthlyLateInTolerance { get; set; }

        /// <summary>Custom019-only.</summary>
        public double MonthlyEarlyOutTolerance { get; set; }
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string AbsenceTypeName { get; set; }

        /// <summary>Custom019-only: gates the excluded-leave adjustment.</summary>
        public string AbsenceTypeCode { get; set; }

        public bool IsPaid { get; set; }

        public HalfdayType HalfdayType { get; set; }

        public double AbsenceHours { get; set; }

        /// <summary>Custom019-only: decides whether the leave excuses the late-in.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Custom019-only: decides whether the leave excuses the early-out.</summary>
        public DateTime EndTime { get; set; }
    }

    private class MonthlyToleranceContext
    {
        public List<PunchEvent> TimeEvents { get; set; }

        public List<EmployeeDutyPeriod> DutyPeriods { get; set; }

        public List<TimePolicyInfo> TimePolicies { get; set; }

        public List<AbsenceInfo> Absences { get; set; }

        public List<EmployeeSubstituteDuty> SubstituteDuties { get; set; }
    }
}
