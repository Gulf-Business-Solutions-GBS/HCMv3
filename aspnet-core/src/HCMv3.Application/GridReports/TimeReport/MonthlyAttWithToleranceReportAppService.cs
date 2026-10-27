using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Monthly Attendance With Tolerance grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 544-1268).
///
/// THE ODD ONE OUT OF THE FAMILY. Every other report in this wave is "filter a table, project columns";
/// this one RECOMPUTES attendance. It does not read <c>TimeRegister</c> at all. For each employee and each
/// calendar day of the selected month it resolves the duty (roster, substitute duty or the duty period's
/// day-of-week duty), matches raw <c>TimeEvent</c> punches inside the time policy's clock-in/clock-out
/// windows (or the duty's flexi window), derives early-in / late-in / early-out / late-out, nets the
/// policy's late-in and early-out tolerances, folds in partial and full-day absences and public holidays,
/// and accumulates a running monthly deduction (MA) against <c>TimePolicy.MonthlyAfterTolerance</c>.
///
/// EMPLOYEE CODE IS MANDATORY (legacy lines 633-635 return an empty table on a blank box). Reproduced --
/// the day-by-day recomputation is far too expensive to run company-wide.
///
/// ACTIVE EMPLOYEES ONLY: legacy's source set is <c>Employee.Status == Active</c> (line 640), with no
/// status combo on the panel.
///
/// DEAD LEGACY FILTER NOT PORTED: <c>cmbPayrollArea</c> exists on the panel but the code that would apply
/// it is commented out (lines 651-659), so it has no effect on the data and is not carried across --
/// implementing it would change what the screen returns.
///
/// CORRECTED DEFECT (stale shift-time leakage). Legacy declares <c>shiftStartTime</c>/<c>shiftEndTime</c>
/// OUTSIDE both loops (lines 876-879) and assigns them only inside <c>if (duty != null)</c> (line 891), so
/// a day with no resolvable duty silently reports the PREVIOUS day's -- or the previous EMPLOYEE's --
/// shift times and planned minutes. That is cross-row data corruption rather than a quirk worth
/// preserving, so this port resets the shift bounds for every day (pinned by a test).
///
/// MODEL ADAPTATIONS: HCMv3's <c>Duty.StartTime</c>/<c>EndTime</c>/<c>FlexiStartTime</c>/<c>FlexiEndTime</c>
/// are <see cref="TimeSpan"/> where legacy carried <see cref="DateTime"/> values whose date part was
/// meaningless; legacy's <c>DateTimeExtension.DBDtNull</c> "unset" sentinel becomes
/// <c>TimeSpan.Zero</c>/<c>default(DateTime)</c> per the convention already set by
/// <c>PunchResolutionStrategyBase</c>. The public-holiday and roster lookups delegate to the already-ported
/// <see cref="ITimeDataService"/> and <see cref="IRosterService"/>, the same entry points legacy called.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the ColumnChooser JSON persistence,
/// the callback-panel state machine, the PDF export button, and the dead <c>Table1_Old()</c> (line 1341+),
/// an earlier formulation of the same recomputation. Legacy also computed <c>breakShiftStartTime</c>/
/// <c>breakShiftEndTime</c> (lines 895-905) and never used them in any output cell -- skipped as dead code.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_MonthlyAttWithTolerance)]
public class MonthlyAttWithToleranceReportAppService
    : GridReportAppServiceBase, IMonthlyAttWithToleranceReportAppService
{
    /// <summary>Legacy <c>earlyInMaxAllowedlimit</c> (line 869): a hardcoded 30, not a policy setting.</summary>
    private const int EarlyInMaxAllowedLimit = 30;

    /// <summary>Legacy <c>TransType.Code</c> for a clock-in punch (line 1035).</summary>
    private const string ClockInTransTypeCode = "1";

    /// <summary>Legacy <c>TransType.Code</c> for a clock-out punch (line 1039).</summary>
    private const string ClockOutTransTypeCode = "2";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly ITimeDataService _timeDataService;
    private readonly IRosterService _rosterService;

    public MonthlyAttWithToleranceReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        ITimeDataService timeDataService,
        IRosterService rosterService)
    {
        _employeeRepository = employeeRepository;
        _timeEventRepository = timeEventRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _timeDataService = timeDataService;
        _rosterService = rosterService;
    }

    protected override string LegacyScreenName => "uscRptMonthlyAttWithTolerance";

    protected override string ScreenCaption => "Monthly Attendance With Tolerance";

    [HttpPost]
    public async Task<PagedResultDto<MonthlyAttWithToleranceReportRowDto>> GetMonthlyAttWithToleranceReport(
        GetMonthlyAttWithToleranceReportInput input)
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

        return new PagedResultDto<MonthlyAttWithToleranceReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetMonthlyAttWithToleranceReportToExcel(
        GetMonthlyAttWithToleranceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "MonthlyAttWithTolerance.xlsx",
            rows,
            GetExportColumns(),
            BuildToleranceParameterString(input));
    }

    /// <summary>Ports legacy <c>GetParameterStr()</c> (lines 529-541): employee code, month and year.</summary>
    private string BuildToleranceParameterString(GetMonthlyAttWithToleranceReportInput input)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Single source of truth for filtering AND row generation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid. Ports legacy
    /// <c>Table1()</c> (lines 630-1268).
    ///
    /// The result is an in-memory queryable because the rows do not exist in the database: they are
    /// computed. The set is bounded by (employees named in the code box) x (days in one month), which is
    /// exactly why legacy demands an employee code.
    /// </summary>
    private async Task<IQueryable<MonthlyAttWithToleranceReportRowDto>> BuildQueryAsync(
        GetMonthlyAttWithToleranceReportInput input)
    {
        var rows = await BuildRowsAsync(input);
        return rows.AsQueryable();
    }

    private async Task<List<MonthlyAttWithToleranceReportRowDto>> BuildRowsAsync(
        GetMonthlyAttWithToleranceReportInput input)
    {
        var rows = new List<MonthlyAttWithToleranceReportRowDto>();

        // Legacy lines 633-635: a blank employee-code box returns an EMPTY table, full stop.
        var employeeCodes = input.ResolveEmployeeCodes();
        if (employeeCodes.Count == 0)
        {
            return rows;
        }

        var (startDate, endDate) = ResolveMonthWindow(input);

        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        // Legacy lines 640-660: Active employees only, narrowed by the code box, then row-level security.
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Where(e => employeeCodes.Contains(e.Code))
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
            // Legacy lines 866-1265: the whole per-employee day loop, including the running MA total.
            rows.AddRange(await BuildEmployeeRowsAsync(
                employee, startDate, endDate, context, isRosterActive));
        }

        return rows;
    }

    /// <summary>
    /// Ports legacy lines 478-494: Month/Year (defaulting to today's) become the first and last day of
    /// that calendar month. Legacy caught a bad Month/Year and fell back to January 1900; the DTO's
    /// <c>[Range]</c> attributes make that unreachable here, so the fallback is not carried.
    /// </summary>
    private (DateTime StartDate, DateTime EndDate) ResolveMonthWindow(
        GetMonthlyAttWithToleranceReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        var startDate = new DateTime(year, month, 1);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        return (startDate, endDate);
    }

    /// <summary>
    /// Ports legacy's "Load Initial Data" region (lines 673-760): every collection the day loop needs is
    /// fetched ONCE for the whole employee set and the whole month, then queried in memory. Legacy did the
    /// same, and it is what keeps a 31-day x N-employee loop from issuing thousands of round trips.
    /// </summary>
    private async Task<MonthlyToleranceContext> LoadContextAsync(
        List<int> employeeIds,
        DateTime startDate,
        DateTime endDate,
        bool useArabicNames)
    {
        // Legacy lines 687-712. The window deliberately runs to EndDate + 1 day so that a night shift
        // starting on the last day of the month can still find its clock-out punch.
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
                // Legacy line 700 trims the seconds off TransTime before comparing or displaying it.
                TransTime = new TimeSpan(t.TransTime.Hour, t.TransTime.Minute, 0),
                TransDateTime = t.TransDate.Date
                    .AddHours(t.TransTime.Hour)
                    .AddMinutes(t.TransTime.Minute)
            })
            .ToList();

        // Legacy lines 715-718. The day duties are needed to resolve the day's duty in the classic branch.
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

        // Legacy lines 720-741.
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
                MonthlyAfterTolerance = p.TimePolicy.MonthlyAfterTolerance,
                CalcLateInAfterTolerance = p.TimePolicy.CalcLateInAfterTolerance,
                LateInTolerance = p.TimePolicy.LateInTolerance,
                CalcEarlyOutAfterTolerance = p.TimePolicy.CalcEarlyOutAfterTolerance,
                EarlyOutTolerance = p.TimePolicy.EarlyOutTolerance
            })
            .ToListAsync();

        // Legacy lines 743-766: absences OVERLAPPING the month, spelled out as four alternatives which
        // reduce to StartDate <= monthEnd && EndDate >= monthStart.
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
                IsPaid = a.AbsenceType.IsPaid,
                HalfdayType = a.HalfdayType,
                AbsenceHours = a.AbsenceHours
            })
            .ToListAsync();

        // Legacy lines 768-773.
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
    /// Ports the per-employee body of legacy's day loop (lines 866-1265). The running
    /// <c>totalMonthlyLateInEarlyoutMins</c> ("MA") is deliberately scoped to the employee and reset
    /// between employees, exactly as legacy scoped it.
    /// </summary>
    private async Task<List<MonthlyAttWithToleranceReportRowDto>> BuildEmployeeRowsAsync(
        EmployeeInfo employee,
        DateTime startDate,
        DateTime endDate,
        MonthlyToleranceContext context,
        bool isRosterActive)
    {
        var rows = new List<MonthlyAttWithToleranceReportRowDto>();

        // Legacy lines 869-891: the monthly tolerance and the two daily tolerances come from the policy
        // covering the FIRST day of the month, not from each day's policy.
        var totalMonthlyLateInEarlyOutMins = 0;
        var monthlyAfterToleranceMins = 0;
        var lateInTolerance = 0;
        var earlyOutTolerance = 0;

        var policyForMonthlyTolerance = context.TimePolicies.FirstOrDefault(
            p => p.EmployeeId == employee.Id && p.StartDate <= startDate && p.EndDate >= startDate);

        if (policyForMonthlyTolerance != null)
        {
            monthlyAfterToleranceMins = (int)policyForMonthlyTolerance.MonthlyAfterTolerance;
            lateInTolerance = policyForMonthlyTolerance.CalcLateInAfterTolerance
                ? (int)policyForMonthlyTolerance.LateInTolerance
                : 0;
            earlyOutTolerance = policyForMonthlyTolerance.CalcEarlyOutAfterTolerance
                ? (int)policyForMonthlyTolerance.EarlyOutTolerance
                : 0;
        }

        // Legacy line 894: TimeDataService.GetPublicHolidaysdatesList.
        var publicHolidays = await _timeDataService.GetPublicHolidaysDatesListAsync(
            employee.Id, startDate, endDate);

        var employeeSubstituteDuties = context.SubstituteDuties
            .Where(s => s.EmployeeId == employee.Id)
            .ToList();

        for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
        {
            // Legacy lines 903-912: no time policy covering the day -> a diagnostic row with ONLY the
            // remarks cell filled, then skip the day.
            var dayPolicy = context.TimePolicies.FirstOrDefault(
                p => p.EmployeeId == employee.Id && p.StartDate <= dayDate && p.EndDate >= dayDate);

            if (dayPolicy == null)
            {
                rows.Add(new MonthlyAttWithToleranceReportRowDto
                {
                    SystemRemarks = "Time policy not found on date : " + dayDate.ToString("dd/MM/yyyy")
                });
                continue;
            }

            // Legacy lines 914-923: same shape for a missing duty period.
            var dutyPeriod = context.DutyPeriods
                .Where(d => d.EmployeeId == employee.Id && d.StartDate <= dayDate && d.EndDate >= dayDate)
                .Select(d => d.DutyPeriod)
                .FirstOrDefault();

            if (dutyPeriod == null)
            {
                rows.Add(new MonthlyAttWithToleranceReportRowDto
                {
                    SystemRemarks = "Duty Period not found on date : " + dayDate.ToString("dd/MM/yyyy")
                });
                continue;
            }

            var currentDay = dayDate;

            // Legacy lines 925-931: punches from the day itself plus its neighbours, so a night shift can
            // pick up a clock-out that landed after midnight.
            var dayEvents = context.TimeEvents
                .Where(t => t.EmployeeId == employee.Id
                            && (t.TransDate == currentDay
                                || t.TransDate == currentDay.AddDays(1)
                                || t.TransDate == currentDay.AddDays(-1)))
                .ToList();

            var duty = isRosterActive
                // Legacy line 887: the roster branch delegates substitute-duty handling to the service.
                ? await _rosterService.GetRosterDutyAsync(
                    employee.Id, dayDate, employeeSubstituteDuties, false)
                // Legacy lines 873-885: a substitute duty wins over the duty period's day-of-week duty.
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
                monthlyAfterToleranceMins,
                ref totalMonthlyLateInEarlyOutMins));
        }

        return rows;
    }

    /// <summary>Legacy lines 878-885: the duty period's Sunday..Saturday duty for the day's weekday.</summary>
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
    /// Ports the body of one day (legacy lines 889-1264): the full-day-leave branch and the normal
    /// attendance branch.
    /// </summary>
    private MonthlyAttWithToleranceReportRowDto BuildDayRow(
        EmployeeInfo employee,
        DateTime dayDate,
        Duty duty,
        TimePolicyInfo dayPolicy,
        List<PunchEvent> dayEvents,
        MonthlyToleranceContext context,
        List<(DateTime HolidayDate, string HolidayName)> publicHolidays,
        int lateInTolerance,
        int earlyOutTolerance,
        int monthlyAfterToleranceMins,
        ref int totalMonthlyLateInEarlyOutMins)
    {
        // CORRECTED DEFECT: reset per day -- see the class remarks. Legacy leaked the previous day's or
        // previous employee's values here whenever the duty could not be resolved.
        var shiftStart = duty?.StartTime ?? TimeSpan.Zero;
        var shiftEnd = duty?.EndTime ?? TimeSpan.Zero;
        var isNightShift = duty != null && duty.IsNightShift;
        var isWeekOff = duty != null && duty.IsWeeklyOff;
        var isRestDay = duty != null && duty.IsRestDay;

        var weekDay = GetLocalizedWeekDayName(dayDate);

        // Legacy lines 902-908: the first public-holiday entry matching the day, if any.
        var holidayIndex = publicHolidays.FindIndex(h => h.HolidayDate == dayDate);
        var isPublicHoliday = holidayIndex >= 0;
        var publicHolidayName = isPublicHoliday
            ? publicHolidays[holidayIndex].HolidayName ?? string.Empty
            : string.Empty;

        var row = new MonthlyAttWithToleranceReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            DayDate = dayDate,
            WeekDay = weekDay,
            ShiftStart = FormatShortTime(shiftStart),
            ShiftEnd = FormatShortTime(shiftEnd)
        };

        // Legacy lines 911-916: a full-day leave is an absence covering the day with AbsenceHours == 0.
        var fullDayLeave = context.Absences.FirstOrDefault(
            a => a.EmployeeId == employee.Id
                 && a.StartDate <= dayDate
                 && a.EndDate >= dayDate
                 && a.AbsenceHours == 0);

        if (fullDayLeave != null)
        {
            // Legacy lines 917-977: shift bounds, planned/working minutes and the deduction flag only;
            // DA and MA are deliberately NOT written on a full-day-leave row.
            var leavePlannedMins = ComputePlannedMinutes(shiftStart, shiftEnd, isNightShift, false);

            if (!isPublicHoliday)
            {
                row.PlannedMins = leavePlannedMins;
                row.WorkingMins = 0;
            }

            row.Deduction = totalMonthlyLateInEarlyOutMins > monthlyAfterToleranceMins;
            row.SystemRemarks = GetStatus(
                isWeekOff, isRestDay, isPublicHoliday, publicHolidayName, false, false,
                TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, fullDayLeave.AbsenceTypeName, false,
                HalfdayType.None, 0);

            return row;
        }

        // ---- normal attendance branch (legacy lines 979-1262) ----

        var (punchIn, punchOut) = ResolvePunches(duty, dayDate, dayPolicy, dayEvents, isNightShift, isWeekOff);

        // Legacy lines 1065-1105: SignIn, EarlyIn, LateIn.
        var earlyInMins = 0;
        var lateInMins = 0;
        var nettedLateIn = TimeSpan.Zero;
        var nettedLateInMins = 0;
        var actualIn = default(DateTime);

        if (punchIn != null)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = FormatShortTime(punchIn.TransTime);

            if (!isWeekOff)
            {
                if (shiftStart > punchIn.TransTime)
                {
                    earlyInMins = MinutesOrZero(shiftStart - punchIn.TransTime);

                    // Legacy lines 1084-1086: an early arrival counts for at most 30 minutes.
                    if (earlyInMins > EarlyInMaxAllowedLimit)
                    {
                        earlyInMins = EarlyInMaxAllowedLimit;
                    }

                    row.EarlyIn = earlyInMins;
                }

                if (shiftStart < punchIn.TransTime)
                {
                    var lateInTs = punchIn.TransTime - shiftStart;
                    nettedLateIn = lateInTs;
                    lateInMins = MinutesOrZero(lateInTs);
                    nettedLateInMins = lateInMins;

                    // Legacy lines 1094-1099: within tolerance is not late AT ALL for the deduction, but
                    // the displayed cell still shows the raw minutes (line 1101).
                    if (lateInMins <= lateInTolerance)
                    {
                        nettedLateIn = TimeSpan.Zero;
                        nettedLateInMins = 0;
                    }

                    row.LateIn = lateInMins;
                }
            }
        }

        // Legacy lines 1107-1149: SignOut, EarlyOut, LateOut.
        var nettedEarlyOut = TimeSpan.Zero;
        var nettedEarlyOutMins = 0;
        var lateOutMins = 0;
        var actualOut = default(DateTime);

        if (punchOut != null)
        {
            actualOut = punchOut.TransDateTime;
            row.SignOut = FormatShortTime(punchOut.TransTime);

            if (!isWeekOff)
            {
                if (shiftEnd > punchOut.TransTime)
                {
                    var earlyOutTs = shiftEnd - punchOut.TransTime;
                    nettedEarlyOut = earlyOutTs;
                    var earlyOutMins = MinutesOrZero(earlyOutTs);
                    nettedEarlyOutMins = earlyOutMins;

                    row.EarlyOut = earlyOutMins;

                    if (earlyOutMins <= earlyOutTolerance)
                    {
                        nettedEarlyOut = TimeSpan.Zero;
                        nettedEarlyOutMins = 0;
                    }
                }

                if (shiftEnd < punchOut.TransTime)
                {
                    lateOutMins = MinutesOrZero(punchOut.TransTime - shiftEnd);

                    // LEGACY QUIRK, REPRODUCED (line 1134): staying late does not count at all if the
                    // employee arrived later than the late-in tolerance.
                    lateOutMins = lateInMins > lateInTolerance ? 0 : lateOutMins;
                    row.LateOut = lateOutMins;
                }
            }
        }

        // Legacy lines 1151-1196: partial leave.
        var partialLeaves = context.Absences
            .Where(a => a.EmployeeId == employee.Id
                        && a.StartDate <= dayDate
                        && a.EndDate >= dayDate
                        && a.AbsenceHours != 0)
            .ToList();

        var partialLeaveMinutes = (int)Math.Round(
            partialLeaves.Sum(a => a.AbsenceHours) * 60.0, MidpointRounding.AwayFromZero);

        // Legacy lines 1180-1183: ANY unpaid absence covering the day suppresses the daily deduction --
        // note it scans the whole absence list, not just the partial ones.
        var isUnpaidLeaveApplied = context.Absences.Any(
            a => a.EmployeeId == employee.Id
                 && a.StartDate <= dayDate
                 && a.EndDate >= dayDate
                 && !a.IsPaid);

        var leaveType = string.Empty;
        var isPartialLeave = false;
        var isPaidPartialLeave = false;
        var halfDayType = HalfdayType.None;

        var partialLeave = partialLeaves.FirstOrDefault();
        if (partialLeave != null)
        {
            leaveType = partialLeave.AbsenceTypeName;
            isPartialLeave = true;
            halfDayType = partialLeave.HalfdayType;
            isPaidPartialLeave = partialLeave.IsPaid;
        }

        if (partialLeaveMinutes > 0)
        {
            row.LeaveMins = partialLeaveMinutes;
        }

        // Legacy lines 1198-1231: planned and working minutes.
        var plannedMins = ComputePlannedMinutes(shiftStart, shiftEnd, isNightShift, isWeekOff);
        var workingMins = 0;

        if (actualIn != default && actualOut != default)
        {
            workingMins = plannedMins
                          + earlyInMins
                          + lateOutMins
                          + (isPaidPartialLeave ? partialLeaveMinutes : 0)
                          - nettedLateInMins
                          - nettedEarlyOutMins;
        }

        if (!isPublicHoliday)
        {
            // Legacy lines 1225-1228: a zero is left BLANK, not written.
            if (plannedMins != 0)
            {
                row.PlannedMins = plannedMins;
            }

            if (workingMins != 0)
            {
                row.WorkingMins = workingMins;
            }
        }

        // Legacy lines 1233-1246: DA, MA, Deduction.
        var daMins = 0;
        if (!isUnpaidLeaveApplied && !isPublicHoliday)
        {
            daMins = plannedMins - workingMins;
        }

        row.DA = daMins;

        // Legacy lines 1241-1242: negative DA (extra time) never reduces the monthly accumulation.
        if (daMins > 0)
        {
            totalMonthlyLateInEarlyOutMins += daMins;
        }

        row.MA = totalMonthlyLateInEarlyOutMins;
        row.Deduction = totalMonthlyLateInEarlyOutMins > monthlyAfterToleranceMins;

        // Legacy lines 1248-1257.
        row.SystemRemarks = GetStatus(
            isWeekOff, isRestDay, isPublicHoliday, publicHolidayName,
            !string.IsNullOrEmpty(row.SignIn), !string.IsNullOrEmpty(row.SignOut),
            nettedLateIn, nettedEarlyOut, TimeSpan.Zero, leaveType, isPartialLeave, halfDayType, daMins);

        return row;
    }

    /// <summary>
    /// Ports legacy lines 993-1063: pick the clock-in and clock-out punches out of the day's events.
    /// FlexiTime policies use the duty's flexi window and ignore the transaction type; every other policy
    /// uses the shift time +/- the policy's ClockIn/ClockOut limit and matches on TransType code 1 / 2.
    /// A week-off duty with a 00:00-00:00 shift widens the window to the whole day (lines 1024-1032).
    /// The <c>TimeEventRule</c> then decides first/last in and first/last out.
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
            // Legacy line 1006 dereferenced `duty` unguarded here; an unresolvable duty is treated as an
            // empty flexi window instead of a NullReferenceException.
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

        // Legacy lines 1045-1062. A policy with TimeEventRule.NA matches none of the four branches and
        // therefore resolves NO punches at all -- reproduced.
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
    /// Legacy lines 1202-1207 (and 962-968 in the leave branch): the shift length, reversed for a night
    /// shift, and zero on a week-off in the normal branch. NOTE the asymmetry -- the full-day-leave branch
    /// does NOT apply the week-off guard, so a week-off day covered by a full-day leave reports the shift
    /// length as planned minutes while a plain week-off day reports zero. Reproduced.
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

        var plannedHours = !isNightShift ? shiftEnd - shiftStart : shiftStart - shiftEnd;
        return MinutesOrZero(plannedHours);
    }

    /// <summary>Legacy <c>MinutesOrZero</c> (lines 1271-1275).</summary>
    private static int MinutesOrZero(TimeSpan value)
    {
        return (int)Math.Round(value.TotalMinutes);
    }

    /// <summary>
    /// Legacy formatted every time cell with <c>DateTimeExtension.TimeFormat</c> under
    /// <c>CultureInfo.CurrentUICulture</c>; the short-time pattern is the equivalent here.
    /// </summary>
    private static string FormatShortTime(TimeSpan value)
    {
        return default(DateTime).Add(value).ToString("t", CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Legacy lines 838-845: the weekday name, localized by a hand-written English/Arabic ternary chain
    /// rather than through the resource files. Reproduced verbatim so the exported text does not change.
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
    /// Verbatim port of legacy <c>GetStatus</c> (lines 1285-1338), including its priority order and its
    /// hardcoded English verdict strings (legacy never localized them, so neither does this).
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
        HalfdayType halfDayType,
        int dailyDed)
    {
        var isLate = lateIn != TimeSpan.Zero;
        var isEarly = earlyOut != TimeSpan.Zero;
        var leaveTypeName = string.IsNullOrEmpty(leaveType) ? string.Empty : leaveType;

        // 1 : public holidays and weekends first (highest priority).
        if (isPublicHoliday)
        {
            return phName;
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

        if (dailyDed < 0)
        {
            return "Extra time";
        }

        // 5 : missed punch in/out.
        if (!hasIn || !hasOut)
        {
            if (!hasIn && !hasOut)
            {
                return "No Show";
            }

            return !hasIn ? "Missed In" : "Missed Out";
        }

        return string.Empty;
    }

    /// <summary>
    /// The exported column set, in legacy DataTable order (lines 605-628). Legacy's <c>Oid</c> column is
    /// omitted because legacy itself never assigned it a value on this screen.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<MonthlyAttWithToleranceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<MonthlyAttWithToleranceReportRowDto>>
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
            new("DailyAdjustment", r => r.DA),
            new("MonthlyAccumulation", r => r.MA),
            new("Deduction", r => r.Deduction),
            new("SystemRemarks", r => r.SystemRemarks)
        };
    }

    // ---- in-memory shapes used by the day loop (legacy used anonymous types and MyTimeEvent) ----

    private class EmployeeInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string FullName { get; set; }
    }

    /// <summary>Legacy <c>MyTimeEvent</c> (declared at the bottom of the legacy code-behind).</summary>
    private class PunchEvent
    {
        public int EmployeeId { get; set; }

        public string TransTypeCode { get; set; }

        public DateTime TransDate { get; set; }

        /// <summary>Legacy <c>TransTime</c> with the seconds trimmed (line 700).</summary>
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

        public double MonthlyAfterTolerance { get; set; }

        public bool CalcLateInAfterTolerance { get; set; }

        public double LateInTolerance { get; set; }

        public bool CalcEarlyOutAfterTolerance { get; set; }

        public double EarlyOutTolerance { get; set; }
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string AbsenceTypeName { get; set; }

        public bool IsPaid { get; set; }

        public HalfdayType HalfdayType { get; set; }

        public double AbsenceHours { get; set; }
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
