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
/// Late/Early Log With Photo analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLogWithPhoto.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 623-1340).
///
/// Despite the name this is NOT "uscRptLateEarlyLog plus a photo": it is an OLDER, NARROWER and
/// algorithmically DIFFERENT sibling. It has no OrgLevel columns, no hours columns, no substitute-duty
/// handling and no "Missed In/Missed Out" wording; and it uniquely splits a day with a break shift into
/// TWO rows (before and after the break, lines 976-1155). Both were ported independently, as written.
///
/// THE PHOTO: legacy rendered a real image read off the web server's file system. HCMv3 has no equivalent
/// source -- only <c>TimeEvent.GeoSelfiePath</c> was ported, image resolution being deferred. This port
/// therefore surfaces the PATH, not the image. See the DTO remarks; it is flagged, not dropped silently.
///
/// STICKY SHIFT DEFECT (legacy lines 651-655, REPRODUCED): the shift and break-shift locals are declared
/// OUTSIDE both loops and only assigned when a Duty resolves, so a day whose Duty is null reports the last
/// successfully resolved day's shift bounds. Faithfulness is the standing rule for this wave.
///
/// SINGLE-DUTY-PERIOD DEFECT (legacy lines 787-793, REPRODUCED and pinned by
/// <c>Should_Emit_No_Row_When_Two_Duty_Periods_Cover_The_Day_Reproducing_Legacy_Defect</c>): the day's duty
/// period is resolved only when EXACTLY ONE assignment covers the day (<c>if (EmployeeDPCount == 1)</c>).
/// With two overlapping assignments the duty period stays null and the day yields NO ROW AT ALL --
/// unlike <c>uscRptLateEarlyLog</c>, which takes the latest-starting one.
///
/// AGREED DIVERGENCE: legacy applied RLS here (line 650) and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogWithPhoto)]
public class LateEarlyLogWithPhotoReportAppService
    : GridReportAppServiceBase, ILateEarlyLogWithPhotoReportAppService
{
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    /// <summary>Legacy status literals (lines 47-55). Hard-coded there, hard-coded here.</summary>
    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string NoReturnDateEn = "No Return Date";

    private const string NoReturnDateAr = "لا تاريخ العودة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    /// <summary>Legacy <c>missedPunch</c> literal (line 56).</summary>
    private const string MissedPunch = "*";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRosterService _rosterService;
    private readonly ITimeDataService _timeDataService;

    public LateEarlyLogWithPhotoReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<Duty> dutyRepository,
        IRosterService rosterService,
        ITimeDataService timeDataService)
    {
        _employeeRepository = employeeRepository;
        _timeEventRepository = timeEventRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _dutyRepository = dutyRepository;
        _rosterService = rosterService;
        _timeDataService = timeDataService;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLogWithPhoto";

    protected override string ScreenCaption => "Late Early Log With Photo";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogWithPhotoReportRowDto>> GetLateEarlyLogWithPhotoReport(
        GetLateEarlyLogWithPhotoReportInput input)
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
            BuildWithPhotoParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogWithPhotoReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogWithPhotoReportToExcel(
        GetLateEarlyLogWithPhotoReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "LateEarlyLogWithPhoto.xlsx",
            rows,
            GetExportColumns(),
            BuildWithPhotoParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> logs the payroll area on top of the employee code and the date
    /// range. The base helper appends extra parts at the END whereas legacy printed the payroll area
    /// FIRST -- an ordering difference inside a free-text audit field.
    /// </summary>
    private string BuildWithPhotoParameterString(GetLateEarlyLogWithPhotoReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (lines 629-1336).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogWithPhotoReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogWithPhotoReportInput input)
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
            return new List<LateEarlyLogWithPhotoReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy line 658: the GeoSelfie cell is populated only when documents are stored on the file
        // system -- the alternative branch is entirely commented out, i.e. it wrote nothing.
        var documentSettings = await SysSettingsAccessor.GetDocumentSettingsAsync();
        var isFileSystemStore = documentSettings != null && documentSettings.EnableDocSaveInFileSystem;

        // Legacy lines 634-650: ACTIVE employees only (hard-coded), optionally restricted to a payroll
        // area as of TODAY, then the comma-split code list, then RLS.
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
                // Legacy emp.CurrentWorkLocation -- the work location as of TODAY, not of the row's day.
                CurrentWorkLocation = _orgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == e.Id && o.StartDate <= today && o.EndDate >= today)
                    .OrderBy(o => o.StartDate)
                    .ThenBy(o => o.Id)
                    .Select(o => o.WorkLocation == null
                        ? string.Empty
                        : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                    .FirstOrDefault(),
                // Legacy lines 884-895: the Country comes from the assignment covering the FROM DATE.
                CountryName = _orgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == e.Id
                                && o.StartDate <= fromDate
                                && o.EndDate >= fromDate
                                && o.PayrollAreaId != null)
                    .OrderBy(o => o.StartDate)
                    .ThenBy(o => o.Id)
                    .Select(o => o.PayrollArea.Country == null
                        ? null
                        : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                    .FirstOrDefault()
            })
            .OrderBy(e => e.Code)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new List<LateEarlyLogWithPhotoReportRowDto>().AsQueryable();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Legacy lines 738-741. NOTE: unlike uscRptLateEarlyLog this variant does NOT widen the window by
        // a day, so a night shift's next-morning clock-out on the LAST day of the window is not loaded.
        var timeEvents = await _timeEventRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.TransDate.Date >= fromDate
                        && t.TransDate.Date <= toDate)
            .Select(t => new TimeEventInfo
            {
                EmployeeId = t.EmployeeId,
                TransTypeCode = t.TransType.Code,
                TransDate = t.TransDate,
                TransTime = t.TransTime,
                WorkLocationEn = t.WorkLocation == null ? null : t.WorkLocation.NameEn,
                WorkLocationAr = t.WorkLocation == null ? null : t.WorkLocation.NameAr,
                GeoSelfiePath = t.GeoSelfiePath
            })
            .ToListAsync();

        foreach (var timeEvent in timeEvents)
        {
            timeEvent.TransDateTime = timeEvent.TransDate.Date.Add(timeEvent.TransTime.TimeOfDay);
        }

        var eventsByEmployee = timeEvents.GroupBy(t => t.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        // Legacy lines 743-744: ALL duty-period assignments, deliberately NOT date-filtered -- the
        // "does this employee have any duty period at all" guard below depends on that.
        var dutyPeriodAssignments = await _employeeDutyPeriodRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeId))
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

        // Legacy lines 746-747: also deliberately NOT date-filtered.
        var timePolicies = await _employeeTimePolicyRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeId))
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
                TimeEventRule = p.TimePolicy.TimeEventRule
            })
            .ToListAsync();

        // Legacy lines 749-756: FULL-DAY absences only -- AbsenceHours == 0 is part of the query on this
        // variant (it is commented out on uscRptLateEarlyLog), so partial leaves never reach this report.
        var absences = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.AbsenceHours == 0
                        && a.StartDate.Date <= toDate
                        && a.EndDate.Date >= fromDate)
            .Select(a => new AbsenceInfo
            {
                EmployeeId = a.EmployeeId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                IsReturnDateRequired = a.AbsenceType.IsReturnDateRequired,
                ReturnDate = a.ReturnDate
            })
            .ToListAsync();

        var duties = await _dutyRepository.GetAll()
            .Select(d => new DutyInfo
            {
                Id = d.Id,
                IsNightShift = d.IsNightShift,
                IsWeeklyOff = d.IsWeeklyOff,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                BreakStartTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.StartTime,
                BreakEndTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.EndTime
            })
            .ToDictionaryAsync(d => d.Id);

        // Legacy called TimeDataService.GetPublicHolidaysdates(session, emp, i, i, dsDutyPeriod) once per
        // (day, employee) (lines 845-848). The ported service exposes the same resolution keyed by employee
        // id over a DATE RANGE, so it is called once per employee for the whole window instead -- same
        // answer per day, one query instead of (days x employees).
        var publicHolidaysByEmployee = new Dictionary<int, HashSet<DateTime>>();
        foreach (var employee in employees)
        {
            var holidays = await _timeDataService.GetPublicHolidaysDatesListAsync(employee.Id, fromDate, toDate);
            publicHolidaysByEmployee[employee.Id] = holidays.Select(h => h.HolidayDate.Date).ToHashSet();
        }

        var rows = new List<LateEarlyLogWithPhotoReportRowDto>();

        // ---- STICKY SHIFT DEFECT: legacy declares these outside both loops (lines 651-655). Reproduced.
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;
        TimeSpan? breakShiftStartTime = null;
        TimeSpan? breakShiftEndTime = null;

        // Legacy line 762: the OUTER loop is the calendar day and the inner one the employee -- the
        // opposite nesting of uscRptLateEarlyLog. It only affects generation order, which paging then
        // overrides anyway, but the shift locals above leak along this order, so it is kept.
        for (var dayDate = fromDate; dayDate <= toDate; dayDate = dayDate.AddDays(1))
        {
            foreach (var employee in employees)
            {
                // Legacy lines 768-775: NOTE this policy lookup is made against the report's FROM DATE,
                // not the row's day -- and it is only used for the Leave / No-Return-Date rows. The normal
                // attendance branch re-resolves the policy against the row's day (line 968). Reproduced.
                var fromDatePolicy = timePolicies
                    .Where(p => p.EmployeeId == employee.Id && p.StartDate <= fromDate && p.EndDate >= fromDate)
                    .OrderBy(p => p.EndDate)
                    .FirstOrDefault();

                // Legacy line 779: an employee with NO duty-period assignment AT ALL contributes no rows.
                if (!dutyPeriodsByEmployee.TryGetValue(employee.Id, out var employeeDutyPeriods))
                {
                    continue;
                }

                eventsByEmployee.TryGetValue(employee.Id, out var employeeEvents);
                employeeEvents ??= new List<TimeEventInfo>();

                var dayEvents = employeeEvents
                    .Where(e => e.TransDate.Date == dayDate
                                || e.TransDate.Date == dayDate.AddDays(1)
                                || e.TransDate.Date == dayDate.AddDays(-1))
                    .ToList();

                // SINGLE-DUTY-PERIOD DEFECT (lines 787-793): only EXACTLY ONE covering assignment yields a
                // duty period; zero or two or more yield none, and the day is skipped entirely.
                var covering = employeeDutyPeriods
                    .Where(d => d.StartDate <= dayDate && d.EndDate >= dayDate)
                    .ToList();

                if (covering.Count != 1)
                {
                    continue;
                }

                var dutyPeriod = covering[0];

                var weekDay = GetWeekDayName(dayDate, useArabicNames);

                var duty = await ResolveDutyAsync(employee.Id, dutyPeriod, dayDate, isRosterActive, duties);

                var isWeekOff = false;
                var isNightShift = false;

                if (duty != null)
                {
                    // Legacy lines 836-842.
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;
                    breakShiftStartTime = duty.BreakStartTime;
                    breakShiftEndTime = duty.BreakEndTime;
                    isNightShift = duty.IsNightShift;
                    isWeekOff = duty.IsWeeklyOff;
                }

                // Legacy lines 845-848.
                var isPublicHoliday = publicHolidaysByEmployee[employee.Id].Contains(dayDate.Date);

                // Legacy lines 851-853: a full-day leave covering the day.
                var onLeave = absences.Any(a => a.EmployeeId == employee.Id
                                                && a.StartDate <= dayDate
                                                && a.EndDate >= dayDate);

                if (onLeave)
                {
                    rows.Add(BuildHeaderOnlyRow(
                        employee,
                        dayDate,
                        weekDay,
                        shiftStartTime,
                        shiftEndTime,
                        useArabicNames ? LeaveAr : LeaveEn,
                        fromDatePolicy));
                    continue;
                }

                // Legacy lines 906-919.
                var employeeAbsences = absences.Where(a => a.EmployeeId == employee.Id).ToList();
                var withReturnDate = employeeAbsences.Where(a => a.ReturnDate != default).ToList();
                var maxReturnDate = withReturnDate.Count > 0
                    ? withReturnDate.Max(a => a.ReturnDate)
                    : new DateTime(1900, 1, 1);

                var notReturnedFromLeave = employeeAbsences.Any(a => a.EndDate <= dayDate
                                                                     && a.ReturnDate == default
                                                                     && a.EndDate > maxReturnDate
                                                                     && a.IsReturnDateRequired);

                if (notReturnedFromLeave)
                {
                    rows.Add(BuildHeaderOnlyRow(
                        employee,
                        dayDate,
                        weekDay,
                        shiftStartTime,
                        shiftEndTime,
                        useArabicNames ? NoReturnDateAr : NoReturnDateEn,
                        fromDatePolicy));
                    continue;
                }

                // Legacy lines 968-971: the normal branch resolves the policy against THE ROW'S DAY.
                var dayPolicy = timePolicies.FirstOrDefault(p => p.EmployeeId == employee.Id
                                                                 && p.StartDate <= dayDate
                                                                 && p.EndDate >= dayDate);
                if (dayPolicy == null)
                {
                    continue;
                }

                // Legacy line 974: a break shift splits the day into TWO rows.
                if (breakShiftStartTime != null && breakShiftEndTime != null)
                {
                    for (var half = 1; half <= 2; half++)
                    {
                        var subShiftStart = half == 1 ? shiftStartTime : breakShiftEndTime.Value;
                        var subShiftEnd = half == 1 ? breakShiftStartTime.Value : shiftEndTime;

                        // Legacy lines 984-991: the tolerance window around the sub-shift.
                        var windowStart = half == 1
                            ? subShiftStart.Add(TimeSpan.FromMinutes(-dayPolicy.ClockInLimit))
                            : subShiftStart.Add(TimeSpan.FromMinutes(-dayPolicy.BreakOutLimit));
                        var windowEnd = half == 1
                            ? subShiftEnd.Add(TimeSpan.FromMinutes(dayPolicy.BreakInLimit))
                            : subShiftEnd.Add(TimeSpan.FromMinutes(dayPolicy.ClockOutLimit));

                        rows.Add(BuildAttendanceRow(
                            employee,
                            dayPolicy,
                            dayEvents,
                            dayDate,
                            weekDay,
                            subShiftStart,
                            subShiftEnd,
                            dayDate.Add(windowStart),
                            dayDate.Add(windowEnd),
                            dayDate.Add(windowStart),
                            dayDate.Add(windowEnd),
                            isWeekOff,
                            isPublicHoliday,
                            // LEGACY DEFECT (line 1095): on the split rows the Late Out test compares the
                            // punch against the sub-shift's START time while subtracting its END time.
                            lateOutComparesAgainstShiftStart: true,
                            useArabicNames: useArabicNames,
                            isFileSystemStore: isFileSystemStore));
                    }

                    continue;
                }

                // Legacy lines 1157-1163: the single-row branch, with the night-shift day roll-over on the
                // clock-out window only.
                var clockInStart = dayDate.Add(shiftStartTime).AddMinutes(-dayPolicy.ClockInLimit);
                var clockInEnd = dayDate.Add(shiftStartTime).AddMinutes(dayPolicy.ClockInLimit);

                var shiftEndBase = isNightShift ? dayDate.AddDays(1).Add(shiftEndTime) : dayDate.Add(shiftEndTime);
                var clockOutStart = shiftEndBase.AddMinutes(-dayPolicy.ClockOutLimit);
                var clockOutEnd = shiftEndBase.AddMinutes(dayPolicy.ClockOutLimit);

                rows.Add(BuildAttendanceRow(
                    employee,
                    dayPolicy,
                    dayEvents,
                    dayDate,
                    weekDay,
                    shiftStartTime,
                    shiftEndTime,
                    clockInStart,
                    clockInEnd,
                    clockOutStart,
                    clockOutEnd,
                    isWeekOff,
                    isPublicHoliday,
                    lateOutComparesAgainstShiftStart: false,
                    useArabicNames: useArabicNames,
                    isFileSystemStore: isFileSystemStore));
            }
        }

        // Legacy declared a "SrNo" column (line 710) but NEVER assigned it -- the cell was always empty.
        // The inherited Id is filled with a 1-based row sequence so the grid has a stable row key.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The Leave row (legacy lines 854-904) and the No-Return-Date row (lines 921-965), which are the same
    /// header block with a different Status and no punch data at all.
    /// </summary>
    private static LateEarlyLogWithPhotoReportRowDto BuildHeaderOnlyRow(
        EmployeeInfo employee,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        string status,
        TimePolicyInfo timePolicy)
    {
        return new LateEarlyLogWithPhotoReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            Date = dayDate,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            // Legacy explicitly blanks all six punch cells on both branches.
            PunchInLocation = string.Empty,
            PunchOutLocation = string.Empty,
            Status = status,
            WeekDay = weekDay,
            TimePolicyName = timePolicy?.TimePolicyName ?? string.Empty,
            WorkLocationName = employee.CurrentWorkLocation ?? string.Empty,
            CountryName = employee.CountryName
        };
    }

    /// <summary>
    /// The normal attendance row -- legacy lines 993-1155 (break-split variant) and 1157-1333 (single-row
    /// variant), which differ only in their punch windows, the displayed shift bounds and the Late Out
    /// comparison, so they share one implementation here.
    /// </summary>
    private static LateEarlyLogWithPhotoReportRowDto BuildAttendanceRow(
        EmployeeInfo employee,
        TimePolicyInfo timePolicy,
        List<TimeEventInfo> dayEvents,
        DateTime dayDate,
        string weekDay,
        TimeSpan displayedShiftStart,
        TimeSpan displayedShiftEnd,
        DateTime punchInWindowStart,
        DateTime punchInWindowEnd,
        DateTime punchOutWindowStart,
        DateTime punchOutWindowEnd,
        bool isWeekOff,
        bool isPublicHoliday,
        bool lateOutComparesAgainstShiftStart,
        bool useArabicNames,
        bool isFileSystemStore)
    {
        var row = new LateEarlyLogWithPhotoReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            WeekDay = weekDay,
            Date = dayDate,
            ShiftStart = displayedShiftStart,
            ShiftEnd = displayedShiftEnd,
            TimePolicyName = timePolicy.TimePolicyName,
            WorkLocationName = employee.CurrentWorkLocation ?? string.Empty,
            CountryName = employee.CountryName
        };

        var punchInCandidates = dayEvents
            .Where(e => e.TransTypeCode == PunchInTransTypeCode
                        && punchInWindowStart <= e.TransDateTime
                        && punchInWindowEnd >= e.TransDateTime)
            .ToList();

        var punchOutCandidates = dayEvents
            .Where(e => e.TransTypeCode == PunchOutTransTypeCode
                        && punchOutWindowStart <= e.TransDateTime
                        && punchOutWindowEnd >= e.TransDateTime)
            .ToList();

        // Legacy lines 1013-1033: the four TimeEventRule orderings.
        var punchIn = timePolicy.TimeEventRule switch
        {
            TimeEventRule.FirstInFirstOut or TimeEventRule.FirstInLastOut =>
                punchInCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
            TimeEventRule.LastInFirstOut or TimeEventRule.LastInLastOut =>
                punchInCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
            // Legacy had no else branch: TimeEventRule.NA leaves the candidate list untrimmed and then
            // takes FirstOrDefault() of it, i.e. the first in natural order.
            _ => punchInCandidates.FirstOrDefault()
        };

        var punchOut = timePolicy.TimeEventRule switch
        {
            TimeEventRule.FirstInFirstOut or TimeEventRule.LastInFirstOut =>
                punchOutCandidates.OrderBy(e => e.TransDateTime).FirstOrDefault(),
            TimeEventRule.FirstInLastOut or TimeEventRule.LastInLastOut =>
                punchOutCandidates.OrderByDescending(e => e.TransDateTime).FirstOrDefault(),
            _ => punchOutCandidates.FirstOrDefault()
        };

        // Legacy lines 1036-1080: the punch cells are filled only when the day is NOT a week off, so on a
        // week off the punches are reported as absent even though they exist.
        if (punchIn != null && !isWeekOff)
        {
            row.SignIn = punchIn.TransTime.TimeOfDay;

            if (displayedShiftStart > punchIn.TransTime.TimeOfDay)
            {
                row.EarlyIn = displayedShiftStart - punchIn.TransTime.TimeOfDay;
            }

            if (displayedShiftStart < punchIn.TransTime.TimeOfDay)
            {
                row.LateIn = punchIn.TransTime.TimeOfDay - displayedShiftStart;
            }

            if (punchIn.WorkLocationEn != null || punchIn.WorkLocationAr != null)
            {
                row.PunchInLocation = useArabicNames ? punchIn.WorkLocationAr : punchIn.WorkLocationEn;
            }

            if (isFileSystemStore)
            {
                row.GeoSelfiePath = punchIn.GeoSelfiePath;
            }
        }

        if (punchOut != null && !isWeekOff)
        {
            row.SignOut = punchOut.TransTime.TimeOfDay;

            if (displayedShiftEnd > punchOut.TransTime.TimeOfDay)
            {
                row.EarlyOut = displayedShiftEnd - punchOut.TransTime.TimeOfDay;
            }

            // LEGACY DEFECT on the break-split rows (line 1095): the guard reads the sub-shift's START
            // while the subtraction uses its END, so LateOut can come out negative or be skipped
            // altogether. The single-row branch (line 1201) correctly guards on the END.
            var lateOutGuard = lateOutComparesAgainstShiftStart ? displayedShiftStart : displayedShiftEnd;
            if (lateOutGuard < punchOut.TransTime.TimeOfDay)
            {
                row.LateOut = punchOut.TransTime.TimeOfDay - displayedShiftEnd;
            }

            if (punchOut.WorkLocationEn != null || punchOut.WorkLocationAr != null)
            {
                // LEGACY DEFECT (lines 1103/1258): the language ternary is INVERTED on this one cell --
                // the Arabic aspect gets the English name and vice versa. Reproduced.
                row.PunchOutLocation = useArabicNames ? punchOut.WorkLocationEn : punchOut.WorkLocationAr;
            }

            if (isFileSystemStore)
            {
                // Last writer wins: the punch-OUT selfie overwrites the punch-IN one. See the DTO remarks.
                row.GeoSelfiePath = punchOut.GeoSelfiePath;
            }
        }

        // Legacy lines 1121-1146: the asterisk marks a day with exactly one of the two punches.
        if (isWeekOff)
        {
            row.Status = useArabicNames ? WeekOffAr : WeekOffEn;
        }
        else if (isPublicHoliday)
        {
            row.Status = useArabicNames ? PublicHolidayAr : PublicHolidayEn;
        }
        else if ((punchInCandidates.Count > 0) != (punchOutCandidates.Count > 0))
        {
            row.Status = MissedPunch;
        }
        else
        {
            row.Status = string.Empty;
        }

        return row;
    }

    /// <summary>
    /// Legacy lines 809-815: the day name, with a hard-coded Arabic literal table. Reproduced verbatim.
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
    /// Ports legacy lines 818-832. NOTE: unlike <c>uscRptLateEarlyLog</c> and <c>uscRptTimeSchedule</c>,
    /// this variant does NOT consult <c>EmployeeSubstituteDuty</c> at all in the weekly branch, and passes
    /// a null substitute list to the roster service. Reproduced.
    /// </summary>
    private async Task<DutyInfo> ResolveDutyAsync(
        int employeeId,
        DutyPeriodInfo dutyPeriod,
        DateTime dayDate,
        bool isRosterActive,
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
    /// The exported column set, in legacy column order (lines 710-729). The never-populated SrNo column is
    /// omitted; the GeoSelfie column is exported as the stored path -- see the DTO remarks.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogWithPhotoReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogWithPhotoReportRowDto>>
        {
            new("GeoSelfie", r => r.GeoSelfiePath),
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Date", r => r.Date),
            new("ShiftStart", r => r.ShiftStart),
            new("ShiftEnd", r => r.ShiftEnd),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("EarlyIn", r => r.EarlyIn),
            new("EarlyOut", r => r.EarlyOut),
            new("LateIn", r => r.LateIn),
            new("LateOut", r => r.LateOut),
            new("PunchInLocation", r => r.PunchInLocation),
            new("PunchOutLocation", r => r.PunchOutLocation),
            new("Status", r => r.Status),
            new("WeekDay", r => r.WeekDay),
            new("TimePolicy", r => r.TimePolicyName),
            new("WorkLocation", r => r.WorkLocationName),
            new("Country", r => r.CountryName)
        };
    }

    private class EmployeeInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string FullName { get; set; }

        public string CurrentWorkLocation { get; set; }

        public string CountryName { get; set; }
    }

    private class TimeEventInfo
    {
        public int EmployeeId { get; set; }

        public string TransTypeCode { get; set; }

        public DateTime TransDate { get; set; }

        public DateTime TransTime { get; set; }

        public DateTime TransDateTime { get; set; }

        public string WorkLocationEn { get; set; }

        public string WorkLocationAr { get; set; }

        public string GeoSelfiePath { get; set; }
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

        public TimeSpan? BreakStartTime { get; set; }

        public TimeSpan? BreakEndTime { get; set; }
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
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsReturnDateRequired { get; set; }

        public DateTime ReturnDate { get; set; }
    }
}
