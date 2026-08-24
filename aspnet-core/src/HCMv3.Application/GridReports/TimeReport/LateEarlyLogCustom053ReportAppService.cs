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
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Custom053 Late/Early Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom053.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 624-1407).
///
/// THE MOST DIVERGENT OF THE LATE/EARLY LOG FAMILY, and the clearest evidence that these variants are
/// separate implementations rather than parameterisations of <see cref="LateEarlyLogReportAppService"/>.
/// It changes the report's central question: the base asks "did the employee meet THEIR OWN roster?",
/// Custom053 asks "did the employee meet the roster of the SITE THEY ACTUALLY LEFT FROM?". Three
/// structural changes follow from that:
///
/// <list type="number">
/// <item><description><b>Work-location-driven shift.</b> The day's LAST clock-out punch carrying a
/// non-blank work location (lines 949-952) selects a <c>WorkLocationDutyPeriod</c> covering the day
/// (lines 958-962); that duty period's day-of-week Duty -- the "wl duty" -- supplies the shift bounds and
/// the night-shift/week-off flags used for all the early/late arithmetic (lines 1024-1044). The
/// employee's own Duty is only the FALLBACK when the work location has no duty period
/// (line 1035).</description></item>
/// <item><description><b>Same-day punches only.</b> The base considers punches from the day before, the
/// day and the day after so a night shift can straddle midnight; Custom053 takes only
/// <c>TransDate == dayDate</c> (lines 945-946). A night shift's next-morning clock-out is therefore
/// invisible to this report.</description></item>
/// <item><description><b>Week-off compensate overtime.</b> A running per-employee counter of
/// "WeekOff Compensate" leave days -- absences of the AbsenceType whose <c>Code</c> is the hard-coded
/// literal <c>"18"</c> (line 891) -- decides how a week-off day's overtime is measured, and DECREMENTS as
/// week-off overtime days are consumed (lines 1339-1355). Nothing else in the family carries state across
/// days.</description></item>
/// </list>
///
/// It also DROPS the base's entire no-return-date row shape (there is no such branch here at all), the
/// three break columns and the Country column, and it changes Overtime Hours from a duration to DECIMAL
/// HOURS -- see <see cref="LateEarlyLogCustom053ReportRowDto.OvertimeHours"/>.
///
/// STICKY SHIFT DEFECT (legacy line 902, REPRODUCED): the two shift locals live OUTSIDE both loops and are
/// only assigned when a work-location-or-employee Duty resolves.
///
/// AGREED DIVERGENCE: RLS is applied here exactly as legacy applied it (line 773).
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill stack, the ColumnChooser
/// JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom053)]
public class LateEarlyLogCustom053ReportAppService : GridReportAppServiceBase,
    ILateEarlyLogCustom053ReportAppService
{
    private const string PunchInTransTypeCode = "1";

    private const string PunchOutTransTypeCode = "2";

    private const string LeaveEn = "Leave";

    private const string LeaveAr = "إجازة";

    private const string WeekOffEn = "Week Off";

    private const string WeekOffAr = "إسبوع إجازة";

    private const string PublicHolidayEn = "Public Holiday";

    private const string PublicHolidayAr = "عطلة رسمية";

    /// <summary>
    /// Legacy line 891: the "WeekOff Compensate" absence type is identified by the HARD-CODED
    /// <c>AbsenceType.Code</c> literal <c>"18"</c>, not by a setting or a flag on the type. Reproduced --
    /// on a tenant with no such code the counter is simply always zero and every week-off overtime day
    /// falls into the "no compensate leave" branch.
    /// </summary>
    private const string WeekOffCompensateAbsenceTypeCode = "18";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<WorkLocationDutyPeriod> _workLocationDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _substituteDutyRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRosterService _rosterService;
    private readonly ITimeDataService _timeDataService;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public LateEarlyLogCustom053ReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<WorkLocationDutyPeriod> workLocationDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
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
        _workLocationDutyPeriodRepository = workLocationDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _substituteDutyRepository = substituteDutyRepository;
        _dutyRepository = dutyRepository;
        _rosterService = rosterService;
        _timeDataService = timeDataService;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptLateEarlyLog_Custom053";

    protected override string ScreenCaption => "Late Early Log (Custom 053)";

    [HttpPost]
    public async Task<PagedResultDto<LateEarlyLogCustom053ReportRowDto>> GetLateEarlyLogCustom053Report(
        GetLateEarlyLogCustom053ReportInput input)
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

        return new PagedResultDto<LateEarlyLogCustom053ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetLateEarlyLogCustom053ReportToExcel(GetLateEarlyLogCustom053ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "LateEarlyLogCustom053.xlsx",
            rows,
            GetExportColumns(),
            BuildLateEarlyParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 606-622).</summary>
    private string BuildLateEarlyParameterString(GetLateEarlyLogCustom053ReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>'s "Apply Filter", "Load Initial Data" and "Check For Each Data"
    /// regions (lines 758-1403).
    /// </summary>
    private async Task<IQueryable<LateEarlyLogCustom053ReportRowDto>> BuildQueryAsync(
        GetLateEarlyLogCustom053ReportInput input)
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
            return new List<LateEarlyLogCustom053ReportRowDto>().AsQueryable();
        }

        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

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
            return new List<LateEarlyLogCustom053ReportRowDto>().AsQueryable();
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // The +1 day ceiling is still in the legacy query (line 803) even though the day loop then only
        // ever looks at same-day punches, so the extra day is loaded and never used. Kept for fidelity of
        // the input set; it changes nothing downstream.
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

        // CUSTOM053-ONLY INPUT SET (legacy lines 811-822): every work location's duty period overlapping
        // the window. Note the name is projected as NameEn UNCONDITIONALLY, while the punch's work
        // location above follows the aspect -- so in the Arabic aspect the two never match and the
        // work-location duty silently falls back to the employee's own. Legacy quirk, reproduced and
        // pinned by a test.
        var workLocationDutyPeriods = await _workLocationDutyPeriodRepository.GetAll()
            .Where(w => w.StartDate.Date <= toDate && w.EndDate.Date >= fromDate)
            .Select(w => new WorkLocationDutyPeriodInfo
            {
                WorkLocationName = w.WorkLocation.NameEn,
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                SundayDutyId = w.DutyPeriod.SundayDutyId,
                MondayDutyId = w.DutyPeriod.MondayDutyId,
                TuesdayDutyId = w.DutyPeriod.TuesdayDutyId,
                WednesdayDutyId = w.DutyPeriod.WednesdayDutyId,
                ThursdayDutyId = w.DutyPeriod.ThursdayDutyId,
                FridayDutyId = w.DutyPeriod.FridayDutyId,
                SaturdayDutyId = w.DutyPeriod.SaturdayDutyId
            })
            .ToListAsync();

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

        // Legacy also selects AbsenceTypeOid and TotalLeaveDays here (lines 246/254) for the week-off
        // compensate counter.
        var absences = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.StartDate.Date <= toDate
                        && a.EndDate.Date >= fromDate)
            .Select(a => new AbsenceInfo
            {
                EmployeeId = a.EmployeeId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                AbsenceTypeId = a.AbsenceTypeId,
                AbsenceTypeName = useArabicNames ? a.AbsenceType.NameAr : a.AbsenceType.NameEn,
                IsPaid = a.AbsenceType.IsPaid,
                AbsenceHours = a.AbsenceHours,
                TotalLeaveDays = a.TotalLeaveDays
            })
            .ToListAsync();

        // Legacy line 891.
        var weekOffCompensateAbsenceTypeId = await _absenceTypeRepository.GetAll()
            .Where(t => t.Code == WeekOffCompensateAbsenceTypeCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId)
                        && o.StartDate.Date <= toDate
                        && o.EndDate.Date >= fromDate)
            .Select(o => new OrgAssignmentInfo
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
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

        var rows = new List<LateEarlyLogCustom053ReportRowDto>();

        // ---- STICKY SHIFT DEFECT: these two locals live OUTSIDE both loops in legacy (line 902).
        var shiftStartTime = TimeSpan.Zero;
        var shiftEndTime = TimeSpan.Zero;

        foreach (var employee in employees)
        {
            // Legacy lines 906-913: the employee's stock of WeekOff Compensate leave days over the window.
            // Note the containment test is STRICT -- StartDate >= from AND EndDate <= to -- so a leave
            // straddling the window boundary is not counted at all. Reproduced.
            var weekOffLeaveCount = weekOffCompensateAbsenceTypeId == null
                ? 0d
                : absences
                    .Where(a => a.EmployeeId == employee.Id
                                && a.StartDate >= fromDate
                                && a.EndDate <= toDate
                                && a.AbsenceTypeId == weekOffCompensateAbsenceTypeId.Value)
                    .Sum(a => a.TotalLeaveDays);

            var publicHolidays = (await _timeDataService.GetPublicHolidaysDatesListAsync(employee.Id, fromDate, toDate))
                .Select(h => h.HolidayDate.Date)
                .ToHashSet();

            // NOTE: unlike the base and the other two customs, Custom053 has NO `dsCount > 0` guard -- an
            // employee with no duty-period assignment anywhere in the window is not skipped up front; the
            // per-day `dutyPeriod == null` test below is the only gate.
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

                // SAME-DAY PUNCHES ONLY (legacy lines 945-946), where the base takes the day before, the
                // day and the day after.
                var dayEvents = employeeEvents.Where(e => e.TransDate.Date == dayDate).ToList();

                // The day's LAST clock-out carrying a non-blank work location decides which site's roster
                // this day is measured against (lines 949-952).
                var workLocationAtPunchOut = dayEvents
                    .Where(e => e.TransTypeCode == PunchOutTransTypeCode && e.WorkLocation != string.Empty)
                    .OrderByDescending(e => e.TransDateTime)
                    .Select(e => e.WorkLocation)
                    .FirstOrDefault();

                var workLocationDutyPeriod = workLocationAtPunchOut == null
                    ? null
                    : workLocationDutyPeriods.FirstOrDefault(w => w.WorkLocationName == workLocationAtPunchOut
                                                                  && w.StartDate <= dayDate
                                                                  && w.EndDate >= dayDate);

                var dutyPeriod = dutyPeriodsByEmployee.TryGetValue(employee.Id, out var employeeDutyPeriods)
                    ? employeeDutyPeriods.FirstOrDefault(d => d.StartDate <= dayDate && d.EndDate >= dayDate)
                    : null;

                if (dutyPeriod == null)
                {
                    continue;
                }

                var weekDay = GetWeekDayName(dayDate, useArabicNames);

                // The EMPLOYEE's duty -- substitute duty or roster, exactly as the base resolves it.
                var duty = await ResolveDutyAsync(
                    employee.Id,
                    dutyPeriod,
                    dayDate,
                    isRosterActive,
                    substituteByEmployeeAndDate,
                    duties);

                var isWeekOff = duty != null && duty.IsWeeklyOff;

                // The WORK LOCATION's duty (lines 1024-1036). NO substitute duty and NO roster here: the
                // day-of-week Duty of the site's duty period, falling back to the employee's own Duty.
                var workLocationDuty = ResolveWorkLocationDuty(workLocationDutyPeriod, dayDate, duties) ?? duty;

                var isWeekOffAtWorkLocation = false;
                var isNightShiftAtWorkLocation = false;

                if (workLocationDuty != null)
                {
                    shiftStartTime = workLocationDuty.StartTime;
                    shiftEndTime = workLocationDuty.EndTime;
                    isNightShiftAtWorkLocation = workLocationDuty.IsNightShift;
                    isWeekOffAtWorkLocation = workLocationDuty.IsWeeklyOff;
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
                        isNightShiftAtWorkLocation,
                        isPublicHoliday,
                        useArabicNames ? LeaveAr : LeaveEn,
                        fullDayLeave.AbsenceTypeName,
                        fullDayLeave.IsPaid,
                        timePolicy));
                    continue;
                }

                // Legacy line 1121: no time policy covering the day means NO ROW. Note there is NO
                // no-return-date branch in Custom053 at all -- it goes straight from full-day leave to the
                // normal attendance row.
                if (timePolicy == null)
                {
                    continue;
                }

                rows.Add(BuildAttendanceRow(
                    employee,
                    org,
                    timePolicy,
                    workLocationDuty,
                    dayEvents,
                    absences,
                    dayDate,
                    weekDay,
                    shiftStartTime,
                    shiftEndTime,
                    isNightShiftAtWorkLocation,
                    isWeekOff,
                    isWeekOffAtWorkLocation,
                    isPublicHoliday,
                    useArabicNames,
                    ref weekOffLeaveCount));
            }
        }

        // Legacy's "Oid" cell is `table.Rows.Count + 1` -- a 1-based ROW SEQUENCE number, not an id.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows.AsQueryable();
    }

    /// <summary>The full-day-leave row -- legacy lines 1063-1116.</summary>
    private static LateEarlyLogCustom053ReportRowDto BuildLeaveRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShiftAtWorkLocation,
        bool isPublicHoliday,
        string status,
        string leaveType,
        bool isPaidLeave,
        TimePolicyInfo timePolicy)
    {
        var row = new LateEarlyLogCustom053ReportRowDto
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
            WorkLocationName = org?.WorkLocation ?? string.Empty
        };

        if (isPublicHoliday)
        {
            return row;
        }

        row.PlannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShiftAtWorkLocation);
        row.TotalHours = TimeSpan.Zero;

        // As in Custom045, the value written here is always ZERO -- the base's
        // `absencehours = plannedHours` is gone -- and it is written at all only for an UNPAID absence.
        if (!isPaidLeave)
        {
            row.AbsenceHours = TimeSpan.Zero;
        }

        return row;
    }

    /// <summary>The normal attendance row -- legacy lines 1120-1387.</summary>
    private static LateEarlyLogCustom053ReportRowDto BuildAttendanceRow(
        EmployeeInfo employee,
        OrgAssignmentInfo org,
        TimePolicyInfo timePolicy,
        DutyInfo workLocationDuty,
        List<TimeEventInfo> dayEvents,
        List<AbsenceInfo> absences,
        DateTime dayDate,
        string weekDay,
        TimeSpan shiftStartTime,
        TimeSpan shiftEndTime,
        bool isNightShiftAtWorkLocation,
        bool isWeekOff,
        bool isWeekOffAtWorkLocation,
        bool isPublicHoliday,
        bool useArabicNames,
        ref double weekOffLeaveCount)
    {
        var row = new LateEarlyLogCustom053ReportRowDto
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

        // ---- Punch selection (legacy lines 1155-1189): a single flexi window taken from the WORK
        // LOCATION's duty, no tolerance limits, no TimeMgmtStatus branch, no night-shift extension.
        //
        // Legacy dereferences `wlduty` unconditionally (lines 1161-1162) although it can be null when
        // neither the work location nor the employee has a resolvable Duty, which crashes the whole
        // report. That is a crash, not a data difference, so it is NOT reproduced: a null Duty yields an
        // empty window and therefore no punches.
        var flexiStart = workLocationDuty?.FlexiStartTime ?? TimeSpan.Zero;
        var flexiEnd = workLocationDuty?.FlexiEndTime ?? TimeSpan.Zero;

        var startWindow = dayDate.Add(flexiStart);
        var endWindow = dayDate.Add(flexiEnd);

        var windowEvents = dayEvents
            .Where(e => startWindow <= e.TransDateTime && endWindow >= e.TransDateTime)
            .ToList();

        var punchInCandidates = windowEvents.Where(e => e.TransTypeCode == PunchInTransTypeCode).ToList();
        var punchOutCandidates = windowEvents.Where(e => e.TransTypeCode == PunchOutTransTypeCode).ToList();

        var punchIn = SelectIn(punchInCandidates, timePolicy.TimeEventRule);
        var punchOut = SelectOut(punchOutCandidates, timePolicy.TimeEventRule);

        // ---- Sign In / Early In / Late In (legacy lines 1191-1217).
        DateTime actualIn = default;
        DateTime actualOut = default;

        if (punchIn != null)
        {
            actualIn = punchIn.TransDateTime;
            row.SignIn = punchIn.TransTime.TimeOfDay;

            // NOTE: the WORK LOCATION's week-off flag gates the arithmetic here, while the Status column
            // below uses the EMPLOYEE's. Legacy does exactly that (line 1198 vs line 1252).
            if (!isWeekOffAtWorkLocation)
            {
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

            // Legacy lines 1212-1213: NOT simply the selected punch's own values -- it re-scans the window
            // for ANY punch-in sharing the selected punch's exact timestamp and carrying a non-blank
            // value, so a badge reader that writes the location on a duplicate event still fills the cell.
            row.PunchInLocation = FirstNonBlank(windowEvents, PunchInTransTypeCode, punchIn.TransDateTime, e => e.WorkLocation);
            row.PunchInAccessUnit = FirstNonBlank(windowEvents, PunchInTransTypeCode, punchIn.TransDateTime, e => e.AccessUnit);
        }

        // ---- Sign Out / Early Out / Late Out (legacy lines 1219-1247).
        if (punchOut != null)
        {
            actualOut = punchOut.TransDateTime;
            row.SignOut = punchOut.TransTime.TimeOfDay;

            if (!isWeekOffAtWorkLocation)
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

            row.PunchOutLocation = FirstNonBlank(windowEvents, PunchOutTransTypeCode, punchOut.TransDateTime, e => e.WorkLocation);
            row.PunchOutAccessUnit = FirstNonBlank(windowEvents, PunchOutTransTypeCode, punchOut.TransDateTime, e => e.AccessUnit);
        }

        // ---- Status (legacy lines 1250-1272). The base's vocabulary, but gated on the EMPLOYEE's
        // week-off flag rather than the work location's.
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
            // English-only literals in legacy, even in the Arabic aspect. Reproduced.
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

        // ---- Hours arithmetic (legacy lines 1289-1384).
        var plannedHoursAtWorkLocation = TimeSpan.Zero;
        var plannedHours = TimeSpan.Zero;
        var totalHours = TimeSpan.Zero;
        var overtimeHours = TimeSpan.Zero;
        var absenceHours = TimeSpan.Zero;

        if (!isWeekOffAtWorkLocation)
        {
            plannedHoursAtWorkLocation =
                GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShiftAtWorkLocation);
        }

        if (!isWeekOff)
        {
            // LEGACY COPY-PASTE, REPRODUCED (line 1306): this is meant to be the EMPLOYEE's planned hours
            // but it is computed from the WORK LOCATION's shift bounds and night-shift flag -- identical
            // to the line above it except for which week-off flag gates it.
            plannedHours = GetPlannedShiftLength(shiftStartTime, shiftEndTime, isNightShiftAtWorkLocation);
        }

        if (actualIn != default && actualOut != default)
        {
            // No break deduction: Custom053 has no break columns.
            totalHours = actualOut.Subtract(actualIn);
        }

        // ---- THE WEEK-OFF COMPENSATE RULE (legacy lines 1339-1355).
        if (totalHours > plannedHours)
        {
            if (!isWeekOff)
            {
                overtimeHours = totalHours.Subtract(plannedHours);
            }
            else if (weekOffLeaveCount > 0)
            {
                // A compensate leave day is available: only the excess over the SITE's planned shift is
                // overtime, and one day of the stock is consumed.
                overtimeHours = totalHours.Subtract(plannedHoursAtWorkLocation);
                weekOffLeaveCount--;
            }
            else
            {
                // No compensate leave left: the entire day counts as overtime.
                overtimeHours = totalHours;
            }
        }

        // NOTE what is MISSING compared with the base and the other two customs: there is no
        // `EmployeeSubGroup.PayOvertime` check. Legacy has it, commented out (lines 1315-1327), and kept
        // only the OTMinLimit swallow -- still oddly guarded on `empOrg != null`, which now affects
        // nothing but whether the swallow runs at all. Reproduced.
        if (overtimeHours != TimeSpan.Zero && org != null)
        {
            var minimumOvertime = new TimeSpan(0, 0, (int)timePolicy.OTMinLimit, 0);
            if (overtimeHours <= minimumOvertime)
            {
                overtimeHours = TimeSpan.Zero;
            }
        }

        if (totalHours < plannedHours && !isWeekOff)
        {
            absenceHours = plannedHours.Subtract(totalHours);
        }

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
            // DECIMAL HOURS, rounded to three places -- the column is typeof(double) in legacy.
            row.OvertimeHours = Math.Round(overtimeHours.TotalHours, 3);
        }

        return row;
    }

    /// <summary>
    /// Legacy lines 1212-1213 / 1240-1241: among the punches of the given type sharing
    /// <paramref name="at"/> as their exact timestamp, the first with a non-blank value.
    /// </summary>
    private static string FirstNonBlank(
        List<TimeEventInfo> windowEvents,
        string transTypeCode,
        DateTime at,
        Func<TimeEventInfo, string> selector)
    {
        return windowEvents
            .Where(e => e.TransTypeCode == transTypeCode
                        && e.TransDateTime == at
                        && selector(e) != string.Empty)
            .Select(selector)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy's planned-shift length: <c>end - start</c> normally but <c>start - end</c> on a night shift
    /// (line 1301). Bare times, so a 22:00-06:00 night shift yields SIXTEEN hours rather than eight.
    /// Reproduced and pinned by a test.
    /// </summary>
    private static TimeSpan GetPlannedShiftLength(TimeSpan shiftStartTime, TimeSpan shiftEndTime, bool isNightShift)
    {
        return isNightShift ? shiftStartTime - shiftEndTime : shiftEndTime - shiftStartTime;
    }

    /// <summary>
    /// Legacy lines 979-986: the day name through a hard-coded Arabic literal table rather than a resource
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
    /// Legacy lines 1024-1034: the WORK LOCATION duty period's day-of-week Duty. Deliberately WITHOUT the
    /// substitute-duty and roster resolution the employee's own duty goes through -- a site's roster is
    /// not substituted per employee.
    /// </summary>
    private static DutyInfo ResolveWorkLocationDuty(
        WorkLocationDutyPeriodInfo workLocationDutyPeriod,
        DateTime dayDate,
        Dictionary<int, DutyInfo> duties)
    {
        if (workLocationDutyPeriod == null)
        {
            return null;
        }

        var dutyId = dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => workLocationDutyPeriod.SundayDutyId,
            DayOfWeek.Monday => workLocationDutyPeriod.MondayDutyId,
            DayOfWeek.Tuesday => workLocationDutyPeriod.TuesdayDutyId,
            DayOfWeek.Wednesday => workLocationDutyPeriod.WednesdayDutyId,
            DayOfWeek.Thursday => workLocationDutyPeriod.ThursdayDutyId,
            DayOfWeek.Friday => workLocationDutyPeriod.FridayDutyId,
            _ => workLocationDutyPeriod.SaturdayDutyId
        };

        return dutyId.HasValue && duties.TryGetValue(dutyId.Value, out var duty) ? duty : null;
    }

    /// <summary>
    /// Legacy lines 995-1015: with the roster off, the employee's substitute duty for that exact date wins
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

    private async Task StampOrgLevelsAsync(List<LateEarlyLogCustom053ReportRowDto> rows)
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
    /// The exported column set, in legacy DataTable order (lines 726-753): the base set MINUS the three
    /// break columns and MINUS Country. The technical Oid row-sequence column is omitted as on the base
    /// report.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<LateEarlyLogCustom053ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<LateEarlyLogCustom053ReportRowDto>>
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

    /// <summary>Custom053-only input set: a work location's duty period over the window.</summary>
    private class WorkLocationDutyPeriodInfo
    {
        /// <summary>Always the ENGLISH name -- see the projection remarks.</summary>
        public string WorkLocationName { get; set; }

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

        public TimeEventRule TimeEventRule { get; set; }

        public double OTMinLimit { get; set; }
    }

    private class AbsenceInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int AbsenceTypeId { get; set; }

        public string AbsenceTypeName { get; set; }

        public bool IsPaid { get; set; }

        public double AbsenceHours { get; set; }

        /// <summary>Custom053-only: feeds the week-off compensate counter.</summary>
        public double TotalLeaveDays { get; set; }
    }

    private class OrgAssignmentInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string WorkLocation { get; set; }

        public long? OrganizationUnitId { get; set; }
    }
}
