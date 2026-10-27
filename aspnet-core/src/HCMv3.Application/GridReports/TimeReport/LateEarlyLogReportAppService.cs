using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
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
/// Late/Early Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 629-1550). The BASE control only: the seven <c>_Custom0XX</c> variants and
/// <c>_Old</c> are separate legacy controls, deliberately out of scope for this port.
///
/// GRAIN: one row per (ACTIVE employee, calendar day in the window) -- but only for employees that have at
/// least one duty-period assignment overlapping the window (legacy <c>dsCount &gt; 0</c> guard, line 936)
/// and only for days on which a duty period covers the day (line 946). Within that, a day produces exactly
/// one of three row shapes: a FULL-DAY LEAVE row, a NO-RETURN-DATE row, or the normal attendance row --
/// and the normal row is skipped entirely when the employee has no time policy covering the day
/// (line 1181).
///
/// IN-MEMORY BY NECESSITY: the punch selection windows depend on the day's resolved Duty, the employee's
/// TimePolicy limits and the night-shift/week-off flags, and the resolved Duty itself depends on substitute
/// duties and on the roster. None of that is expressible as SQL, so the window-bounded input sets are
/// materialized once (exactly the six lists legacy loaded at lines 800-905) and the rows generated here,
/// then handed back as an <c>IQueryable</c> so paging, sorting and the export all go through one path.
///
/// STICKY SHIFT DEFECT (legacy lines 907-914, REPRODUCED and pinned by
/// <c>Should_Carry_Previous_Shift_Bounds_Into_A_Day_Without_Duty_Reproducing_Legacy_Defect</c>): the six
/// shift/break/flexi locals are declared OUTSIDE both loops and are only assigned when a Duty resolves. A
/// day whose Duty is null therefore reports the LAST successfully resolved day's shift bounds instead of
/// nothing. Faithfulness is the standing rule for this wave, so the locals are carried across iterations
/// here in exactly the same way.
///
/// AGREED DIVERGENCE: legacy applied RLS here (line 793) and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1551+).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLog)]
public class LateEarlyLogReportAppService : GridReportAppServiceBase, ILateEarlyLogReportAppService
{
    /// <summary>Legacy <c>TransType.Code</c> "1" is the clock-in event, "2" the clock-out event.</summary>
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    /// <summary>Legacy status literals (lines 45-53). Hard-coded there, hard-coded here.</summary>
    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string NoReturnDateEn = "No Return Date";

    private const string NoReturnDateAr = "لا تاريخ العودة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _substituteDutyRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRosterService _rosterService;
    private readonly ITimeDataService _timeDataService;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubstituteDuty> substituteDutyRepository,
        IRepository<Duty> dutyRepository,
        IRosterService rosterService,
        ITimeDataService timeDataService,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _timeEventRepository = timeEventRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _substituteDutyRepository = substituteDutyRepository;
        _dutyRepository = dutyRepository;
        _rosterService = rosterService;
        _timeDataService = timeDataService;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog";

    protected override string ScreenCaption => "Late Early Log";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogReportRowDto>> GetLateEarlyLogReport(
        GetLateEarlyLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildLateEarlyParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogReportToExcel(GetLateEarlyLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLog.xlsx",
            rows,
            GetExportColumns(),
            BuildLateEarlyParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (lines 611-627) logs the payroll area on top of the employee code
    /// and the date range. The base helper appends extra parts at the END whereas legacy printed the
    /// payroll area FIRST -- an ordering difference inside a free-text audit field.
    /// </summary>
    private string BuildLateEarlyParameterString(GetLateEarlyLogReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s "Apply Filter", "Load Initial Data" and "Check For Each Data"
    /// regions (lines 764-1546).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogReportRowDto>> BuildQueryAsync(GetLateEarlyLogReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var payrollAreaCode = input.PayrollAreaCode;
        var hasPayrollAreaFilter = !payrollAreaCode.IsNullOrWhiteSpace();
        var today = Today;

        if (fromDate > toDate)
        {
            return new List<LateEarlyLogReportRowDto>().AsQueryable();
        }

        // Legacy line 550/876: EnableRosterDuty switches duty resolution to the roster service entirely.
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy lines 765-793: ACTIVE employees only (hard-coded, there is no status combo), optionally
        // restricted to a payroll area as of TODAY, then the comma-split code list, then RLS.
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(hasPayrollAreaFilter, e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.PayrollAreaId != null
                          && o.PayrollArea.Code == payrollAreaCode))
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => new EmployeeInfo
            {
                Id = e.Id,
                Code = e.Code,
                FullName = useArabicNames ? e.FullNameAr : e.FullNameEn,
                AccessId = e.AccessID
            })
            .OrderBy(e => e.Code)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new List<LateEarlyLogReportRowDto>().AsQueryable();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Legacy lines 800-817: TransDate between dtFrom and dtTo + 1 DAY -- the extra day is what lets a
        // night shift's clock-out (which falls on the following calendar day) be found.
        var timeEventCeiling = toDate.AddDays(1);
        var timeEvents = await _timeEventRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.TransDate.Date >= fromDate
                        && t.TransDate.Date <= timeEventCeiling)
            .Select(t => new TimeEventInfo
            {
                EmployeeId = t.EmployeeId,
                TransTypeCode = t.TransType.Code,
                TransDate = t.TransDate,
                TransTime = t.TransTime,
                WorkLocation = t.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? t.WorkLocation.NameAr : t.WorkLocation.NameEn),
                AccessUnit = t.AccessUnit == null
                    ? string.Empty
                    : (useArabicNames ? t.AccessUnit.NameAr : t.AccessUnit.NameEn)
            })
            .ToListAsync();

        foreach (var timeEvent in timeEvents)
        {
            timeEvent.TransDateTime = timeEvent.TransDate.Date.Add(timeEvent.TransTime.TimeOfDay);
        }

        var eventsByEmployee = timeEvents.GroupBy(t => t.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        // Legacy lines 819-831: duty-period assignments overlapping the window, DutyPeriod not null.
        var dutyPeriodAssignments = await _employeeDutyPeriodRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeId)
                        && d.StartDate.Date <= toDate
                        && d.EndDate.Date >= fromDate)
            .Select(d => new DutyPeriodInfo
            {
                EmployeeId = d.EmployeeId,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                SundayDutyId = d.DutyPeriod.SundayDutyId,
                MondayDutyId = d.DutyPeriod.MondayDutyId,
                TuesdayDutyId = d.DutyPeriod.TuesdayDutyId,
                WednesdayDutyId = d.DutyPeriod.WednesdayDutyId,
                ThursdayDutyId = d.DutyPeriod.ThursdayDutyId,
                FridayDutyId = d.DutyPeriod.FridayDutyId,
                SaturdayDutyId = d.DutyPeriod.SaturdayDutyId
            })
            .ToListAsync();

        var dutyPeriodsByEmployee = dutyPeriodAssignments
            .GroupBy(d => d.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Legacy lines 833-851.
        var timePolicies = await _employeeTimePolicyRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeId)
                        && p.StartDate.Date <= toDate
                        && p.EndDate.Date >= fromDate)
            .Select(p => new TimePolicyInfo
            {
                EmployeeId = p.EmployeeId,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                TimePolicyName = useArabicNames ? p.TimePolicy.NameAr : p.TimePolicy.NameEn,
                ClockInLimit = p.TimePolicy.ClockInLimit,
                ClockOutLimit = p.TimePolicy.ClockOutLimit,
                BreakInLimit = p.TimePolicy.BreakInLimit,
                BreakOutLimit = p.TimePolicy.BreakOutLimit,
                TimeEventRule = p.TimePolicy.TimeEventRule,
                TimeMgmtStatus = p.TimePolicy.TimeMgmtStatus,
                OTMinLimit = p.TimePolicy.OTMinLimit
            })
            .ToListAsync();

        // Legacy lines 853-870. Note the commented-out `AbsenceHours == 0` filter (line 856): BOTH full-day
        // and partial-day absences are loaded here, and the two are separated later by AbsenceHours.
        var absences = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.StartDate.Date <= toDate
                        && a.EndDate.Date >= fromDate)
            .Select(a => new AbsenceInfo
            {
                EmployeeId = a.EmployeeId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                AbsenceTypeName = useArabicNames ? a.AbsenceType.NameAr : a.AbsenceType.NameEn,
                IsReturnDateRequired = a.AbsenceType.IsReturnDateRequired,
                ReturnDate = a.ReturnDate,
                AbsenceHours = a.AbsenceHours
            })
            .ToListAsync();

        // Legacy lines 872-890: the org assignments overlapping the window (NOT "as of today" on this
        // report -- the row's own day selects among them).
        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId)
                        && o.StartDate.Date <= toDate
                        && o.EndDate.Date >= fromDate)
            .Select(o => new OrgAssignmentInfo
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                PayOvertime = o.EmployeeSubGroup != null && o.EmployeeSubGroup.PayOvertime,
                PayrollCountry = o.PayrollArea == null || o.PayrollArea.Country == null
                    ? string.Empty
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn),
                WorkLocation = o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn),
                OrganizationUnitId = o.OrganizationUnitId
            })
            .OrderBy(o => o.StartDate)
            .ToListAsync();

        // Legacy lines 900-905.
        var substitutes = await _substituteDutyRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId)
                        && s.SubstituteDate.Date >= fromDate
                        && s.SubstituteDate.Date <= toDate)
            .Select(s => new { s.EmployeeId, s.SubstituteDate, s.DutyId })
            .ToListAsync();

        var substituteByEmployeeAndDate = substitutes
            .GroupBy(s => (s.EmployeeId, s.SubstituteDate.Date))
            .ToDictionary(g => g.Key, g => g.First().DutyId);

        var duties = await _dutyRepository.GetAll()
            .Select(d => new DutyInfo
            {
                Id = d.Id,
                IsNightShift = d.IsNightShift,
                IsWeeklyOff = d.IsWeeklyOff,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                FlexiStartTime = d.FlexiStartTime,
                FlexiEndTime = d.FlexiEndTime,
                BreakShiftId = d.BreakShiftId,
                BreakStartTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.StartTime,
                BreakEndTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.EndTime,
                IsBreakStartNextDay = d.BreakShift != null && d.BreakShift.IsBreakStartNextDay,
                IsBreakEndNextDay = d.BreakShift != null && d.BreakShift.IsBreakEndNextDay
            })
            .ToDictionaryAsync(d => d.Id);

        var rows = new List<LateEarlyLogReportRowDto>();

        // ---- STICKY SHIFT DEFECT: these six locals live OUTSIDE both loops in legacy (lines 907-914) and
        // are only reassigned when a Duty resolves, so they leak into a day that has no Duty. Reproduced.
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;
        DateTime actualShiftStartTime = default;
        DateTime actualShiftEndTime = default;
        DateTime breakShiftStartTime = default;
        DateTime breakShiftEndTime = default;
        var flexiStartTime = TimeSpan.Zero;

        foreach (var employee in employees)
        {
            // Legacy lines 920-922.
            var publicHolidays = (await _timeDataService.GetPublicHolidaysDatesListAsync(employee.Id, fromDate, toDate))
                .Select(h => h.HolidayDate.Date)
                .ToHashSet();

            // Legacy line 936: an employee with NO duty-period assignment in the window contributes no
            // rows at all, whatever their punches or absences say.
            if (!dutyPeriodsByEmployee.TryGetValue(employee.Id, out var employeeDutyPeriods))
            {
                continue;
            }

            eventsByEmployee.TryGetValue(employee.Id, out var employeeEvents);
            employeeEvents ??= new List<TimeEventInfo>();

            for (var dayDate = fromDate; dayDate <= toDate; dayDate = dayDate.AddDays(1))
            {
                // Legacy lines 928-934.
                var timePolicy = timePolicies.FirstOrDefault(p => p.EmployeeId == employee.Id
                                                                  && p.StartDate <= dayDate
                                                                  && p.EndDate >= dayDate);

                var org = orgAssignments.FirstOrDefault(o => o.EmployeeId == employee.Id
                                                             && o.StartDate <= dayDate
                                                             && o.EndDate >= dayDate);

                // Legacy lines 947-949: the LATEST-starting duty period covering the day wins.
                var dutyPeriod = employeeDutyPeriods
                    .Where(d => d.StartDate <= dayDate && d.EndDate >= dayDate)
                    .OrderByDescending(d => d.StartDate)
                    .FirstOrDefault();

                if (dutyPeriod == null)
                {
                    continue;
                }

                // Legacy lines 941-945: punches from the day before, the day and the day after are all
                // candidates (a night shift straddles midnight in both directions).
                var dayEvents = employeeEvents
                    .Where(e => e.TransDate.Date == dayDate
                                || e.TransDate.Date == dayDate.AddDays(1)
                                || e.TransDate.Date == dayDate.AddDays(-1))
                    .ToList();

                var weekDay = GetWeekDayName(dayDate, useArabicNames);

                var duty = await ResolveDutyAsync(
                    employee.Id,
                    dutyPeriod,
                    dayDate,
                    isRosterActive,
                    substituteByEmployeeAndDate,
                    duties);

                var isWeekOff = false;
                var isNightShift = false;
                var hasBreakShift = false;

                if (duty != null)
                {
                    // Legacy lines 992-1020.
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;

                    actualShiftStartTime = dayDate.Add(duty.StartTime);
                    actualShiftEndTime = duty.IsNightShift
                        ? dayDate.AddDays(1).Add(duty.EndTime)
                        : dayDate.Add(duty.EndTime);

                    breakShiftStartTime = duty.BreakStartTime == null
                        ? default
                        : (duty.IsBreakStartNextDay
                            ? dayDate.AddDays(1).Add(duty.BreakStartTime.Value)
                            : dayDate.Add(duty.BreakStartTime.Value));

                    breakShiftEndTime = duty.BreakEndTime == null
                        ? default
                        : ((duty.IsBreakStartNextDay || duty.IsBreakEndNextDay)
                            ? dayDate.AddDays(1).Add(duty.BreakEndTime.Value)
                            : dayDate.Add(duty.BreakEndTime.Value));

                    isNightShift = duty.IsNightShift;
                    isWeekOff = duty.IsWeeklyOff;
                    hasBreakShift = duty.BreakShiftId != null;

                    // Legacy line 1019 -- FlexiEndTime is read but, see below, never actually used.
                    flexiStartTime = duty.FlexiStartTime ?? TimeSpan.Zero;
                }

                var isPublicHoliday = publicHolidays.Contains(dayDate.Date);

                // Legacy lines 1035-1039: a full-day leave is an absence with AbsenceHours == 0.
                var fullDayLeave = absences.FirstOrDefault(a => a.EmployeeId == employee.Id
                                                                && a.StartDate <= dayDate
                                                                && a.EndDate >= dayDate
                                                                && a.AbsenceHours == 0);

                if (fullDayLeave != null)
                {
                    rows.Add(BuildLeaveRow(
                        employee,
                        org,
                        dayDate,
                        weekDay,
                        shiftStartTime,
                        shiftEndTime,
                        actualShiftStartTime,
                        actualShiftEndTime,
                        isPublicHoliday,
                        useArabicNames ? LeaveAr : LeaveEn,
                        fullDayLeave.AbsenceTypeName,
                        timePolicy));
                    continue;
                }

                // Legacy lines 1105-1118: an absence that has ENDED, requires a return date, has none, and
                // ends after the employee's latest recorded return date.
                var employeeAbsences = absences.Where(a => a.EmployeeId == employee.Id).ToList();
                var withReturnDate = employeeAbsences.Where(a => a.ReturnDate != default).ToList();
                var maxReturnDate = withReturnDate.Count > 0
                    ? withReturnDate.Max(a => a.ReturnDate)
                    : new DateTime(1900, 1, 1);

                var notReturnedFromLeave = employeeAbsences.Any(a => a.EndDate <= dayDate
                                                                     && a.ReturnDate == default
                                                                     && a.EndDate > maxReturnDate
                                                                     && a.IsReturnDateRequired
                                                                     && a.AbsenceHours == 0);

                if (notReturnedFromLeave)
                {
                    // Legacy lines 1120-1160: the same header block as the leave row, but with no leave
                    // type and NO planned/total/absence hours at all.
                    rows.Add(BuildLeaveRow(
                        employee,
                        org,
                        dayDate,
                        weekDay,
                        shiftStartTime,
                        shiftEndTime,
                        actualShiftStartTime,
                        actualShiftEndTime,
                        isPublicHoliday,
                        useArabicNames ? NoReturnDateAr : NoReturnDateEn,
                        leaveType: null,
                        timePolicy: timePolicy,
                        includeHours: false));
                    continue;
                }

                // Legacy line 1181: no time policy covering the day means NO ROW for that day.
                if (timePolicy == null)
                {
                    continue;
                }

                rows.Add(BuildAttendanceRow(
                    employee,
                    org,
                    timePolicy,
                    dayEvents,
                    absences,
                    dayDate,
                    weekDay,
                    shiftStartTime,
                    shiftEndTime,
                    actualShiftStartTime,
                    actualShiftEndTime,
                    breakShiftStartTime,
                    breakShiftEndTime,
                    flexiStartTime,
                    isNightShift,
                    isWeekOff,
                    isPublicHoliday,
                    hasBreakShift,
                    useArabicNames));
            }
        }

        // Legacy set the "Oid" cell to `table.Rows.Count + 1`, a 1-based ROW SEQUENCE number rather than a
        // database id (line 1043). Reproduced.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The full-day-leave row (legacy lines 1040-1103) and, with <paramref name="includeHours"/> false,
    /// the no-return-date row (lines 1120-1160), which share the same header block.
    /// </summary>
    private static LateEarlyLogReportRowDto BuildLeaveRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        DateTime actualShiftStartTime,
        DateTime actualShiftEndTime,
        bool isPublicHoliday,
        string status,
        string leaveType,
        TimePolicyInfo timePolicy,
        bool includeHours = true)
    {
        var row = new LateEarlyLogReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AccessId = employee.AccessId,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            // Legacy explicitly blanks these on both branches (lines 1070-1076).
            PunchInAccessUnit = string.Empty,
            PunchOutAccessUnit = string.Empty,
            PunchInLocation = string.Empty,
            PunchOutLocation = string.Empty,
            Status = status,
            LeaveType = leaveType,
            TimePolicyName = timePolicy?.TimePolicyName ?? string.Empty,
            WorkLocationName = org?.WorkLocation ?? string.Empty,
            CountryName = org?.PayrollCountry ?? string.Empty
        };

        if (!includeHours)
        {
            return row;
        }

        // Legacy lines 1085-1101: the whole planned shift counts as absence on a full-day leave, and
        // nothing at all is reported on a public holiday.
        var plannedHours = TimeSpan.Zero;
        if (actualShiftStartTime != default && actualShiftEndTime != default)
        {
            plannedHours = actualShiftEndTime.Subtract(actualShiftStartTime);
        }

        if (!isPublicHoliday)
        {
            row.PlannedHours = plannedHours;
            row.TotalHours = TimeSpan.Zero;
            row.AbsenceHours = plannedHours;
        }

        return row;
    }

    /// <summary>
    /// The normal attendance row -- legacy lines 1180-1544: punch selection, break selection, early/late
    /// deltas, status and the planned/total/overtime/absence hour arithmetic.
    /// </summary>
    private static LateEarlyLogReportRowDto BuildAttendanceRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        TimePolicyInfo timePolicy,
        List<TimeEventInfo> dayEvents,
        List<AbsenceInfo> absences,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        DateTime actualShiftStartTime,
        DateTime actualShiftEndTime,
        DateTime breakShiftStartTime,
        DateTime breakShiftEndTime,
        TimeSpan flexiStartTime,
        bool isNightShift,
        bool isWeekOff,
        bool isPublicHoliday,
        bool hasBreakShift,
        bool useArabicNames)
    {
        var row = new LateEarlyLogReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AccessId = employee.AccessId,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime
        };

        // ---- Punch selection (legacy lines 1216-1275).
        List<TimeEventInfo> punchInCandidates;
        List<TimeEventInfo> punchOutCandidates;

        if (timePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            // LEGACY DEFECT REPRODUCED (lines 1224-1225): the flexi END window is computed from
            // flexiSTARTTime, not FlexiEndTime, so on a non-night shift the window collapses to a single
            // instant and effectively selects nothing.
            var flexiStartWindow = dayDate.Add(flexiStartTime);
            var flexiEndWindow = isNightShift
                ? dayDate.Add(flexiStartTime).AddDays(1)
                : dayDate.Add(flexiStartTime);

            // Also note: NO TransType filter on the flexi branch -- both lists are the same set.
            punchInCandidates = dayEvents
                .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                .ToList();
            punchOutCandidates = punchInCandidates.ToList();
        }
        else
        {
            var clockInStart = dayDate.Add(shiftStartTime).AddMinutes(-timePolicy.ClockInLimit);
            var clockInEnd = dayDate.Add(shiftStartTime).AddMinutes(timePolicy.ClockInLimit);

            var shiftEndBase = isNightShift ? dayDate.AddDays(1).Add(shiftEndTime) : dayDate.Add(shiftEndTime);
            var clockOutStart = shiftEndBase.AddMinutes(-timePolicy.ClockOutLimit);
            var clockOutEnd = shiftEndBase.AddMinutes(timePolicy.ClockOutLimit);

            // Legacy lines 1238-1246: on a midnight-to-midnight week-off duty the window becomes the
            // whole calendar day, so any punch on a week off is still picked up.
            if (isWeekOff && shiftStartTime == TimeSpan.Zero && shiftEndTime == TimeSpan.Zero)
            {
                clockInStart = dayDate.Date;
                clockInEnd = dayDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                clockOutStart = clockInStart;
                clockOutEnd = clockInEnd;
            }

            punchInCandidates = dayEvents
                .Where(e => e.TransTypeCode == PunchInTransTypeCode
                            && clockInStart <= e.TransDateTime
                            && clockInEnd >= e.TransDateTime)
                .ToList();

            punchOutCandidates = dayEvents
                .Where(e => e.TransTypeCode == PunchOutTransTypeCode
                            && clockOutStart <= e.TransDateTime
                            && clockOutEnd >= e.TransDateTime)
                .ToList();
        }

        var punchIn = SelectIn(punchInCandidates, timePolicy.TimeEventRule);
        var punchOut = SelectOut(punchOutCandidates, timePolicy.TimeEventRule);

        // ---- Break punch selection (legacy lines 1277-1360). Only when the duty has a break shift.
        TimeEventInfo breakIn = null;
        TimeEventInfo breakOut = null;

        if (hasBreakShift)
        {
            List<TimeEventInfo> breakInCandidates;
            List<TimeEventInfo> breakOutCandidates;

            if (timePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
            {
                var flexiStartWindow = dayDate.Add(flexiStartTime);
                var flexiEndWindow = isNightShift
                    ? dayDate.Add(flexiStartTime).AddDays(1)
                    : dayDate.Add(flexiStartTime);

                breakInCandidates = dayEvents
                    .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                    .ToList();
                breakOutCandidates = breakInCandidates.ToList();
            }
            else
            {
                // LEGACY QUIRK REPRODUCED (lines 1300-1304): the break-IN window is built from the break
                // shift's END time and the break-OUT window from its START time -- i.e. you punch OUT at
                // the start of the break and back IN at its end. Note also that the break-in window is
                // built on dayDate even for a night shift, unlike the break-out window.
                var breakInStart = dayDate.Add(breakShiftEndTime.TimeOfDay).AddMinutes(-timePolicy.BreakInLimit);
                var breakInEnd = dayDate.Add(breakShiftEndTime.TimeOfDay).AddMinutes(timePolicy.BreakInLimit);

                var breakOutBase = isNightShift
                    ? dayDate.AddDays(1).Add(breakShiftStartTime.TimeOfDay)
                    : dayDate.Add(breakShiftStartTime.TimeOfDay);
                var breakOutStart = breakOutBase.AddMinutes(-timePolicy.BreakOutLimit);
                var breakOutEnd = breakOutBase.AddMinutes(timePolicy.BreakOutLimit);

                if (isWeekOff
                    && breakShiftStartTime.TimeOfDay == TimeSpan.Zero
                    && breakShiftEndTime.TimeOfDay == TimeSpan.Zero)
                {
                    breakInStart = dayDate.Date;
                    breakInEnd = dayDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                    breakOutStart = breakInStart;
                    breakOutEnd = breakInEnd;
                }

                breakOutCandidates = dayEvents
                    .Where(e => e.TransTypeCode == PunchOutTransTypeCode
                                && breakOutStart <= e.TransDateTime
                                && breakOutEnd >= e.TransDateTime)
                    .ToList();

                breakInCandidates = dayEvents
                    .Where(e => e.TransTypeCode == PunchInTransTypeCode
                                && breakInStart <= e.TransDateTime
                                && breakInEnd >= e.TransDateTime)
                    .ToList();
            }

            // Legacy lines 1334-1358: for the BREAK pair the roles of the four rules are mirrored --
            // FirstInLastOut takes the LAST break-out, LastInFirstOut the LAST break-in, and so on.
            breakOut = timePolicy.TimeEventRule switch
            {
                TimeEventRule.FirstInFirstOut => breakOutCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.FirstInLastOut => breakOutCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.LastInFirstOut => breakOutCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.LastInLastOut => breakOutCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                _ => null
            };

            breakIn = timePolicy.TimeEventRule switch
            {
                TimeEventRule.FirstInFirstOut => breakInCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.FirstInLastOut => breakInCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.LastInFirstOut => breakInCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                TimeEventRule.LastInLastOut => breakInCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
                _ => null
            };
        }

        // ---- Break In / Break Out cells (legacy lines 1362-1375).
        DateTime breakOutAt = default;
        DateTime breakInAt = default;

        if (breakOut != null)
        {
            breakOutAt = breakOut.TransDateTime;
            row.BreakOut = breakOut.TransTime.TimeOfDay;
        }

        if (breakIn != null)
        {
            breakInAt = breakIn.TransDateTime;
            row.BreakIn = breakIn.TransTime.TimeOfDay;
        }

        // ---- Sign In / Early In / Late In (legacy lines 1377-1401).
        DateTime actualIn = default;
        DateTime actualOut = default;

        if (punchIn != null)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = punchIn.TransTime.TimeOfDay;

            if (!isWeekOff)
            {
                if (actualShiftStartTime > punchIn.TransDateTime)
                {
                    row.EarlyIn = actualShiftStartTime.Subtract(punchIn.TransDateTime);
                }
                else if (actualShiftStartTime < punchIn.TransDateTime)
                {
                    row.LateIn = punchIn.TransDateTime.Subtract(actualShiftStartTime);
                }
            }

            row.PunchInLocation = punchIn.WorkLocation;
            row.PunchInAccessUnit = punchIn.AccessUnit;
        }

        // ---- Sign Out / Early Out / Late Out (legacy lines 1403-1425).
        if (punchOut != null)
        {
            actualOut = punchOut.TransDateTime;
            row.SignOut = punchOut.TransTime.TimeOfDay;

            if (!isWeekOff)
            {
                if (actualShiftEndTime > punchOut.TransDateTime)
                {
                    row.EarlyOut = actualShiftEndTime.Subtract(punchOut.TransDateTime);
                }

                if (actualShiftEndTime < punchOut.TransDateTime)
                {
                    row.LateOut = punchOut.TransDateTime.Subtract(actualShiftEndTime);
                }
            }

            row.PunchOutLocation = punchOut.WorkLocation;
            row.PunchOutAccessUnit = punchOut.AccessUnit;
        }

        // ---- Status (legacy lines 1427-1450).
        if (isWeekOff)
        {
            row.Status = useArabicNames ? WeekOffAr : WeekOffEn;
        }
        else if (isPublicHoliday)
        {
            row.Status = useArabicNames ? PublicHolidayAr : PublicHolidayEn;
        }
        else if (punchOutCandidates.Count == 0 || punchInCandidates.Count == 0)
        {
            // Legacy built this literal in English only, even in the Arabic aspect. Reproduced.
            var missed = string.Empty;
            if (punchInCandidates.Count == 0)
            {
                missed = "Missed In";
            }

            if (punchOutCandidates.Count == 0)
            {
                missed = missed + (missed == string.Empty ? string.Empty : " ") + "Missed Out";
            }

            row.Status = missed;
        }
        else
        {
            row.Status = string.Empty;
        }

        row.TimePolicyName = timePolicy.TimePolicyName;
        row.WorkLocationName = org?.WorkLocation ?? string.Empty;
        row.CountryName = org?.PayrollCountry ?? string.Empty;

        // ---- Partial leave (legacy lines 1452-1465): an absence covering the day with NON-zero hours.
        var partialLeave = absences.FirstOrDefault(a => a.EmployeeId == employee.Id
                                                        && a.StartDate <= dayDate
                                                        && a.EndDate >= dayDate
                                                        && a.AbsenceHours != 0);
        if (partialLeave != null)
        {
            row.LeaveType = partialLeave.AbsenceTypeName;

            var hours = (int)partialLeave.AbsenceHours;
            var minutes = (int)((partialLeave.AbsenceHours - hours) * 60);
            row.PartialLeaveHours = new TimeSpan(hours, minutes, 0);
        }

        // ---- Hours arithmetic (legacy lines 1467-1540).
        var breakShiftHours = TimeSpan.Zero;
        var breakHours = TimeSpan.Zero;
        var plannedHours = TimeSpan.Zero;
        var totalHours = TimeSpan.Zero;
        var overtimeHours = TimeSpan.Zero;
        var absenceHours = TimeSpan.Zero;

        if (hasBreakShift)
        {
            breakShiftHours = breakShiftEndTime.Subtract(breakShiftStartTime);
        }

        if (breakInAt != default && breakOutAt != default)
        {
            breakHours = breakInAt.Subtract(breakOutAt);
            row.BreakHours = breakHours;
        }

        if (actualShiftStartTime != default && actualShiftEndTime != default && !isWeekOff)
        {
            plannedHours = actualShiftEndTime.Subtract(actualShiftStartTime).Subtract(breakShiftHours);
        }

        if (actualIn != default && actualOut != default)
        {
            totalHours = actualOut.Subtract(actualIn).Subtract(breakHours);
        }

        if (totalHours > plannedHours && !isWeekOff)
        {
            overtimeHours = totalHours.Subtract(plannedHours);
        }

        // Legacy lines 1508-1519: overtime is dropped entirely when the employee's sub-group does not pay
        // overtime, and swallowed when it does not exceed the policy's OTMinLimit MINUTES.
        if (overtimeHours != TimeSpan.Zero && org != null)
        {
            if (org.PayOvertime)
            {
                var minimumOvertime = new TimeSpan(0, 0, (int)timePolicy.OTMinLimit, 0);
                if (overtimeHours <= minimumOvertime)
                {
                    overtimeHours = TimeSpan.Zero;
                }
            }
            else
            {
                overtimeHours = TimeSpan.Zero;
            }
        }

        if (totalHours < plannedHours && !isWeekOff)
        {
            absenceHours = plannedHours.Subtract(totalHours);
        }

        // Legacy lines 1521-1538: zero-valued duration cells are left EMPTY, not written as 00:00, and on
        // a public holiday only the total hours are reported.
        if (!isPublicHoliday)
        {
            if (plannedHours != TimeSpan.Zero)
            {
                row.PlannedHours = plannedHours;
            }

            if (totalHours != TimeSpan.Zero)
            {
                row.TotalHours = totalHours;
            }

            if (absenceHours != TimeSpan.Zero)
            {
                row.AbsenceHours = absenceHours;
            }
        }
        else if (totalHours != TimeSpan.Zero)
        {
            row.TotalHours = totalHours;
        }

        if (overtimeHours != TimeSpan.Zero)
        {
            row.OvertimeHours = overtimeHours;
        }

        return row;
    }

    private static TimeEventInfo SelectIn(List<TimeEventInfo> candidates, TimeEventRule rule)
    {
        return rule switch
        {
            TimeEventRule.FirstInFirstOut or TimeEventRule.FirstInLastOut =>
                candidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
            TimeEventRule.LastInFirstOut or TimeEventRule.LastInLastOut =>
                candidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
            // Legacy had no else branch: TimeEventRule.NA selects no punch at all.
            _ => null
        };
    }

    private static TimeEventInfo SelectOut(List<TimeEventInfo> candidates, TimeEventRule rule)
    {
        return rule switch
        {
            TimeEventRule.FirstInFirstOut or TimeEventRule.LastInFirstOut =>
                candidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
            TimeEventRule.FirstInLastOut or TimeEventRule.LastInLastOut =>
                candidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
            _ => null
        };
    }

    /// <summary>
    /// Legacy lines 691-698: the day name, with a hard-coded Arabic literal table rather than a resource
    /// lookup. Reproduced verbatim.
    /// </summary>
    private static string GetWeekDayName(DateTime dayDate, bool useArabicNames)
    {
        if (!useArabicNames)
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
    /// Ports legacy lines 878-890: with the roster off, the employee's substitute duty for that exact date
    /// wins over the duty period's day-of-week Duty; with the roster on, resolution is delegated entirely
    /// to the roster service.
    /// </summary>
    private async Task<DutyInfo> ResolveDutyAsync(
        int employeeId,
        DutyPeriodInfo dutyPeriod,
        DateTime dayDate,
        bool isRosterActive,
        Dictionary<(int, DateTime), int> substituteByEmployeeAndDate,
        Dictionary<int, DutyInfo> duties)
    {
        if (isRosterActive)
        {
            // throwException: false -- one misconfigured duty period must not blank the whole report.
            var rosterDuty = await _rosterService.GetRosterDutyAsync(employeeId, dayDate, null, false);
            if (rosterDuty == null)
            {
                return null;
            }

            return duties.TryGetValue(rosterDuty.Id, out var resolved) ? resolved : null;
        }

        if (substituteByEmployeeAndDate.TryGetValue((employeeId, dayDate.Date), out var substituteDutyId)
            && duties.TryGetValue(substituteDutyId, out var substituteDuty))
        {
            return substituteDuty;
        }

        var dutyId = dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDutyId,
            DayOfWeek.Monday => dutyPeriod.MondayDutyId,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDutyId,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDutyId,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDutyId,
            DayOfWeek.Friday => dutyPeriod.FridayDutyId,
            _ => dutyPeriod.SaturdayDutyId
        };

        return dutyId.HasValue && duties.TryGetValue(dutyId.Value, out var weekdayDuty) ? weekdayDuty : null;
    }

    /// <summary>
    /// Fills the seven legacy OrgLevel columns after materialization. Shared by the read and the export so
    /// the two can never drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<LateEarlyLogReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 722-765). The technical Oid row-sequence
    /// column is omitted because it carries no information the reader does not already have.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("AccessId", r => r.AccessId),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Date", r => r.Date),
            new("WeekDay", r => r.WeekDay),
            new("ShiftStart", r => r.ShiftStart),
            new("ShiftEnd", r => r.ShiftEnd),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("BreakOut", r => r.BreakOut),
            new("BreakIn", r => r.BreakIn),
            new("BreakHours", r => r.BreakHours),
            new("EarlyIn", r => r.EarlyIn),
            new("EarlyOut", r => r.EarlyOut),
            new("LateIn", r => r.LateIn),
            new("LateOut", r => r.LateOut),
            new("TotalHours", r => r.TotalHours),
            new("PlannedHours", r => r.PlannedHours),
            new("OvertimeHours", r => r.OvertimeHours),
            new("AbsenceHours", r => r.AbsenceHours),
            new("Status", r => r.Status),
            new("LeaveType", r => r.LeaveType),
            new("PartialLeaveHours", r => r.PartialLeaveHours),
            new("PunchInAccessUnit", r => r.PunchInAccessUnit),
            new("PunchOutAccessUnit", r => r.PunchOutAccessUnit),
            new("TimePolicy", r => r.TimePolicyName),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName),
            new("PunchInLocation", r => r.PunchInLocation),
            new("PunchOutLocation", r => r.PunchOutLocation)
        };
    }

    private class EmployeeInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string FullName { get; set; }

        public string AccessId { get; set; }
    }

    private class TimeEventInfo
    {
        public int EmployeeId { get; set; }

        public string TransTypeCode { get; set; }

        public DateTime TransDate { get; set; }

        public DateTime TransTime { get; set; }

        /// <summary>Legacy <c>MyTimeEvent.TransDateTime</c> -- the punch date plus the punch time-of-day.</summary>
        public DateTime TransDateTime { get; set; }

        public string WorkLocation { get; set; }

        public string AccessUnit { get; set; }
    }

    private class DutyPeriodInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? SundayDutyId { get; set; }

        public int? MondayDutyId { get; set; }

        public int? TuesdayDutyId { get; set; }

        public int? WednesdayDutyId { get; set; }

        public int? ThursdayDutyId { get; set; }

        public int? FridayDutyId { get; set; }

        public int? SaturdayDutyId { get; set; }
    }

    private class DutyInfo
    {
        public int Id { get; set; }

        public bool IsNightShift { get; set; }

        public bool IsWeeklyOff { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public TimeSpan? FlexiStartTime { get; set; }

        public TimeSpan? FlexiEndTime { get; set; }

        public int? BreakShiftId { get; set; }

        public TimeSpan? BreakStartTime { get; set; }

        public TimeSpan? BreakEndTime { get; set; }

        public bool IsBreakStartNextDay { get; set; }

        public bool IsBreakEndNextDay { get; set; }
    }

    private class TimePolicyInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string TimePolicyName { get; set; }

        public double ClockInLimit { get; set; }

        public double ClockOutLimit { get; set; }

        public double BreakInLimit { get; set; }

        public double BreakOutLimit { get; set; }

        public TimeEventRule TimeEventRule { get; set; }

        public TimeMgmtStatus TimeMgmtStatus { get; set; }

        public double OTMinLimit { get; set; }
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string AbsenceTypeName { get; set; }

        public bool IsReturnDateRequired { get; set; }

        public DateTime ReturnDate { get; set; }

        public double AbsenceHours { get; set; }
    }

    private class OrgAssignmentInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool PayOvertime { get; set; }

        public string PayrollCountry { get; set; }

        public string WorkLocation { get; set; }

        public long? OrganizationUnitId { get; set; }
    }
}
