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
/// Custom045 Late/Early Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom045.ascx(.cs)</c>,
/// method <c>GetTimeLogData(...)</c> (lines 600-1486). That is the method <c>GetTable()</c> actually calls
/// (line 553); <c>Table1_OptFinal()</c>, <c>Table1_Old()</c> and <c>GetTimeLogData_Shabbir(...)</c> are
/// three unreferenced alternative implementations left in the file and are NOT ported.
///
/// WHY THIS IS A SEPARATE SERVICE AND NOT A SUBCLASS OF <see cref="LateEarlyLogReportAppService"/>: see
/// the same discussion on <see cref="LateEarlyLogCustom040ReportAppService"/>. Custom045 in particular
/// REPLACES the base's entire punch-selection strategy, which is the single largest block of the base
/// algorithm, so there is very little left to inherit.
///
/// THE DEFINING CHANGE -- A DIFFERENT PUNCH MODEL. The base selects a clock-in inside a
/// <c>ClockInLimit</c> window around the shift start and a clock-out inside a <c>ClockOutLimit</c> window
/// around the shift end, and infers the break from clock-in/clock-out punches inside a break window.
/// Custom045 throws all of that away (lines 1108-1155):
/// <list type="bullet">
/// <item><description>ONE window for the whole day: <c>[dayDate + Duty.FlexiStartTime,
/// dayDate + Duty.FlexiEndTime]</c>. No tolerance limits, no <c>TimeMgmtStatus</c> branch, no night-shift
/// extension (the <c>AddDays(1)</c> variant is present but commented out, line 1123) and no week-off
/// whole-day override -- so on a duty with no flexi times configured the window collapses to a single
/// instant and the day reports no punches at all.</description></item>
/// <item><description>FOUR transaction types instead of two: "1" clock in, "2" clock out, and the
/// DEDICATED break events "3" (break out) and "4" (break in) that the base never reads.</description></item>
/// </list>
///
/// FULL-DAY LEAVE NO LONGER CHARGES ABSENCE HOURS: the base's <c>absencehours = plannedHours</c> was
/// deleted (base line 470 has it, Custom045 line 494 does not), and the cell is written only when the
/// absence type is UNPAID (lines 498-503). A paid full-day leave therefore shows a blank Absence Hours and
/// an unpaid one shows 00:00 -- neither shows the planned shift. Reproduced and pinned by a test.
///
/// STICKY SHIFT DEFECT (legacy lines 909-912, REPRODUCED): the four shift/break locals live OUTSIDE both
/// loops and are only assigned when a Duty resolves, so a day with no Duty reports the last successfully
/// resolved day's bounds.
///
/// NOT PORTED -- THE ESS ENTRY POINT: <c>GetTimeLogData</c> is public and takes an
/// <c>organizationUnitType</c>/<c>organizationUnit</c>/<c>startDate</c>/<c>endDate</c> quadruple used by an
/// ESS caller (lines 657-668), which also OVERWRITES the report's own date range. The grid always passes
/// nulls and defaults, so only the grid path is a report and only it is exposed here.
///
/// AGREED DIVERGENCE: RLS is applied here exactly as legacy applied it (line 670, via the two-argument
/// <c>ApplyRowLevelSecurity</c> overload at line 4223).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom045)]
public class LateEarlyLogCustom045ReportAppService : GridReportAppServiceBase,
    ILateEarlyLogCustom045ReportAppService
{
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    /// <summary>
    /// Custom045-only transaction types. The base control has no concept of these: it derives the break
    /// from ordinary clock-in/clock-out punches falling inside a break window.
    /// </summary>
    private const string BreakOutTransTypeCode = "3";

    private const string BreakInTransTypeCode = "4";

    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string NoReturnDateEn = "No Return Date";

    private const string NoReturnDateAr = "لا تاريخ العودة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    /// <summary>Legacy's "no overtime type on this duty" sentinel (line 1019).</summary>
    private const int NoOvertimeTypeId = -1;

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

    public LateEarlyLogCustom045ReportAppService(
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

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom045";

    protected override string ScreenCaption => "Late Early Log (Custom 045)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom045ReportRowDto>> GetLateEarlyLogCustom045Report(
        GetLateEarlyLogCustom045ReportInput input)
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

        return new PagedResultDto<LateEarlyLogCustom045ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom045ReportToExcel(GetLateEarlyLogCustom045ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLogCustom045.xlsx",
            rows,
            GetExportColumns(),
            BuildLateEarlyParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 582-598).</summary>
    private string BuildLateEarlyParameterString(GetLateEarlyLogCustom045ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports the "Apply Filter", "Load Initial Data" and "Check For Each Data" regions
    /// (legacy lines 640-1482).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom045ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom045ReportInput input)
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
            return new List<LateEarlyLogCustom045ReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy lines 643-670: ACTIVE employees, optionally restricted to a payroll area AS OF TODAY,
        // then the comma-split code list, then RLS.
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
            return new List<LateEarlyLogCustom045ReportRowDto>().AsQueryable();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

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
                TimeEventRule = p.TimePolicy.TimeEventRule,
                OTMinLimit = p.TimePolicy.OTMinLimit
            })
            .ToListAsync();

        // NOTE what is NOT selected above: ClockInLimit/ClockOutLimit/BreakInLimit/BreakOutLimit and
        // TimeMgmtStatus. Custom045's single flexi window uses none of them.

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
                IsPaid = a.AbsenceType.IsPaid,
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
                FlexiEndTime = d.FlexiEndTime,
                BreakShiftId = d.BreakShiftId,
                BreakStartTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.StartTime,
                BreakEndTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.EndTime,
                IsBreakStartNextDay = d.BreakShift != null && d.BreakShift.IsBreakStartNextDay,
                IsBreakEndNextDay = d.BreakShift != null && d.BreakShift.IsBreakEndNextDay,
                // ALWAYS ENGLISH: legacy reads NameEn unconditionally on this one column (line 1018), even
                // in the Arabic aspect, unlike every other lookup name on the row. Reproduced.
                OvertimeTypeName = d.OvertimeType == null ? string.Empty : d.OvertimeType.NameEn,
                OvertimeTypeId = d.OvertimeTypeId
            })
            .ToDictionaryAsync(d => d.Id);

        var rows = new List<LateEarlyLogCustom045ReportRowDto>();

        // ---- STICKY SHIFT DEFECT: these four locals live OUTSIDE both loops in legacy (lines 909-912).
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;
        DateTime breakShiftStartTime = default;
        DateTime breakShiftEndTime = default;

        foreach (var employee in employees)
        {
            var publicHolidays = (await _timeDataService.GetPublicHolidaysDatesListAsync(employee.Id, fromDate, toDate))
                .Select(h => h.HolidayDate.Date)
                .ToHashSet();

            // Legacy line 936 (`dsCount > 0`).
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

                // Like Custom040, Custom045 DROPPED the base's `.OrderByDescending(x => x.StartDate)`
                // (line 947): with two overlapping duty periods the first the query returned wins.
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
                var hasBreakShift = false;

                // Unlike the shift locals these two are declared INSIDE the day loop in legacy
                // (lines 956-957), so they reset to ""/-1 on a day with no Duty rather than sticking.
                var overtimeTypeName = string.Empty;
                var overtimeTypeId = NoOvertimeTypeId;

                if (duty != null)
                {
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;

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

                    overtimeTypeName = duty.OvertimeTypeName;
                    overtimeTypeId = duty.OvertimeTypeId ?? NoOvertimeTypeId;
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
                        timePolicy,
                        overtimeTypeName,
                        fullDayLeave.IsPaid));
                    continue;
                }

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
                    // The no-return-date row carries the OvertimeType caption but, unlike the attendance
                    // row, NOT the OvertimeTypeOid (legacy writes only the former here, line 548).
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
                        overtimeTypeName: overtimeTypeName,
                        isPaidLeave: false,
                        includeHours: false));
                    continue;
                }

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
                    breakShiftStartTime,
                    breakShiftEndTime,
                    isNightShift,
                    isWeekOff,
                    isPublicHoliday,
                    hasBreakShift,
                    overtimeTypeName,
                    overtimeTypeId,
                    useArabicNames));
            }
        }

        // Legacy's "Oid" cell is `table.Rows.Count + 1` -- a 1-based ROW SEQUENCE number, not an id.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The full-day-leave row (legacy lines 452-510) and, with <paramref name="includeHours"/> false, the
    /// no-return-date row (lines 537-560).
    /// </summary>
    private static LateEarlyLogCustom045ReportRowDto BuildLeaveRow(
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
        string overtimeTypeName,
        bool isPaidLeave,
        bool includeHours = true)
    {
        var row = new LateEarlyLogCustom045ReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AccessId = employee.AccessId,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            PunchInAccessUnit = string.Empty,
            PunchOutAccessUnit = string.Empty,
            PunchInLocation = string.Empty,
            PunchOutLocation = string.Empty,
            Status = status,
            LeaveType = leaveType,
            TimePolicyName = timePolicy?.TimePolicyName ?? string.Empty,
            WorkLocationName = org?.WorkLocation ?? string.Empty,
            CountryName = org?.PayrollCountry ?? string.Empty,
            OvertimeTypeName = overtimeTypeName,
            // Legacy leaves the OvertimeTypeOid cell empty on both leave branches; -1 is its "none"
            // sentinel and is the closest non-null equivalent on a non-nullable column.
            OvertimeTypeId = NoOvertimeTypeId
        };

        if (!includeHours)
        {
            return row;
        }

        var plannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShift);

        if (!isPublicHoliday)
        {
            row.PlannedHours = plannedHours;
            row.TotalHours = TimeSpan.Zero;

            // CUSTOM045: the base's `absencehours = plannedHours` is GONE, so the value written here is
            // always ZERO -- and it is written at all only for an UNPAID absence type (lines 498-503).
            if (!isPaidLeave)
            {
                row.AbsenceHours = TimeSpan.Zero;
            }
        }

        return row;
    }

    /// <summary>The normal attendance row -- legacy lines 588-1482.</summary>
    private static LateEarlyLogCustom045ReportRowDto BuildAttendanceRow(
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
        DateTime breakShiftStartTime,
        DateTime breakShiftEndTime,
        bool isNightShift,
        bool isWeekOff,
        bool isPublicHoliday,
        bool hasBreakShift,
        string overtimeTypeName,
        int overtimeTypeId,
        bool useArabicNames)
    {
        var row = new LateEarlyLogCustom045ReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AccessId = employee.AccessId,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            OvertimeTypeName = overtimeTypeName,
            OvertimeTypeId = overtimeTypeId
        };

        // ---- THE CUSTOM045 PUNCH MODEL (legacy lines 1108-1155). One flexi window for the whole day,
        // four transaction types, no tolerance limits and no TimeMgmtStatus branch.
        //
        // Legacy dereferences `duty` unconditionally here even though `duty` is only assigned inside
        // `if (duty != null)` above, so a day with no resolvable Duty crashes the entire report with a
        // NullReferenceException. That is a crash, not a data difference, so it is NOT reproduced: a null
        // Duty yields an empty window and therefore no punches, which is what the row would have shown.
        var flexiStart = duty?.FlexiStartTime ?? TimeSpan.Zero;
        var flexiEnd = duty?.FlexiEndTime ?? TimeSpan.Zero;

        var startWindow = dayDate.Add(flexiStart);
        var endWindow = dayDate.Add(flexiEnd);

        var windowEvents = dayEvents
            .Where(e => startWindow <= e.TransDateTime && endWindow >= e.TransDateTime)
            .ToList();

        var punchInCandidates = windowEvents.Where(e => e.TransTypeCode == PunchInTransTypeCode).ToList();
        var punchOutCandidates = windowEvents.Where(e => e.TransTypeCode == PunchOutTransTypeCode).ToList();
        var breakOutCandidates = windowEvents.Where(e => e.TransTypeCode == BreakOutTransTypeCode).ToList();
        var breakInCandidates = windowEvents.Where(e => e.TransTypeCode == BreakInTransTypeCode).ToList();

        // Legacy lines 1157-1183. The four rules are spelled out one by one there, and the break pair does
        // NOT follow the punch pair symmetrically -- e.g. under FirstInLastOut the break-OUT is the FIRST
        // one but the break-IN is the LAST, and under LastInFirstOut it is the other way round.
        TimeEventInfo punchIn = null;
        TimeEventInfo punchOut = null;
        TimeEventInfo breakOut = null;
        TimeEventInfo breakIn = null;

        switch (timePolicy.TimeEventRule)
        {
            case TimeEventRule.FirstInFirstOut:
                punchIn = First(punchInCandidates);
                breakOut = First(breakOutCandidates);
                breakIn = First(breakInCandidates);
                punchOut = First(punchOutCandidates);
                break;

            case TimeEventRule.FirstInLastOut:
                punchIn = First(punchInCandidates);
                breakOut = First(breakOutCandidates);
                breakIn = Last(breakInCandidates);
                punchOut = Last(punchOutCandidates);
                break;

            case TimeEventRule.LastInFirstOut:
                punchIn = Last(punchInCandidates);
                breakOut = Last(breakOutCandidates);
                breakIn = First(breakInCandidates);
                punchOut = First(punchOutCandidates);
                break;

            case TimeEventRule.LastInLastOut:
                punchIn = Last(punchInCandidates);
                breakOut = Last(breakOutCandidates);
                breakIn = Last(breakInCandidates);
                punchOut = Last(punchOutCandidates);
                break;

            // Legacy had no else branch: TimeEventRule.NA selects nothing at all.
        }

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

        // ---- Sign In / Early In / Late In (legacy lines 677-706).
        DateTime actualIn = default;
        DateTime actualOut = default;

        if (punchIn != null)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = punchIn.TransTime.TimeOfDay;

            if (!isWeekOff)
            {
                // CUSTOM045: TIME-OF-DAY comparison, where the base compares full date-times.
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

        // ---- Sign Out / Early Out / Late Out (legacy lines 708-741).
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

        // ---- Status (legacy lines 743-780). Custom045 KEEPS the base's vocabulary; the customer's
        // alternative "*" / single-punch wording is present in the source but commented out.
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

        // ---- Hours arithmetic (legacy lines 800-880).
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

        if (!isWeekOff)
        {
            // Planned hours from the BARE shift times (night shift inverted) minus the SCHEDULED break,
            // where the base used day-anchored date-times.
            plannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShift)
                .Subtract(breakShiftHours);
        }

        if (actualIn != default && actualOut != default)
        {
            totalHours = actualOut.Subtract(actualIn).Subtract(breakHours);
        }

        if (totalHours > plannedHours && !isWeekOff)
        {
            overtimeHours = totalHours.Subtract(plannedHours);
        }

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

        // Zero-valued duration cells are left EMPTY, and on a public holiday only the totals are reported.
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
    /// (line 819). Both operands are bare times on a shared anchor in legacy, so a 22:00-06:00 night shift
    /// yields SIXTEEN hours rather than eight. Reproduced and pinned by a test.
    /// </summary>
    private static TimeSpan GetPlannedShiftLength(TimeSpan shiftStartTime, TimeSpan shiftEndTime, bool isNightShift)
    {
        return isNightShift ? shiftStartTime - shiftEndTime : shiftEndTime - shiftStartTime;
    }

    private static TimeEventInfo First(List<TimeEventInfo> candidates)
    {
        return candidates.OrderBy(e => e.TransDateTime).FirstOrDefault();
    }

    private static TimeEventInfo Last(List<TimeEventInfo> candidates)
    {
        return candidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault();
    }

    /// <summary>
    /// Legacy lines 964-971: the day name through a hard-coded Arabic literal table. Reproduced verbatim.
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
    /// Legacy lines 973-994: with the roster off, the employee's substitute duty for that exact date wins
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

    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom045ReportRowDto> rows)
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
    /// The exported column set, in legacy DataTable order (lines 696-739) plus the OvertimeType caption.
    /// The technical Oid row-sequence column is omitted as on the base report, and so is
    /// <c>OvertimeTypeOid</c>: nothing in the legacy grid reads it -- it exists only for the ESS caller --
    /// and it is a raw database id, not information for a reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom045ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom045ReportRowDto>>
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
            new("PunchOutLocation", r => r.PunchOutLocation),
            new("OvertimeType", r => r.OvertimeTypeName)
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

        public string OvertimeTypeName { get; set; }

        public int? OvertimeTypeId { get; set; }
    }

    private class TimePolicyInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string TimePolicyName { get; set; }

        public TimeEventRule TimeEventRule { get; set; }

        public double OTMinLimit { get; set; }
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string AbsenceTypeName { get; set; }

        public bool IsReturnDateRequired { get; set; }

        /// <summary>Custom045-only: gates whether the Absence Hours cell is written at all.</summary>
        public bool IsPaid { get; set; }

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
