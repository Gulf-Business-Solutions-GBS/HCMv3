using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Custom053 Late/Early Log grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom053.ascx</c>).
///
/// Same panel as the base: dtFrom/dtTo/cmbPayrollArea/txtEmpCode, employees hard-filtered to
/// <c>EmployeeStatus.Active</c> (line 761), so the inherited <c>EmployeeStatus</c> is unused.
/// </summary>
public class GetLateEarlyLogCustom053ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>), matched against an org assignment
    /// covering TODAY (lines 763-770).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of legacy <c>uscRptLateEarlyLog_Custom053.ascx.cs</c> <c>Table1()</c> (lines 624-1407).
///
/// THE MOST DIVERGENT OF THE THREE CUSTOMS. Against the base row it DROPS four columns -- <c>Break Out</c>,
/// <c>Break In</c> and <c>Break Hours</c> (commented out, lines 735-737) and <c>Country</c> (line 756) --
/// adds none, and CHANGES THE TYPE OF ONE: <c>Overtime Hours</c> is declared <c>typeof(double)</c>
/// (line 746) and written as <c>Math.Round(overTimeHours.TotalHours, 3)</c> (line 1381), i.e. decimal
/// hours rather than a duration. See <see cref="OvertimeHours"/>.
///
/// The behavioural divergences (work-location-driven duty, same-day-only punches, week-off compensate
/// overtime) are documented on the app service.
/// </summary>
public class LateEarlyLogCustom053ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

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

    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "Shift Start" -- from the WORK LOCATION's duty, not the employee's, whenever the day's last
    /// clock-out punch carries a work location that has a duty period covering the day (lines 1024-1044).
    /// Still carries the base's sticky-local defect when no duty resolves at all.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". Work-location duty, as <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    public TimeSpan? SignIn { get; set; }

    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Early In" -- time-of-day comparison against the WORK LOCATION shift start, suppressed when
    /// the work-location duty is a week off (line 1198).
    /// </summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out". As <see cref="EarlyIn"/>.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In". As <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Legacy "Late Out". As <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateOut { get; set; }

    /// <summary>Legacy "Total Hours" -- punch-out minus punch-in, no break deduction.</summary>
    public TimeSpan? TotalHours { get; set; }

    /// <summary>
    /// Legacy "Planned Hours" -- computed against the EMPLOYEE's week-off flag but with the WORK
    /// LOCATION's night-shift flag (line 1306, a copy-paste of the line above it). Reproduced.
    /// </summary>
    public TimeSpan? PlannedHours { get; set; }

    /// <summary>
    /// Legacy "Overtime Hours" -- DECIMAL HOURS, not a duration: the column is <c>typeof(double)</c> and
    /// legacy writes <c>Math.Round(overTimeHours.TotalHours, 3)</c> (line 1381). Modelled as a nullable
    /// <c>double</c> so the grid and the export show what legacy showed; every other duration on this row
    /// stays a <c>TimeSpan</c>.
    /// </summary>
    public double? OvertimeHours { get; set; }

    /// <summary>
    /// Legacy "Absence Hours". As in Custom045 the full-day-leave branch no longer charges the planned
    /// shift as absence and writes the cell only for an UNPAID absence type (lines 1104-1110).
    /// </summary>
    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// Legacy "Status" -- the base's vocabulary, but the week-off verdict uses the EMPLOYEE's duty flag
    /// while the early/late arithmetic above uses the WORK LOCATION's (line 1252 vs line 1198).
    /// </summary>
    public string Status { get; set; }

    public string LeaveType { get; set; }

    public TimeSpan? PartialLeaveHours { get; set; }

    public string PunchInAccessUnit { get; set; }

    public string PunchOutAccessUnit { get; set; }

    public string TimePolicyName { get; set; }

    public string WorkLocationName { get; set; }

    /// <summary>Legacy "PunchInLocation" -- see the app service: resolved across all punches sharing the selected punch's timestamp.</summary>
    public string PunchInLocation { get; set; }

    /// <summary>Legacy "PunchOutLocation". As <see cref="PunchInLocation"/>.</summary>
    public string PunchOutLocation { get; set; }
}
