using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// SHARED IMPLEMENTATION of the two "geo" Late/Early Log variants,
/// <c>uscRptLateEarlyLog_Custom033</c> and <c>uscRptLateEarlyLog_Custom036</c>.
///
/// WHY THEY SHARE. A line-by-line diff of the two live <c>Table1()</c> bodies (Custom033 lines 619-1192,
/// Custom036 lines 617-1116) shows their ONLY behavioural difference is that Custom036 drops
/// <c>PunchInAccessUnit</c> and <c>PunchOutAccessUnit</c>: the two column declarations, the two
/// <c>dr[...] = ""</c> blanking statements and the two <c>te_Punch*.AccessUnit</c> assignments. Everything
/// else -- filters, loads, duty resolution, punch selection, status ladder, hours arithmetic -- is
/// identical modulo whitespace. Porting them as two copies would be two places for one algorithm to rot,
/// so the algorithm lives here once and the two services differ only in their column list.
///
/// HOW THIS ALGORITHM DIFFERS FROM THE BASE LATE/EARLY LOG. It is much cruder, and deliberately so:
/// <list type="number">
/// <item><description>NIGHT SHIFT IS HARD-CODED TRUE (<c>isNightShift = true; //isNightShift =
/// duty.IsNightShift;</c>, Custom033 line 961). Every day is treated as straddling midnight, so the punch
/// window always extends into the following calendar day regardless of the duty's real flag. Reproduced
/// and pinned by <c>Should_Treat_Every_Duty_As_A_Night_Shift_Reproducing_Legacy_Defect</c>.</description></item>
/// <item><description>THE TIME POLICY'S MANAGEMENT STATUS IS IGNORED. Where every other variant branches
/// on <c>TimeMgmtStatus == FlexiTime</c>, these two ALWAYS use the duty's flexi window and never build the
/// clock-in/clock-out windows from the policy's limits at all. <c>ClockInLimit</c>, <c>ClockOutLimit</c>,
/// <c>BreakInLimit</c>, <c>BreakOutLimit</c>, <c>TimeEventRule</c> and <c>OTMinLimit</c> are loaded and
/// never read.</description></item>
/// <item><description>PUNCH SELECTION IGNORES THE TRANSACTION TYPE. Both candidate lists are the same
/// unfiltered set, so "Sign In" is simply the earliest event in the window and "Sign Out" the latest one
/// that is not at the very same instant -- a clock-OUT punch can become the Sign In.</description></item>
/// <item><description>TOTAL HOURS IS WRITTEN EVEN WHEN ZERO, and is never reduced by a break.</description></item>
/// </list>
///
/// TWO UNREACHABLE LEGACY BRANCHES, both flagged rather than reproduced as behaviour:
/// <list type="bullet">
/// <item><description>THE FULL-DAY-LEAVE BRANCH IS DEAD (Custom033 lines 979-1024). It writes
/// <c>dr[Status] = "Leave"</c> but does NOT <c>continue</c>, and the normal-attendance block that follows
/// overwrites Status unconditionally. When there IS a time policy the "Leave" marker is destroyed; when
/// there is not, the row is never added. Either way no user ever saw it. This is a genuine DATA-LOSS
/// defect -- a full-day leave is indistinguishable from a day with no punches -- and it is called out
/// here rather than silently reproduced; the port matches the OBSERVABLE legacy output, which is a row
/// with no Leave status.</description></item>
/// <item><description>THE "*" STATUS IS UNREACHABLE (Custom033 line 1085). It fires when exactly one of
/// the two candidate lists is non-empty, but the two lists are built from IDENTICAL predicates, so they
/// are always both empty or both non-empty. The port keeps the branch shape so the correspondence with
/// legacy is legible, and a test pins that it never fires.</description></item>
/// </list>
///
/// STICKY SHIFT DEFECT (Custom033 lines 890-893): the shift locals live OUTSIDE both loops and are only
/// assigned when a Duty resolves, so a day whose Duty is null reports the LAST successfully resolved
/// day's shift bounds. Reproduced.
///
/// AGREED DIVERGENCE: legacy applied RLS here and so does this port.
/// </summary>
public abstract class LateEarlyLogGeoVariantReportAppServiceBase : GridReportAppServiceBase
{
    /// <summary>Legacy status literals. Hard-coded there, hard-coded here.</summary>
    protected const string WeekOffEn = "Week Off";

    protected const string WeekOffAr = "إسبوع إجازة";

    protected const string PublicHolidayEn = "Public Holiday";

    protected const string PublicHolidayAr = "عطلة رسمية";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly LateEarlyLogCustomDataLoader _dataLoader;

    protected LateEarlyLogGeoVariantReportAppServiceBase(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        LateEarlyLogCustomDataLoader dataLoader)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _dataLoader = dataLoader;
    }

    /// <summary>
    /// Ports the shared "Apply Filter", "Load Initial Data" and "Check For Each Data" regions and returns
    /// the neutral rows both variants project from. Called by BOTH the paged read and the export of both
    /// services, which is what guarantees the four code paths cannot drift.
    /// </summary>
    protected async Task<List<LateEarlyLogGeoVariantRow>> BuildRowsAsync(
        GridReportInputBase input,
        string payrollAreaCode)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var hasPayrollAreaFilter = !payrollAreaCode.IsNullOrWhiteSpace();
        var today = Today;

        var rows = new List<LateEarlyLogGeoVariantRow>();

        if (fromDate > toDate)
        {
            return rows;
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
            return rows;
        }

        var data = await _dataLoader.LoadAsync(
            employees,
            fromDate,
            toDate,
            useArabicNames,
            isRosterActive,
            // Legacy filtered AbsenceHours == 0 in the query: these two have no partial-leave columns.
            new LateEarlyLogCustomLoadOptions { FullDayAbsencesOnly = true });

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
                var isNightShift = false;

                if (duty != null)
                {
                    shiftStartTime = duty.StartTime;
                    shiftEndTime = duty.EndTime;
                    // HARD-CODED, not `duty.IsNightShift`. See the class remarks.
                    isNightShift = true;
                    isWeekOff = duty.IsWeeklyOff;
                }

                var isPublicHoliday = publicHolidays.Contains(dayDate.Date);

                // The full-day-leave branch legacy has here is DEAD -- see the class remarks. Nothing it
                // wrote ever survived to the grid, so nothing is written here either.

                // Legacy: no time policy covering the day means NO ROW for that day.
                if (timePolicy == null)
                {
                    continue;
                }

                var row = new LateEarlyLogGeoVariantRow
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
                    WorkLocationName = org?.WorkLocation ?? string.Empty,
                    CountryName = org?.PayrollCountry ?? string.Empty
                };

                // ---- Punch selection. Legacy dereferenced `duty` here with no null check, so a day whose
                // Duty did not resolve threw and blanked the whole report. Treated as a genuine defect
                // rather than reproduced: an unresolvable duty yields no candidates instead of an
                // exception. NOTE both lists use the SAME predicate -- that is legacy, not a typo here.
                // Legacy's TimeEventList1 / TimeEventList2, kept as two names because the status ladder
                // below compares them -- see the unreachable "*" branch.
                var candidateList1 = new List<LateEarlyLogTimeEventInfo>();
                var candidateList2 = new List<LateEarlyLogTimeEventInfo>();

                if (duty != null)
                {
                    var flexiStart = duty.FlexiStartTime ?? TimeSpan.Zero;
                    var flexiEnd = duty.FlexiEndTime ?? TimeSpan.Zero;

                    var flexiStartWindow = dayDate.Add(flexiStart);
                    // isNightShift is unconditionally true whenever duty resolved, so this always adds a
                    // day. Written out in full so the correspondence with legacy stays legible.
                    var flexiEndWindow = isNightShift
                        ? dayDate.Add(flexiEnd).AddDays(1)
                        : dayDate.Add(flexiEnd);

                    candidateList1 = dayEvents
                        .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                        .ToList();
                    candidateList2 = dayEvents
                        .Where(e => flexiStartWindow <= e.TransDateTime && flexiEndWindow >= e.TransDateTime)
                        .ToList();
                }

                var punchIn = candidateList1.OrderBy(e => e.TransDateTime).FirstOrDefault();
                LateEarlyLogTimeEventInfo punchOut = null;

                if (punchIn != null)
                {
                    // The LATEST event that is not at the very same instant as the punch-in, so a day with
                    // exactly one punch has a Sign In and no Sign Out.
                    punchOut = candidateList2
                        .Where(e => e.TransDateTime != punchIn.TransDateTime)
                        .OrderByDescending(e => e.TransDateTime)
                        .FirstOrDefault();
                }

                DateTime actualIn = default;
                DateTime actualOut = default;

                if (punchIn != null)
                {
                    actualIn = punchIn.TransDateTime;
                    row.SignIn = punchIn.TransTime.TimeOfDay;
                    row.PunchInAccessUnit = punchIn.AccessUnit;
                }

                if (punchOut != null)
                {
                    actualOut = punchOut.TransDateTime;
                    row.SignOut = punchOut.TransTime.TimeOfDay;
                    row.PunchOutAccessUnit = punchOut.AccessUnit;
                }

                // ---- Status.
                if (isWeekOff)
                {
                    row.Status = useArabicNames ? WeekOffAr : WeekOffEn;
                }
                else if (isPublicHoliday)
                {
                    row.Status = useArabicNames ? PublicHolidayAr : PublicHolidayEn;
                }
                else if (candidateList1.Count > 0 != candidateList2.Count > 0)
                {
                    // UNREACHABLE. Legacy compared TimeEventList1 against TimeEventList2, which are built
                    // from identical predicates and are therefore always in the same state. Kept in this
                    // shape so the correspondence with legacy is visible; pinned by a test.
                    row.Status = "*";
                }
                else
                {
                    row.Status = string.Empty;
                }

                // ---- Total hours: written even when zero, and never reduced by a break.
                row.TotalHours = actualIn != default && actualOut != default
                    ? actualOut.Subtract(actualIn)
                    : TimeSpan.Zero;

                rows.Add(row);
            }
        }

        // Legacy set the "Oid" cell to `table.Rows.Count + 1`, a 1-based ROW SEQUENCE number rather than a
        // database id. Reproduced.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Id = i + 1;
        }

        return rows;
    }
}

/// <summary>
/// The neutral row both geo variants project their own DTO from. It carries the UNION of the two column
/// sets -- which, since Custom036 is Custom033 minus two columns, is just Custom033's set.
/// </summary>
public class LateEarlyLogGeoVariantRow
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string AccessId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public DateTime Date { get; set; }

    public string WeekDay { get; set; }

    public TimeSpan ShiftStart { get; set; }

    public TimeSpan ShiftEnd { get; set; }

    public TimeSpan? SignIn { get; set; }

    public TimeSpan? SignOut { get; set; }

    public TimeSpan TotalHours { get; set; }

    public string Status { get; set; }

    /// <summary>Populated by both variants; exported and rendered by Custom033 only.</summary>
    public string PunchInAccessUnit { get; set; }

    /// <summary>Populated by both variants; exported and rendered by Custom033 only.</summary>
    public string PunchOutAccessUnit { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }
}
