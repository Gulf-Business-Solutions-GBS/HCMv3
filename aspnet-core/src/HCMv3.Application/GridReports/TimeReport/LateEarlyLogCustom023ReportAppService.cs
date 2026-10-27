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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log (Custom 023) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom023.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 707-1482).
///
/// WHY THIS IS NOT THE BASE REPORT WITH FLAGS. Custom023 keeps the base control's filter panel and most
/// of its column set, but computes four things DIFFERENTLY, and each of them changes the number a user
/// reads off the screen:
/// <list type="number">
/// <item><description>EARLY/LATE ARE TIME-OF-DAY DELTAS. The base subtracts two full timestamps
/// (<c>actualshiftStartTime</c> vs the punch's <c>TransDateTime</c>). Custom023 subtracts bare
/// times-of-day (<c>shiftStartTime.TimeOfDay</c> vs <c>TransTime.TimeOfDay</c>, lines 1068-1078), so on a
/// night shift the delta does NOT wrap across midnight and a punch made after midnight reads as hours
/// EARLY rather than minutes late.</description></item>
/// <item><description>PLANNED HOURS COME FROM THE RAW SHIFT TIMES with a night-shift inversion
/// (<c>!isNightShift ? end - start : start - end</c>, line 1123) instead of from the resolved shift
/// timestamps -- and, unlike the base, are computed even on a week off.</description></item>
/// <item><description>A RICHER STATUS LADDER: "Rest Day", "*" and "Absent" all exist here and none of them
/// exists in the base control, whose ladder ends at "Missed In"/"Missed Out" (lines 1157-1191).</description></item>
/// <item><description>NO BREAK MACHINERY AT ALL: Break Out, Break In and Break Hours are not declared, not
/// selected and never netted out of Planned or Total Hours.</description></item>
/// </list>
///
/// GRAIN: one row per (ACTIVE employee, calendar day in the window), but only for employees with at least
/// one duty-period assignment in the window (legacy <c>dsCount &gt; 0</c> guard) and only for days a duty
/// period covers. Within that, a day produces exactly one of three row shapes: a FULL-DAY LEAVE row, a
/// NO-RETURN-DATE row, or the normal attendance row -- and the normal row is skipped entirely when the
/// employee has no time policy covering the day.
///
/// IN-MEMORY BY NECESSITY: see <see cref="LateEarlyLogCustomDataLoader"/>.
///
/// STICKY SHIFT DEFECT (legacy lines 990-996, REPRODUCED and pinned by
/// <c>Should_Carry_Previous_Shift_Bounds_Into_A_Day_Without_Duty_Reproducing_Legacy_Defect</c>): the shift
/// locals live OUTSIDE both loops and are only assigned when a Duty resolves, so a day whose Duty is null
/// reports the LAST successfully resolved day's shift bounds.
///
/// PUBLIC-HOLIDAY BLACKOUT (legacy lines 1149-1155, REPRODUCED and pinned by
/// <c>Should_Report_No_Hours_At_All_On_A_Public_Holiday_Reproducing_Legacy_Defect</c>): the base control
/// and Custom019 both fall back to reporting Total Hours on a public holiday. Custom023 has NO such
/// branch, so a public holiday reports no Planned, Total or Absence hours whatsoever even when the
/// employee actually worked -- only Overtime survives, because its write sits outside the
/// <c>if (!isPublicHoliday)</c>.
///
/// UNSET STATUS ON A CLEAN DAY (legacy lines 1185-1191): where the base writes <c>""</c>, Custom023's
/// final <c>else</c> writes NOTHING when both punch lists are non-empty, leaving the cell DBNull. That is
/// null here, not empty string.
///
/// AGREED DIVERGENCE: legacy applied RLS here and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1484-2489).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom023)]
public class LateEarlyLogCustom023ReportAppService : GridReportAppServiceBase,
    ILateEarlyLogCustom023ReportAppService
{
    /// <summary>Legacy <c>TransType.Code</c> "1" is the clock-in event, "2" the clock-out event.</summary>
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    /// <summary>Legacy status literals. Hard-coded there, hard-coded here.</summary>
    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string NoReturnDateEn = "No Return Date";

    private const string NoReturnDateAr = "لا تاريخ العودة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string RestDayEn = "Rest Day";

    private const string RestDayAr = "يوم راحة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly LateEarlyLogCustomDataLoader _dataLoader;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogCustom023ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        LateEarlyLogCustomDataLoader dataLoader,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _dataLoader = dataLoader;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom023";

    protected override string ScreenCaption => "Late Early Log (Custom 023)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom023ReportRowDto>> GetLateEarlyLogCustom023Report(
        GetLateEarlyLogCustom023ReportInput input)
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
            BuildCustom023ParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogCustom023ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom023ReportToExcel(GetLateEarlyLogCustom023ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLogCustom023.xlsx",
            rows,
            GetExportColumns(),
            BuildCustom023ParameterString(input));
    }

    private string BuildCustom023ParameterString(GetLateEarlyLogCustom023ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>'s "Apply Filter", "Load Initial Data" and "Check For Each Data"
    /// regions (lines 792-1478).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom023ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom023ReportInput input)
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
            return new List<LateEarlyLogCustom023ReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // ACTIVE employees only (hard-coded, there is no status combo), optionally restricted to a payroll
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
            .Select(e => new LateEarlyLogEmployeeInfo
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
            return new List<LateEarlyLogCustom023ReportRowDto>().AsQueryable();
        }

        var data = await _dataLoader.LoadAsync(employees, fromDate, toDate, useArabicNames, isRosterActive);

        var rows = new List<LateEarlyLogCustom023ReportRowDto>();

        // ---- STICKY SHIFT DEFECT: declared OUTSIDE both loops in legacy. Reproduced. See class remarks.
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;

        foreach (var employee in data.Employees)
        {
            var publicHolidays = await _dataLoader.GetPublicHolidayDatesAsync(employee.Id, fromDate, toDate);

            if (!data.HasAnyDutyPeriod(employee.Id))
            {
                continue;
            }

            for (var dayDate = fromDate; dayDate <= toDate; dayDate = dayDate.AddDays(1))
            {
                var timePolicy = data.FindTimePolicy(employee.Id, dayDate);
                var org = data.FindOrgAssignment(employee.Id, dayDate);
                var dutyPeriod = data.FindDutyPeriod(employee.Id, dayDate);

                if (dutyPeriod == null)
                {
                    continue;
                }

                var dayEvents = data.GetDayEvents(employee.Id, dayDate);
                var weekDay = LateEarlyLogCustomDataSet.GetWeekDayName(dayDate, useArabicNames);

                var duty = await data.ResolveDutyAsync(employee.Id, dutyPeriod, dayDate);

                var isWeekOff = false;
                var isRestDay = false;
                var isNightShift = false;

                if (duty != null)
                {
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;
                    isNightShift = duty.IsNightShift;
                    isWeekOff = duty.IsWeeklyOff;
                    isRestDay = duty.IsRestDay;
                }

                var isPublicHoliday = publicHolidays.Contains(dayDate.Date);

                var fullDayLeave = data.Absences.FirstOrDefault(a => a.EmployeeId == employee.Id
                                                                     && a.StartDate <= dayDate
                                                                     && a.EndDate >= dayDate
                                                                     && a.AbsenceHours == 0);

                if (fullDayLeave != null)
                {
                    rows.Add(BuildLeaveRow(
                        employee, org, dayDate, weekDay, shiftStartTime, shiftEndTime, isNightShift,
                        isPublicHoliday, useArabicNames ? LeaveAr : LeaveEn,
                        fullDayLeave.AbsenceTypeName, timePolicy));
                    continue;
                }

                var employeeAbsences = data.Absences.Where(a => a.EmployeeId == employee.Id).ToList();
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
                        employee, org, dayDate, weekDay, shiftStartTime, shiftEndTime, isNightShift,
                        isPublicHoliday, useArabicNames ? NoReturnDateAr : NoReturnDateEn,
                        leaveType: null, timePolicy: timePolicy, includeHours: false));
                    continue;
                }

                if (timePolicy == null)
                {
                    continue;
                }

                rows.Add(BuildAttendanceRow(
                    employee, org, timePolicy, duty, dayEvents, data.Absences, dayDate, weekDay,
                    shiftStartTime, shiftEndTime, isNightShift, isWeekOff, isRestDay, isPublicHoliday,
                    useArabicNames));
            }
        }

        // Legacy NEVER assigned the Oid cell on this variant (there is no `dr[Oid] =` in its Table1()), so
        // the column was always blank. The Id is stamped here anyway because the Angular grid needs a
        // stable row key; the column is not exported.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The full-day-leave row (legacy lines 1005-1063) and, with <paramref name="includeHours"/> false,
    /// the no-return-date row (lines 1080-1105), which share the same header block.
    /// </summary>
    private static LateEarlyLogCustom023ReportRowDto BuildLeaveRow(
        LateEarlyLogEmployeeInfo employee,
        LateEarlyLogOrgAssignmentInfo org,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        bool isPublicHoliday,
        string status,
        string leaveType,
        LateEarlyLogTimePolicyInfo timePolicy,
        bool includeHours = true)
    {
        var row = new LateEarlyLogCustom023ReportRowDto
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

        // Legacy blanks the two access-unit cells on the full-day-leave branch only (lines 1035-1036).
        row.PunchInAccessUnit = string.Empty;
        row.PunchOutAccessUnit = string.Empty;

        // Legacy lines 1047-1060: the whole planned shift counts as absence on a full-day leave, and
        // nothing at all is reported on a public holiday. The Planned/Total/Absence cells are written
        // UNCONDITIONALLY here, including when zero, unlike on the normal attendance row.
        var plannedHours = ComputePlannedHours(shiftStartTime, shiftEndTime, isNightShift);

        if (!isPublicHoliday)
        {
            row.PlannedHours = plannedHours;
            row.TotalHours = TimeSpan.Zero;
            row.AbsenceHours = plannedHours;
        }

        return row;
    }

    /// <summary>
    /// The normal attendance row -- legacy lines 1107-1478.
    /// </summary>
    private static LateEarlyLogCustom023ReportRowDto BuildAttendanceRow(
        LateEarlyLogEmployeeInfo employee,
        LateEarlyLogOrgAssignmentInfo org,
        LateEarlyLogTimePolicyInfo timePolicy,
        LateEarlyLogDutyInfo duty,
        List<LateEarlyLogTimeEventInfo> dayEvents,
        List<LateEarlyLogAbsenceInfo> absences,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        bool isWeekOff,
        bool isRestDay,
        bool isPublicHoliday,
        bool useArabicNames)
    {
        var row = new LateEarlyLogCustom023ReportRowDto
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

        // ---- Punch selection (legacy lines 1140-1180). NOTE: unlike the base control and Custom019 there
        // is NO week-off widening here, so a punch on a midnight-to-midnight week-off duty falls outside
        // both windows and is never selected.
        var punchInCandidates = SelectPunchCandidates(
            dayEvents, timePolicy, duty, dayDate, shiftStartTime, shiftEndTime, isNightShift,
            PunchInTransTypeCode);
        var punchOutCandidates = SelectPunchCandidates(
            dayEvents, timePolicy, duty, dayDate, shiftStartTime, shiftEndTime, isNightShift,
            PunchOutTransTypeCode);

        var punchIn = SelectIn(punchInCandidates, timePolicy.TimeEventRule);
        var punchOut = SelectOut(punchOutCandidates, timePolicy.TimeEventRule);

        // ---- Sign In / Early In / Late In (legacy lines 1063-1090 of the extracted body). The whole block
        // is gated on `!isWeekOff`, so a punch made on a week off is not reported at all -- and neither
        // ActualIn nor ActualOut is set, which in turn zeroes Total Hours for that day.
        DateTime actualIn = default;
        DateTime actualOut = default;

        if (punchIn != null && !isWeekOff)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = punchIn.TransTime.TimeOfDay;

            // TIME-OF-DAY DELTAS, not timestamp deltas. See the class remarks.
            var punchInTimeOfDay = punchIn.TransTime.TimeOfDay;
            if (shiftStartTime > punchInTimeOfDay)
            {
                row.EarlyIn = shiftStartTime - punchInTimeOfDay;
            }

            if (shiftStartTime < punchInTimeOfDay)
            {
                row.LateIn = punchInTimeOfDay - shiftStartTime;
            }

            row.PunchInLocation = punchIn.WorkLocation;
            row.PunchInAccessUnit = punchIn.AccessUnit;
        }

        // ---- Sign Out / Early Out / Late Out.
        if (punchOut != null && !isWeekOff)
        {
            actualOut = punchOut.TransDateTime;
            row.SignOut = punchOut.TransTime.TimeOfDay;

            var punchOutTimeOfDay = punchOut.TransTime.TimeOfDay;
            if (shiftEndTime > punchOutTimeOfDay)
            {
                row.EarlyOut = shiftEndTime - punchOutTimeOfDay;
            }

            if (shiftEndTime < punchOutTimeOfDay)
            {
                row.LateOut = punchOutTimeOfDay - shiftEndTime;
            }

            row.PunchOutLocation = punchOut.WorkLocation;
            row.PunchOutAccessUnit = punchOut.AccessUnit;
        }

        // ---- Status (legacy lines 1157-1191). Five-rung ladder; see the class remarks.
        if (isWeekOff)
        {
            row.Status = useArabicNames ? WeekOffAr : WeekOffEn;
        }
        else if (isRestDay)
        {
            row.Status = useArabicNames ? RestDayAr : RestDayEn;
        }
        else if (isPublicHoliday)
        {
            row.Status = useArabicNames ? PublicHolidayAr : PublicHolidayEn;
        }
        else if (punchInCandidates.Count > 0 != punchOutCandidates.Count > 0)
        {
            // Exactly one of the two lists has punches -- legacy wrote a bare asterisk.
            row.Status = "*";
        }
        else if (punchInCandidates.Count == 0 && punchOutCandidates.Count == 0)
        {
            // Legacy built this literal in English only, even in the Arabic aspect. Reproduced.
            row.Status = "Absent";
        }

        // else: BOTH lists have punches -- legacy wrote nothing at all, leaving the cell DBNull. Status
        // therefore stays null here, NOT string.Empty as the base control would have it.

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

        // ---- Hours arithmetic (legacy lines 1193-1229 of the extracted body). NOTE that NONE of the three
        // comparisons below is gated on `isWeekOff`, unlike the base control and Custom019: a week off with
        // a non-zero planned shift therefore reports full Absence Hours.
        var plannedHours = ComputePlannedHours(shiftStartTime, shiftEndTime, isNightShift);
        var totalHours = TimeSpan.Zero;
        var overtimeHours = TimeSpan.Zero;
        var absenceHours = TimeSpan.Zero;

        if (actualIn != default && actualOut != default)
        {
            totalHours = actualOut.Subtract(actualIn);
        }

        if (totalHours > plannedHours)
        {
            overtimeHours = totalHours.Subtract(plannedHours);
        }

        if (org != null)
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

        if (totalHours < plannedHours)
        {
            absenceHours = plannedHours.Subtract(totalHours);
        }

        // PUBLIC-HOLIDAY BLACKOUT, REPRODUCED: there is deliberately no `else if (isPublicHoliday)` branch
        // reporting Total Hours, unlike the base control. See the class remarks.
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

        if (overtimeHours != TimeSpan.Zero)
        {
            row.OvertimeHours = overtimeHours;
        }

        return row;
    }

    /// <summary>
    /// Legacy line 1123: <c>!isNightShift ? shiftEnd - shiftStart : shiftStart - shiftEnd</c>, over the RAW
    /// shift times rather than the resolved shift timestamps.
    ///
    /// Legacy guarded this with <c>shiftStartTime != default(DateTime) &amp;&amp; shiftEndTime !=
    /// default(DateTime)</c>, but those locals were seeded with <c>DateTime.Parse("1900-01-01 00:00:00")</c>
    /// and a Duty's times are also 1900-based, so the guard was ALWAYS TRUE and is therefore not
    /// reproduced as a guard here -- reproducing it as <c>!= TimeSpan.Zero</c> would silently change
    /// behaviour for a midnight-starting shift.
    /// </summary>
    private static TimeSpan ComputePlannedHours(TimeSpan shiftStartTime, TimeSpan shiftEndTime, bool isNightShift)
    {
        return isNightShift ? shiftStartTime - shiftEndTime : shiftEndTime - shiftStartTime;
    }

    /// <summary>
    /// Legacy lines 1140-1180. On the FlexiTime branch Custom023 uses the duty's real
    /// <c>FlexiEndTime</c> and applies no transaction-type filter, so both lists are the same set.
    /// </summary>
    private static List<LateEarlyLogTimeEventInfo> SelectPunchCandidates(
        List<LateEarlyLogTimeEventInfo> dayEvents,
        LateEarlyLogTimePolicyInfo timePolicy,
        LateEarlyLogDutyInfo duty,
        DateTime dayDate,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        string transTypeCode)
    {
        if (timePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            // Legacy dereferenced `duty` here without a null check, so a day whose Duty did not resolve
            // threw and blanked the whole report. Treated as a genuine defect rather than reproduced.
            if (duty == null)
            {
                return new List<LateEarlyLogTimeEventInfo>();
            }

            var flexiStart = duty.FlexiStartTime ?? TimeSpan.Zero;
            var flexiEnd = duty.FlexiEndTime ?? TimeSpan.Zero;

            var flexiStartWindow = dayDate.Add(flexiStart);
            var flexiEndWindow = isNightShift
                ? dayDate.Add(flexiEnd).AddDays(1)
                : dayDate.Add(flexiEnd);

            return dayEvents
                .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                .ToList();
        }

        DateTime windowStart;
        DateTime windowEnd;

        if (transTypeCode == PunchInTransTypeCode)
        {
            windowStart = dayDate.Add(shiftStartTime).AddMinutes(-timePolicy.ClockInLimit);
            windowEnd = dayDate.Add(shiftStartTime).AddMinutes(timePolicy.ClockInLimit);
        }
        else
        {
            var shiftEndBase = isNightShift ? dayDate.AddDays(1).Add(shiftEndTime) : dayDate.Add(shiftEndTime);
            windowStart = shiftEndBase.AddMinutes(-timePolicy.ClockOutLimit);
            windowEnd = shiftEndBase.AddMinutes(timePolicy.ClockOutLimit);
        }

        return dayEvents
            .Where(e => e.TransTypeCode == transTypeCode
                        && windowStart <= e.TransDateTime
                        && windowEnd >= e.TransDateTime)
            .ToList();
    }

    private static LateEarlyLogTimeEventInfo SelectIn(
        List<LateEarlyLogTimeEventInfo> candidates, TimeEventRule rule)
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

    private static LateEarlyLogTimeEventInfo SelectOut(
        List<LateEarlyLogTimeEventInfo> candidates, TimeEventRule rule)
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
    /// Fills the seven legacy OrgLevel columns after materialization. Shared by the read and the export so
    /// the two can never drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom023ReportRowDto> rows)
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
    /// The exported column set, in legacy column order (lines 793-827). The technical Oid row-sequence
    /// column is omitted -- legacy never populated it on this variant anyway.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom023ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom023ReportRowDto>>
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
}
