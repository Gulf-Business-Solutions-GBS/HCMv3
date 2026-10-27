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
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log (Custom 019) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom019.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 733-1735).
///
/// WHY THIS IS NOT THE BASE REPORT WITH FLAGS. Custom019 shares the base control's skeleton but differs
/// in four ways that change what a row MEANS, not merely which columns are shown:
/// <list type="number">
/// <item><description>A <c>cmbVarient</c> combo (lines 298-304) with three modes. Two of them REGROUP the
/// result to one row per (employee, shift) and sum the durations (lines 1652-1730) -- a different grain
/// from every other report in the family.</description></item>
/// <item><description>The payroll-area filter is replaced by THREE org-hierarchy filters,
/// department/division/section = legacy OrgLevel2/3/4 codes (lines 776-800).</description></item>
/// <item><description>The break machinery is DEAD: Custom019 still selects break punches (lines 1385-1425)
/// but every write of Break Out / Break In / Break Hours is commented out, and -- crucially -- so are the
/// two subtractions that reduced Planned Hours by the break-shift length and Total Hours by the break
/// length (lines 1596-1610). Its hours are therefore GROSS where the base's are net of break.</description></item>
/// <item><description>The full-day-leave row gates Absence Hours on <c>AbsenceType.IsPaid</c>
/// (line 962).</description></item>
/// </list>
///
/// GRAIN (Detail): one row per (ACTIVE employee, calendar day in the window), but only for employees with
/// at least one duty-period assignment in the window (legacy <c>dsCount &gt; 0</c> guard) and only for
/// days a duty period covers. Within that, a day produces exactly one of three row shapes: a FULL-DAY
/// LEAVE row, a NO-RETURN-DATE row, or the normal attendance row -- and the normal row is skipped
/// entirely when the employee has no time policy covering the day.
///
/// IN-MEMORY BY NECESSITY: see <see cref="LateEarlyLogCustomDataLoader"/>. The window-bounded input sets
/// are materialized once and the rows generated here, then handed back as an <c>IQueryable</c> so paging,
/// sorting and the export all go through one path.
///
/// STICKY SHIFT DEFECT (legacy lines 1015-1021, REPRODUCED and pinned by
/// <c>Should_Carry_Previous_Shift_Bounds_Into_A_Day_Without_Duty_Reproducing_Legacy_Defect</c>): the four
/// shift locals are declared OUTSIDE both loops and are only assigned when a Duty resolves, so a day
/// whose Duty is null reports the LAST successfully resolved day's shift bounds. Faithfulness is the
/// standing rule for this wave, so they are carried across iterations here in the same way.
///
/// ZERO-ABSENCE-HOURS DEFECT (legacy lines 951-963, REPRODUCED and pinned by
/// <c>Should_Report_Zero_Absence_Hours_On_An_Unpaid_Full_Day_Leave_Reproducing_Legacy_Defect</c>): on a
/// full-day leave the base control assigns <c>absencehours = plannedHours</c> before writing the cell.
/// Custom019 DELETED that assignment but kept the write, so an UNPAID full-day leave reports 00:00:00
/// absence hours instead of the planned shift length. Flagged, reproduced, pinned.
///
/// AGREED DIVERGENCE: legacy applied RLS here (line 902) and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up/drill-down session stack,
/// the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the
/// dead <c>Table1_Old()</c> (lines 1737-2720).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom019)]
public class LateEarlyLogCustom019ReportAppService : GridReportAppServiceBase,
    ILateEarlyLogCustom019ReportAppService
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

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly LateEarlyLogCustomDataLoader _dataLoader;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogCustom019ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        LateEarlyLogCustomDataLoader dataLoader,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _dataLoader = dataLoader;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom019";

    protected override string ScreenCaption => "Late Early Log (Custom 019)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom019ReportRowDto>> GetLateEarlyLogCustom019Report(
        GetLateEarlyLogCustom019ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await StampOrgLevelsAsync(rows, input.Variant);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildCustom019ParameterString(input),
            totalCount);

        return new PagedResultDto<LateEarlyLogCustom019ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom019ReportToExcel(GetLateEarlyLogCustom019ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows, input.Variant);

        return await ExportToFileAsync(
            "LateEarlyLogCustom019.xlsx",
            rows,
            GetExportColumns(input.Variant),
            BuildCustom019ParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (lines 700-715) logs the variant and the three org filters on top of
    /// the employee code and the date range. The base helper appends extra parts at the END whereas legacy
    /// printed them FIRST -- an ordering difference inside a free-text audit field.
    /// </summary>
    private string BuildCustom019ParameterString(GetLateEarlyLogCustom019ReportInput input)
    {
        return BuildParameterString(
            input,
            "Varient : " + input.Variant,
            input.DepartmentCode.IsNullOrWhiteSpace() ? null : "Department : " + input.DepartmentCode,
            input.DivisionCode.IsNullOrWhiteSpace() ? null : "Division : " + input.DivisionCode,
            input.SectionCode.IsNullOrWhiteSpace() ? null : "Section : " + input.SectionCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid. Ports legacy
    /// <c>Table1()</c>'s "Apply Filter", "Load Initial Data", "Check For Each Data" and variant-grouping
    /// regions (lines 764-1733).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom019ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom019ReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var today = Today;

        if (fromDate > toDate)
        {
            return new List<LateEarlyLogCustom019ReportRowDto>().AsQueryable();
        }

        // Legacy lines 776-800: the three org combos are ANDed, each matched against an org assignment
        // covering TODAY. In HCMv3 the seven legacy OrgLevelN foreign keys collapse into one hierarchical
        // OrganizationUnit, so "the assignment's OrgLevel3 has code X" becomes "the assignment's leaf unit
        // is a descendant-or-self of the Level3 unit whose profile code is X" -- and because Zero's
        // OrganizationUnit.Code is a dot-separated path, descendant-or-self is exactly a string prefix.
        var departmentPrefix = await ResolveOrgUnitPrefixAsync(
            OrganizationUnitType.Level2Code, input.DepartmentCode);
        var divisionPrefix = await ResolveOrgUnitPrefixAsync(
            OrganizationUnitType.Level3Code, input.DivisionCode);
        var sectionPrefix = await ResolveOrgUnitPrefixAsync(
            OrganizationUnitType.Level4Code, input.SectionCode);

        // A code that matches no org unit must select NOTHING, exactly as legacy's `OrgLevelN.Code == x`
        // did -- not "everything", which is what silently dropping the filter would mean.
        if (departmentPrefix == UnmatchedOrgFilter
            || divisionPrefix == UnmatchedOrgFilter
            || sectionPrefix == UnmatchedOrgFilter)
        {
            return new List<LateEarlyLogCustom019ReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy lines 765-902: ACTIVE employees only (hard-coded, there is no status combo), then the org
        // filters as of TODAY, then the comma-split code list, then RLS.
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .WhereIf(departmentPrefix != null, e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnit != null
                          && o.OrganizationUnit.Code.StartsWith(departmentPrefix)))
            .WhereIf(divisionPrefix != null, e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnit != null
                          && o.OrganizationUnit.Code.StartsWith(divisionPrefix)))
            .WhereIf(sectionPrefix != null, e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnit != null
                          && o.OrganizationUnit.Code.StartsWith(sectionPrefix)))
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
            return new List<LateEarlyLogCustom019ReportRowDto>().AsQueryable();
        }

        var data = await _dataLoader.LoadAsync(
            employees,
            fromDate,
            toDate,
            useArabicNames,
            isRosterActive,
            // Legacy line 926 widened the punch window one day BACKWARD, which the base control does not.
            new LateEarlyLogCustomLoadOptions { TimeEventLookbackDays = 1 });

        var rows = await BuildDetailRowsAsync(data, fromDate, toDate, useArabicNames);

        if (input.Variant != LateEarlyLogCustom019Variant.Detail)
        {
            rows = BuildSummaryRows(rows, input.Variant);
        }

        // Legacy set the "Oid" cell to `table.Rows.Count + 1`, a 1-based ROW SEQUENCE number rather than a
        // database id. In Detail it never actually assigned it (line 978 is inside the summary block only),
        // so the detail grid showed a blank Oid; the Id is stamped here regardless because the Angular grid
        // needs a stable row key. The column is not exported.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>Sentinel meaning "the user picked a code that matches no org unit" -- select nothing.</summary>
    private const string UnmatchedOrgFilter = " unmatched";

    /// <summary>
    /// Maps a legacy OrgLevelN CODE to the hierarchical <c>OrganizationUnit.Code</c> prefix that selects
    /// that unit and all its descendants. Returns null when no filter was supplied and
    /// <see cref="UnmatchedOrgFilter"/> when the supplied code matches nothing.
    /// </summary>
    private async Task<string> ResolveOrgUnitPrefixAsync(string levelCode, string filterCode)
    {
        if (filterCode.IsNullOrWhiteSpace())
        {
            return null;
        }

        var prefix = await _orgUnitProfileRepository.GetAll()
            .Where(p => p.OrganizationUnitType.Code == levelCode && p.Code == filterCode)
            .Select(p => p.OrganizationUnit.Code)
            .FirstOrDefaultAsync();

        return prefix ?? UnmatchedOrgFilter;
    }

    /// <summary>
    /// The Detail variant's rows -- legacy's "&lt;&lt; Check For Each Data &gt;&gt;" region
    /// (lines 302-917 of the extracted body, i.e. lines 1034-1649 of the file).
    /// </summary>
    private async Task<List<LateEarlyLogCustom019ReportRowDto>> BuildDetailRowsAsync(
        LateEarlyLogCustomDataSet data,
        DateTime fromDate,
        DateTime toDate,
        bool useArabicNames)
    {
        var rows = new List<LateEarlyLogCustom019ReportRowDto>();

        // ---- STICKY SHIFT DEFECT: these live OUTSIDE both loops in legacy and are only reassigned when a
        // Duty resolves, so they leak into a day that has no Duty. Reproduced. See the class remarks.
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;
        DateTime actualShiftStartTime = default;
        DateTime actualShiftEndTime = default;
        DateTime breakShiftStartTime = default;
        DateTime breakShiftEndTime = default;

        foreach (var employee in data.Employees)
        {
            var publicHolidays = await _dataLoader.GetPublicHolidayDatesAsync(employee.Id, fromDate, toDate);

            // Legacy `dsCount > 0`: an employee with NO duty-period assignment in the window contributes no
            // rows at all, whatever their punches or absences say.
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
                var isNightShift = false;
                var hasBreakShift = false;

                if (duty != null)
                {
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
                }

                var isPublicHoliday = publicHolidays.Contains(dayDate.Date);

                // Legacy lines 1153-1157: a full-day leave is an absence with AbsenceHours == 0.
                var fullDayLeave = data.Absences.FirstOrDefault(a => a.EmployeeId == employee.Id
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
                        timePolicy,
                        isPaidLeave: fullDayLeave.IsPaid));
                    continue;
                }

                // Legacy lines 1200-1213: an absence that has ENDED, requires a return date, has none, and
                // ends after the employee's latest recorded return date.
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
                    // Legacy lines 1215-1253: the same header block as the leave row, but with no leave
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
                        isPaidLeave: false,
                        includeHours: false));
                    continue;
                }

                // Legacy line 1259: no time policy covering the day means NO ROW for that day.
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
                    data.Absences,
                    dayDate,
                    weekDay,
                    shiftStartTime,
                    shiftEndTime,
                    actualShiftStartTime,
                    actualShiftEndTime,
                    breakShiftStartTime,
                    breakShiftEndTime,
                    isNightShift,
                    isWeekOff,
                    isPublicHoliday,
                    hasBreakShift,
                    useArabicNames));
            }
        }

        return rows;
    }

    /// <summary>
    /// The full-day-leave row (legacy lines 1158-1196) and, with <paramref name="includeHours"/> false,
    /// the no-return-date row (lines 1215-1253), which share the same header block.
    /// </summary>
    private static LateEarlyLogCustom019ReportRowDto BuildLeaveRow(
        LateEarlyLogEmployeeInfo employee,
        LateEarlyLogOrgAssignmentInfo org,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        DateTime actualShiftStartTime,
        DateTime actualShiftEndTime,
        bool isPublicHoliday,
        string status,
        string leaveType,
        LateEarlyLogTimePolicyInfo timePolicy,
        bool isPaidLeave,
        bool includeHours = true)
    {
        var row = new LateEarlyLogCustom019ReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime,
            Status = status,
            LeaveType = leaveType,
            TimePolicyName = timePolicy?.TimePolicyName ?? string.Empty,
            WorkLocationName = org?.WorkLocation ?? string.Empty
        };

        if (!includeHours)
        {
            return row;
        }

        var plannedHours = TimeSpan.Zero;
        if (actualShiftStartTime != default && actualShiftEndTime != default)
        {
            plannedHours = actualShiftEndTime.Subtract(actualShiftStartTime);
        }

        // Legacy lines 951-963. Note that Planned Hours and Total Hours are written UNCONDITIONALLY here,
        // including when zero -- unlike the base control, which suppresses zero-valued duration cells.
        if (!isPublicHoliday)
        {
            row.PlannedHours = plannedHours;
            row.TotalHours = TimeSpan.Zero;

            // ZERO-ABSENCE-HOURS DEFECT, REPRODUCED: the base control sets `absencehours = plannedHours`
            // before this point; Custom019 deleted that line but kept the write, so an UNPAID full-day
            // leave reports 00:00:00 rather than the planned shift length. A PAID leave writes nothing.
            if (!isPaidLeave)
            {
                row.AbsenceHours = TimeSpan.Zero;
            }
        }

        return row;
    }

    /// <summary>
    /// The normal attendance row -- legacy lines 1262-1649: punch selection, early/late deltas, status and
    /// the planned/total/overtime/absence hour arithmetic.
    /// </summary>
    private static LateEarlyLogCustom019ReportRowDto BuildAttendanceRow(
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
        DateTime actualShiftStartTime,
        DateTime actualShiftEndTime,
        DateTime breakShiftStartTime,
        DateTime breakShiftEndTime,
        bool isNightShift,
        bool isWeekOff,
        bool isPublicHoliday,
        bool hasBreakShift,
        bool useArabicNames)
    {
        var row = new LateEarlyLogCustom019ReportRowDto
        {
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            OrganizationUnitId = org?.OrganizationUnitId,
            Date = dayDate,
            WeekDay = weekDay,
            ShiftStart = shiftStartTime,
            ShiftEnd = shiftEndTime
        };

        // ---- Punch selection (legacy lines 1327-1372).
        var punchInCandidates = SelectPunchCandidates(
            dayEvents, timePolicy, duty, dayDate, shiftStartTime, shiftEndTime, isNightShift, isWeekOff,
            PunchInTransTypeCode);
        var punchOutCandidates = SelectPunchCandidates(
            dayEvents, timePolicy, duty, dayDate, shiftStartTime, shiftEndTime, isNightShift, isWeekOff,
            PunchOutTransTypeCode);

        var punchIn = SelectIn(punchInCandidates, timePolicy.TimeEventRule);
        var punchOut = SelectOut(punchOutCandidates, timePolicy.TimeEventRule);

        // ---- Break punch selection (legacy lines 1385-1425) is DELIBERATELY OMITTED. Custom019 still
        // performs it, but every consumer of the result -- the Break Out / Break In / Break Hours cells and
        // the two subtractions that netted the break out of Planned and Total Hours -- is commented out
        // (lines 1427-1439 and 1596-1610). Reproducing the selection would compute values nothing reads.
        // `hasBreakShift`, `breakShiftStartTime` and `breakShiftEndTime` are threaded through only so the
        // signature keeps documenting that legacy had a break shift in scope here.
        _ = hasBreakShift;
        _ = breakShiftStartTime;
        _ = breakShiftEndTime;

        // ---- Sign In / Early In / Late In (legacy lines 1441-1466). NOTE the guard is on the punch
        // ALONE: unlike Custom023, Sign In is reported even on a week off; only the deltas are suppressed.
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
        }

        // ---- Sign Out / Early Out / Late Out (legacy lines 1468-1493).
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
        }

        // ---- Status (legacy lines 1495-1518).
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

        // ---- Partial leave (legacy lines 1520-1533): an absence covering the day with NON-zero hours.
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

        // ---- Hours arithmetic (legacy lines 1535-1649). GROSS of break: see the class remarks.
        var plannedHours = TimeSpan.Zero;
        var totalHours = TimeSpan.Zero;
        var overtimeHours = TimeSpan.Zero;
        var absenceHours = TimeSpan.Zero;

        if (actualShiftStartTime != default && actualShiftEndTime != default && !isWeekOff)
        {
            plannedHours = actualShiftEndTime.Subtract(actualShiftStartTime);
        }

        if (actualIn != default && actualOut != default)
        {
            totalHours = actualOut.Subtract(actualIn);
        }

        if (totalHours > plannedHours && !isWeekOff)
        {
            overtimeHours = totalHours.Subtract(plannedHours);
        }

        // Overtime is dropped entirely when the employee's sub-group does not pay overtime, and swallowed
        // when it does not exceed the policy's OTMinLimit MINUTES.
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
    /// Legacy lines 1327-1372. On the FlexiTime branch Custom019 uses the duty's real
    /// <c>FlexiEndTime</c> -- the base control's collapsed-window defect is NOT present here -- and applies
    /// no transaction-type filter, so both lists are the same set.
    /// </summary>
    private static List<LateEarlyLogTimeEventInfo> SelectPunchCandidates(
        List<LateEarlyLogTimeEventInfo> dayEvents,
        LateEarlyLogTimePolicyInfo timePolicy,
        LateEarlyLogDutyInfo duty,
        DateTime dayDate,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShift,
        bool isWeekOff,
        string transTypeCode)
    {
        if (timePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            // Legacy dereferenced `duty` here without a null check, so a day whose Duty did not resolve
            // threw and blanked the whole report. Treated as a genuine defect rather than reproduced:
            // an unresolvable duty yields no candidates instead of an exception.
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

        // Legacy lines 1349-1357: on a midnight-to-midnight week-off duty the window becomes the whole
        // calendar day, so any punch on a week off is still picked up.
        if (isWeekOff && shiftStartTime == TimeSpan.Zero && shiftEndTime == TimeSpan.Zero)
        {
            windowStart = dayDate.Date;
            windowEnd = dayDate.AddHours(23).AddMinutes(59).AddSeconds(59);
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
    /// The two Summary variants -- legacy lines 1658-1730. The detail rows are regrouped by
    /// (Code, Employee, Shift Start, Shift End) and the four early/late durations plus Total Hours are
    /// SUMMED, treating an unset cell as zero. "Summary (LateIn &gt; 3 Hours)" then keeps only groups whose
    /// summed Late In exceeds three hours.
    ///
    /// Note the grouping key includes the shift bounds, so an employee whose shift changed inside the
    /// window produces MORE THAN ONE summary row. That is legacy behaviour, not an accident of the port.
    /// </summary>
    private static List<LateEarlyLogCustom019ReportRowDto> BuildSummaryRows(
        List<LateEarlyLogCustom019ReportRowDto> detailRows,
        LateEarlyLogCustom019Variant variant)
    {
        var grouped = detailRows
            .GroupBy(r => new { r.EmployeeCode, r.EmployeeName, r.ShiftStart, r.ShiftEnd })
            .Select(g => new LateEarlyLogCustom019ReportRowDto
            {
                EmployeeCode = g.Key.EmployeeCode,
                EmployeeName = g.Key.EmployeeName,
                ShiftStart = g.Key.ShiftStart,
                ShiftEnd = g.Key.ShiftEnd,
                EarlyIn = Sum(g.Select(r => r.EarlyIn)),
                EarlyOut = Sum(g.Select(r => r.EarlyOut)),
                LateIn = Sum(g.Select(r => r.LateIn)),
                LateOut = Sum(g.Select(r => r.LateOut)),
                TotalHours = Sum(g.Select(r => r.TotalHours))
            })
            .ToList();

        if (variant == LateEarlyLogCustom019Variant.SummaryLateInOverThreeHours)
        {
            grouped = grouped.Where(r => r.LateIn > TimeSpan.FromHours(3)).ToList();
        }

        return grouped;
    }

    private static TimeSpan Sum(IEnumerable<TimeSpan?> values)
    {
        return new TimeSpan(values.Sum(v => v?.Ticks ?? 0));
    }

    /// <summary>
    /// Fills the legacy OrgLevel columns after materialization, shared by the read and the export so the
    /// two can never drift. Skipped on the summary variants, whose narrower table has no OrgLevel columns
    /// at all (legacy lines 1660-1670).
    /// </summary>
    private async Task StampOrgLevelsAsync(
        List<LateEarlyLogCustom019ReportRowDto> rows,
        LateEarlyLogCustom019Variant variant)
    {
        if (rows.Count == 0 || variant != LateEarlyLogCustom019Variant.Detail)
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
    /// The exported column set, in legacy column order. The technical Oid row-sequence column is omitted
    /// because it carries no information the reader does not already have -- and Custom019 never populated
    /// it on a detail row anyway.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom019ReportRowDto>> GetExportColumns(
        LateEarlyLogCustom019Variant variant)
    {
        if (variant != LateEarlyLogCustom019Variant.Detail)
        {
            // Legacy sumTable, lines 1660-1670.
            return new List<GridReportColumn<LateEarlyLogCustom019ReportRowDto>>
            {
                new("Code", r => r.EmployeeCode),
                new("Employee", r => r.EmployeeName),
                new("ShiftStart", r => r.ShiftStart),
                new("ShiftEnd", r => r.ShiftEnd),
                new("EarlyIn", r => r.EarlyIn),
                new("EarlyOut", r => r.EarlyOut),
                new("LateIn", r => r.LateIn),
                new("LateOut", r => r.LateOut),
                new("TotalHours", r => r.TotalHours)
            };
        }

        return new List<GridReportColumn<LateEarlyLogCustom019ReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            // OrgLevel1 and AccessId are commented out in the legacy control -- deliberately absent.
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
            new("TimePolicy", r => r.TimePolicyName),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }
}
