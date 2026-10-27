using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Late/Early Log analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog.ascx</c>).
///
/// The legacy panel is dtFrom/dtTo/cmbPayrollArea/txtEmpCode. There is NO employee-status combo: the query
/// hard-codes <c>Status == EmployeeStatus.Active</c> (uscRptLateEarlyLog.ascx.cs:766). The inherited
/// <c>EmployeeStatus</c> is therefore UNUSED and the Angular screen hides the field rather than forking the
/// shared filter panel.
///
/// SEVEN CUSTOMER VARIANTS OUT OF SCOPE: <c>uscRptLateEarlyLog_Custom019/023/033/036/040/045/053</c> and
/// <c>uscRptLateEarlyLog_Old</c> are separate legacy controls that are deliberately NOT part of this port.
/// </summary>
public class GetLateEarlyLogReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window -- whose PayrollArea has that code (lines 768-775).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable employee-by-employee then day-by-day (lines
    /// 918-924). Employee code then date is used here so server-side paging is deterministic and reads in
    /// the same order the legacy grid did.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row -- one (employee, day) attendance evaluation. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptLateEarlyLog.ascx.cs:629-1550</c> column-for-column: Oid, Code, Employee, AccessId,
/// OrgLevel1..OrgLevel7, Date, WeekDay, Shift Start, Shift End, Sign In, Sign Out, Break Out, Break In,
/// Break Hours, Early In, Early Out, Late In, Late Out, Total Hours, Planned Hours, Overtime Hours,
/// Absence Hours, Status, LeaveType, PartialLeaveHours, PunchInAccessUnit, PunchOutAccessUnit, Time Policy,
/// WorkLocation, Country, PunchInLocation, PunchOutLocation.
///
/// Legacy emitted the punch/shift cells as PRE-FORMATTED STRINGS in the app time format and the duration
/// cells as <c>TimeSpan</c>. Here the punches and shift bounds are <see cref="TimeSpan"/> time-of-day
/// values and the durations are nullable <see cref="TimeSpan"/> (legacy left an unset duration cell as
/// <c>DBNull</c>, which is null here, NOT <c>TimeSpan.Zero</c>) -- formatting happens in the Angular grid,
/// which is what makes server-side sorting on them correct.
/// </summary>
public class LateEarlyLogReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column -- <c>emp.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- FullNameAr when Arabic, else FullNameEn (line 787).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "AccessId" column -- <c>Employee.AccessID</c>.</summary>
    public string AccessId { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public DateTime Date { get; set; }

    /// <summary>
    /// Legacy "WeekDay" column -- the day name, localized to Arabic in the Arabic aspect through a
    /// hard-coded literal table (lines 691-698), not through a resource file. Reproduced.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "Shift Start" column. See the app service remarks on the STICKY SHIFT DEFECT: on a day with
    /// no resolvable Duty, legacy re-used the previously resolved shift bounds, and so does this port.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End" column. See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    /// <summary>Legacy "Sign In" column -- the selected punch-in event's TransTime, null when none.</summary>
    public TimeSpan? SignIn { get; set; }

    /// <summary>Legacy "Sign Out" column -- the selected punch-out event's TransTime, null when none.</summary>
    public TimeSpan? SignOut { get; set; }

    /// <summary>Legacy "Break Out" column.</summary>
    public TimeSpan? BreakOut { get; set; }

    /// <summary>Legacy "Break In" column.</summary>
    public TimeSpan? BreakIn { get; set; }

    /// <summary>Legacy "Break Hours" column -- BreakIn minus BreakOut, only when both punches exist.</summary>
    public TimeSpan? BreakHours { get; set; }

    /// <summary>Legacy "Early In" -- shift start minus punch-in, only when the day is not a week off.</summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out" -- shift end minus punch-out, only when the day is not a week off.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In" -- punch-in minus shift start, only when the day is not a week off.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Legacy "Late Out" -- punch-out minus shift end, only when the day is not a week off.</summary>
    public TimeSpan? LateOut { get; set; }

    /// <summary>Legacy "Total Hours" column.</summary>
    public TimeSpan? TotalHours { get; set; }

    /// <summary>Legacy "Planned Hours" column.</summary>
    public TimeSpan? PlannedHours { get; set; }

    /// <summary>Legacy "Overtime Hours" column.</summary>
    public TimeSpan? OvertimeHours { get; set; }

    /// <summary>Legacy "Absence Hours" column.</summary>
    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// Legacy "Status" column -- one of the hard-coded literals "Leave"/"No Return Date"/"Week Off"/
    /// "Public Holiday" (each with an Arabic twin), or "Missed In"/"Missed Out"/"Missed In Missed Out"
    /// (English only in legacy, lines 1427-1436 -- reproduced), or "" for a clean day.
    /// </summary>
    public string Status { get; set; }

    /// <summary>Legacy "LeaveType" column -- the absence type name for a full-day or partial leave.</summary>
    public string LeaveType { get; set; }

    /// <summary>Legacy "PartialLeaveHours" column -- set only for an absence with non-zero AbsenceHours.</summary>
    public TimeSpan? PartialLeaveHours { get; set; }

    public string PunchInAccessUnit { get; set; }

    public string PunchOutAccessUnit { get; set; }

    /// <summary>Legacy "Time Policy" column.</summary>
    public string TimePolicyName { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }

    /// <summary>Legacy "PunchInLocation" column -- the punch-in event's WorkLocation.</summary>
    public string PunchInLocation { get; set; }

    /// <summary>Legacy "PunchOutLocation" column -- the punch-out event's WorkLocation.</summary>
    public string PunchOutLocation { get; set; }
}
