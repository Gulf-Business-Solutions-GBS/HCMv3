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
/// Custom040 Late/Early Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom040.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 622-1400).
///
/// WHY THIS IS A SEPARATE SERVICE AND NOT A SUBCLASS OF <see cref="LateEarlyLogReportAppService"/>.
/// Custom040 shares the base's skeleton -- same six materialized input sets, same day loop, same three row
/// shapes -- but it differs in almost every STEP of the per-day computation: the punch-window construction,
/// the early/late arithmetic, the planned/total-hours arithmetic, the status vocabulary and the column set.
/// A template-method base would need a virtual hook at each of those points and would be left with no
/// shared body worth inheriting, while making the already-shipped base service fragile for the other
/// customer variants. What genuinely IS shared is already shared: <see cref="GridReportAppServiceBase"/>
/// (RLS, execution log, Arabic-name switch, export tail), <see cref="EmployeeOrgLevelNameResolver"/> and
/// the <c>GridReportInputBase</c> filter contract.
///
/// WHAT CUSTOM040 IS: an OLDER, BREAK-LESS FORK of the base control. It predates the base's break-shift
/// support and its day-anchored "actual shift" date-times. Concretely, against the base:
/// <list type="number">
/// <item><description>NO break columns and NO break punch selection at all (the entire
/// "BreakIn &amp; BreakOut From TimeEvent" region is absent). The two break-shift locals are still assigned
/// (legacy lines 349-350) but are never read again -- dead code, not ported.</description></item>
/// <item><description>NO <c>actualShiftStart/EndTime</c>: the shift bounds stay bare times and every
/// comparison against a punch is done on TIME-OF-DAY only.</description></item>
/// <item><description>The flexi window is built from <c>FlexiStartTime</c> AND <c>FlexiEndTime</c>
/// (lines 546-547), i.e. WITHOUT the base's flexi-end defect.</description></item>
/// <item><description>A different STATUS vocabulary -- see <see cref="BuildAttendanceRow"/>.</description></item>
/// </list>
///
/// STICKY SHIFT DEFECT (legacy lines 245-248, REPRODUCED): the two shift locals are declared OUTSIDE both
/// loops and are only assigned when a Duty resolves, so a day with no Duty reports the LAST successfully
/// resolved day's shift bounds. Same defect as the base, one variable pair smaller.
///
/// AGREED DIVERGENCE: RLS is applied here exactly as legacy applied it (line 752).
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1401-2401).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom040)]
public class LateEarlyLogCustom040ReportAppService : GridReportAppServiceBase,
    ILateEarlyLogCustom040ReportAppService
{
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string NoReturnDateEn = "No Return Date";

    private const string NoReturnDateAr = "لا تاريخ العودة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    /// <summary>
    /// The two Custom040-only status literals (legacy lines 682 and 686). English-only in legacy, even in
    /// the Arabic aspect -- reproduced.
    /// </summary>
    private const string PresentWithSinglePunch = "Present with Single Punch";

    private const string NoPunchFound = "No Punch Found";

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

    public LateEarlyLogCustom040ReportAppService(
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

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom040";

    protected override string ScreenCaption => "Late Early Log (Custom 040)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom040ReportRowDto>> GetLateEarlyLogCustom040Report(
        GetLateEarlyLogCustom040ReportInput input)
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

        return new PagedResultDto<LateEarlyLogCustom040ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom040ReportToExcel(GetLateEarlyLogCustom040ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLogCustom040.xlsx",
            rows,
            GetExportColumns(),
            BuildLateEarlyParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (lines 604-620): payroll area on top of the employee code and the
    /// date range.
    /// </summary>
    private string BuildLateEarlyParameterString(GetLateEarlyLogCustom040ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export, so
    /// the export can never honour different filters from the grid. Ports legacy <c>Table1()</c>'s
    /// "Apply Filter", "Load Initial Data" and "Check For Each Data" regions (lines 750-1396).
    ///
    /// IN-MEMORY BY NECESSITY, for the same reason as the base: the punch windows depend on the day's
    /// resolved Duty, the policy limits and the night-shift/week-off flags, and the Duty itself depends on
    /// substitute duties and the roster. None of that is expressible in SQL, so the window-bounded input
    /// sets are materialized once and the rows generated here, then handed back as an
    /// <c>IQueryable</c> so paging, sorting and the export share one path.
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom040ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom040ReportInput input)
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
            return new List<LateEarlyLogCustom040ReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy lines 752-776: ACTIVE employees only (hard-coded), optionally restricted to a payroll
        // area AS OF TODAY, then the comma-split code list, then RLS.
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
            return new List<LateEarlyLogCustom040ReportRowDto>().AsQueryable();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Legacy lines 785-800: the extra day lets a night shift's next-morning clock-out be found.
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

        // Legacy lines 802-816.
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
                TimeEventRule = p.TimePolicy.TimeEventRule,
                TimeMgmtStatus = p.TimePolicy.TimeMgmtStatus,
                OTMinLimit = p.TimePolicy.OTMinLimit
            })
            .ToListAsync();

        // Legacy lines 838-856. The `AbsenceHours == 0` filter is commented out there too, so BOTH
        // full-day and partial-day absences are loaded and separated later by AbsenceHours.
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
                FlexiEndTime = d.FlexiEndTime
            })
            .ToDictionaryAsync(d => d.Id);

        var rows = new List<LateEarlyLogCustom040ReportRowDto>();

        // ---- STICKY SHIFT DEFECT: these two locals live OUTSIDE both loops in legacy (lines 245-246) and
        // are only reassigned when a Duty resolves, so they leak into a day that has no Duty. Reproduced.
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;

        foreach (var employee in employees)
        {
            var publicHolidays = (await _timeDataService.GetPublicHolidaysDatesListAsync(employee.Id, fromDate, toDate))
                .Select(h => h.HolidayDate.Date)
                .ToHashSet();

            // Legacy line 296 (`dsCount > 0`): an employee with NO duty-period assignment in the window
            // contributes no rows at all, whatever their punches or absences say.
            if (!dutyPeriodsByEmployee.TryGetValue(employee.Id, out var employeeDutyPeriods))
            {
                continue;
            }

            eventsByEmployee.TryGetValue(employee.Id, out var employeeEvents);
            employeeEvents ??= new List<TimeEventInfo>();

            for (var dayDate = fromDate; dayDate <= toDate; dayDate = dayDate.AddDays(1))
            {
                var timePolicy = timePolicies.FirstOrDefault(p => p.EmployeeId == employee.Id
                                                                  && p.StartDate <= dayDate
                                                                  && p.EndDate >= dayDate);

                var org = orgAssignments.FirstOrDefault(o => o.EmployeeId == employee.Id
                                                             && o.StartDate <= dayDate
                                                             && o.EndDate >= dayDate);

                // LEGACY DIVERGENCE FROM THE BASE (line 302): Custom040 DROPPED the base's
                // `.OrderByDescending(x => x.StartDate)`, so when two duty periods overlap the day it is
                // the first one the query happened to return that wins rather than the latest-starting
                // one. Reproduced; the ordering is left to the query, exactly as unspecified as legacy's.
                var dutyPeriod = employeeDutyPeriods
                    .FirstOrDefault(d => d.StartDate <= dayDate && d.EndDate >= dayDate);

                if (dutyPeriod == null)
                {
                    continue;
                }

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

                if (duty != null)
                {
                    // Legacy lines 346-352. Note what is NOT here compared with the base: no
                    // actualShiftStart/End, and the two break-shift locals legacy still assigns are never
                    // read again, so they are omitted.
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;
                    isNightShift = duty.IsNightShift;
                    isWeekOff = duty.IsWeeklyOff;
                }

                var isPublicHoliday = publicHolidays.Contains(dayDate.Date);

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
                        isNightShift,
                        isPublicHoliday,
                        useArabicNames ? LeaveAr : LeaveEn,
                        fullDayLeave.AbsenceTypeName,
                        timePolicy));
                    continue;
                }

                // Legacy lines 496-509: an absence that has ENDED, requires a return date, has none, and
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
                    rows.Add(BuildLeaveRow(
                        employee,
                        org,
                        dayDate,
                        weekDay,
                        shiftStartTime,
                        shiftEndTime,
                        isNightShift,
                        isPublicHoliday,
                        useArabicNames ? NoReturnDateAr : NoReturnDateEn,
                        leaveType: null,
                        timePolicy: timePolicy,
                        includeHours: false));
                    continue;
                }

                // Legacy line 527: no time policy covering the day means NO ROW for that day.
                if (timePolicy == null)
                {
                    continue;
                }

                rows.Add(BuildAttendanceRow(
                    employee,
                    org,
                    timePolicy,
                    duty,
                    dayEvents,
                    absences,
                    dayDate,
                    weekDay,
                    shiftStartTime,
                    shiftEndTime,
                    isNightShift,
                    isWeekOff,
                    isPublicHoliday,
                    useArabicNames));
            }
        }

        // Legacy set the "Oid" cell to `table.Rows.Count + 1` -- a 1-based ROW SEQUENCE number, not a
        // database id (line 439). Reproduced.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The full-day-leave row (legacy lines 434-494) and, with <paramref name="includeHours"/> false, the
    /// no-return-date row (lines 511-525), which share the same header block.
    /// </summary>
    private static LateEarlyLogCustom040ReportRowDto BuildLeaveRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        bool isPublicHoliday,
        string status,
        string leaveType,
        TimePolicyInfo timePolicy,
        bool includeHours = true)
    {
        var row = new LateEarlyLogCustom040ReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AccessId = employee.AccessId,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            // Legacy explicitly blanks these on both branches.
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

        // Legacy lines 480-492: the whole planned shift counts as absence on a full-day leave, and nothing
        // at all is reported on a public holiday.
        var plannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShift);

        if (!isPublicHoliday)
        {
            row.PlannedHours = plannedHours;
            row.TotalHours = TimeSpan.Zero;
            row.AbsenceHours = plannedHours;
        }

        return row;
    }

    /// <summary>
    /// The normal attendance row -- legacy lines 528-1396.
    /// </summary>
    private static LateEarlyLogCustom040ReportRowDto BuildAttendanceRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        TimePolicyInfo timePolicy,
        DutyInfo duty,
        List<TimeEventInfo> dayEvents,
        List<AbsenceInfo> absences,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        bool isWeekOff,
        bool isPublicHoliday,
        bool useArabicNames)
    {
        var row = new LateEarlyLogCustom040ReportRowDto
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

        // ---- Punch selection (legacy lines 541-597).
        List<TimeEventInfo> punchInCandidates;
        List<TimeEventInfo> punchOutCandidates;

        if (timePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            // Legacy lines 546-547 dereference `duty` UNCONDITIONALLY here, but `duty` is only assigned
            // inside `if (duty != null)` above -- so a FlexiTime employee on a day with no resolvable Duty
            // crashes the whole report with a NullReferenceException. That is a crash, not a data
            // difference, so it is NOT reproduced: a null Duty yields an empty flexi window (and therefore
            // no punches), which is what the row would have shown had legacy not thrown.
            var flexiStart = duty?.FlexiStartTime ?? TimeSpan.Zero;
            var flexiEnd = duty?.FlexiEndTime ?? TimeSpan.Zero;

            var flexiStartWindow = dayDate.Add(flexiStart);
            var flexiEndWindow = isNightShift
                ? dayDate.Add(flexiEnd).AddDays(1)
                : dayDate.Add(flexiEnd);

            // NO TransType filter on the flexi branch -- both lists are literally the same set, exactly as
            // in the base control.
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

            // Legacy lines 563-571: on a midnight-to-midnight week-off duty the window becomes the whole
            // calendar day, so a punch on a week off is still picked up.
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

        // ---- Sign In / Early In / Late In (legacy lines 601-630).
        DateTime actualIn = default;
        DateTime actualOut = default;

        if (punchIn != null)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = punchIn.TransTime.TimeOfDay;

            if (!isWeekOff)
            {
                // CUSTOM040: TIME-OF-DAY comparison (lines 610/615), where the base compares full
                // date-times. On a night shift whose punch falls on the next calendar day this measures
                // the punch against the wrong day and can report a large "Early In" for a late arrival.
                // Reproduced and pinned by a test. Note also that legacy uses two independent `if`s here
                // rather than the base's `if/else if` -- equivalent, since the conditions are exclusive.
                var punchTimeOfDay = punchIn.TransTime.TimeOfDay;

                if (shiftStartTime > punchTimeOfDay)
                {
                    row.EarlyIn = shiftStartTime - punchTimeOfDay;
                }

                if (shiftStartTime < punchTimeOfDay)
                {
                    row.LateIn = punchTimeOfDay - shiftStartTime;
                }
            }

            row.PunchInLocation = punchIn.WorkLocation;
            row.PunchInAccessUnit = punchIn.AccessUnit;
        }

        // ---- Sign Out / Early Out / Late Out (legacy lines 632-665).
        if (punchOut != null)
        {
            actualOut = punchOut.TransDateTime;
            row.SignOut = punchOut.TransTime.TimeOfDay;

            if (!isWeekOff)
            {
                var punchTimeOfDay = punchOut.TransTime.TimeOfDay;

                if (shiftEndTime > punchTimeOfDay)
                {
                    row.EarlyOut = shiftEndTime - punchTimeOfDay;
                }

                if (shiftEndTime < punchTimeOfDay)
                {
                    row.LateOut = punchTimeOfDay - shiftEndTime;
                }
            }

            row.PunchOutLocation = punchOut.WorkLocation;
            row.PunchOutAccessUnit = punchOut.AccessUnit;
        }

        // ---- Status (legacy lines 667-696). THE SIGNATURE CUSTOM040 CHANGE: the base's
        // "Missed In"/"Missed Out" pair is replaced by this customer's own two-verdict vocabulary.
        if (isWeekOff)
        {
            row.Status = useArabicNames ? WeekOffAr : WeekOffEn;
        }
        else if (isPublicHoliday)
        {
            row.Status = useArabicNames ? PublicHolidayAr : PublicHolidayEn;
        }
        else if (punchInCandidates.Count > 0 ^ punchOutCandidates.Count > 0)
        {
            row.Status = PresentWithSinglePunch;
        }
        else if (punchInCandidates.Count == 0 && punchOutCandidates.Count == 0)
        {
            row.Status = NoPunchFound;
        }
        else
        {
            row.Status = string.Empty;
        }

        row.TimePolicyName = timePolicy.TimePolicyName;
        row.WorkLocationName = org?.WorkLocation ?? string.Empty;
        row.CountryName = org?.PayrollCountry ?? string.Empty;

        // ---- Partial leave: an absence covering the day with NON-zero hours.
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

        // ---- Hours arithmetic (legacy lines 712-800). NO break deduction anywhere, and planned hours
        // come from the bare shift times rather than day-anchored date-times.
        var plannedHours = TimeSpan.Zero;
        var totalHours = TimeSpan.Zero;
        var overtimeHours = TimeSpan.Zero;
        var absenceHours = TimeSpan.Zero;

        if (!isWeekOff)
        {
            plannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShift);
        }

        if (actualIn != default && actualOut != default)
        {
            totalHours = actualOut.Subtract(actualIn);
        }

        if (totalHours > plannedHours && !isWeekOff)
        {
            overtimeHours = totalHours.Subtract(plannedHours);
        }

        // Overtime is dropped entirely when the sub-group does not pay overtime, and swallowed when it
        // does not exceed the policy's OTMinLimit MINUTES. Unchanged from the base.
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

        // Zero-valued duration cells are left EMPTY, not written as 00:00, and on a public holiday only
        // the total hours are reported.
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

    /// <summary>
    /// Legacy's planned-shift length: <c>end - start</c> normally but <c>start - end</c> on a night shift
    /// (e.g. lines 720-721). Both operands are bare times on a shared 1900-01-01 anchor in legacy, so a
    /// 22:00-06:00 night shift yields SIXTEEN hours rather than eight. Reproduced and pinned by a test.
    /// </summary>
    private static TimeSpan GetPlannedShiftLength(TimeSpan shiftStartTime, TimeSpan shiftEndTime, bool isNightShift)
    {
        return isNightShift ? shiftStartTime - shiftEndTime : shiftEndTime - shiftStartTime;
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
    /// Legacy lines 324-331: the day name through a hard-coded Arabic literal table rather than a resource
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
    /// Legacy lines 333-344: with the roster off, the employee's substitute duty for that exact date wins
    /// over the duty period's day-of-week Duty; with the roster on, resolution is delegated to the roster
    /// service.
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
    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom040ReportRowDto> rows)
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
    /// The exported column set, in legacy DataTable order (lines 701-756) MINUS the three break columns
    /// Custom040 does not have. The technical Oid row-sequence column is omitted, as on the base report.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom040ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom040ReportRowDto>>
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
    }

    private class TimePolicyInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string TimePolicyName { get; set; }

        public double ClockInLimit { get; set; }

        public double ClockOutLimit { get; set; }

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
