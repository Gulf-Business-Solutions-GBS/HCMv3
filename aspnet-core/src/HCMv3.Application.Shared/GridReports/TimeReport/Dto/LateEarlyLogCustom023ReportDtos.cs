using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Late/Early Log (Custom 023) analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom023.ascx</c>).
///
/// Same panel as the base control -- dtFrom/dtTo/cmbPayrollArea/txtEmpCode -- and, like every Late/Early
/// Log control, NO employee-status combo: the query hard-codes <c>Status == EmployeeStatus.Active</c>, so
/// the inherited <c>EmployeeStatus</c> is UNUSED and the Angular screen hides the field rather than
/// forking the shared filter panel.
/// </summary>
public class GetLateEarlyLogCustom023ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window -- whose PayrollArea has that code.
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable employee-by-employee then day-by-day. Employee code
    /// then date is used here so server-side paging is deterministic and reads in the same order the
    /// legacy grid did.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of the Late/Early Log (Custom 023) report -- one (employee, day) attendance evaluation.
///
/// COLUMN SET (legacy lines 793-827): Oid, Code, Employee, AccessId, OrgLevel1..OrgLevel7, Date, WeekDay,
/// Shift Start, Shift End, Sign In, Sign Out, Early In, Early Out, Late In, Late Out, Total Hours,
/// Planned Hours, Overtime Hours, Absence Hours, Status, LeaveType, PartialLeaveHours,
/// PunchInAccessUnit, PunchOutAccessUnit, Time Policy, WorkLocation, Country, PunchInLocation,
/// PunchOutLocation.
///
/// Relative to the base control this variant drops exactly THREE columns -- Break Out, Break In and Break
/// Hours -- and the break machinery with them: Custom023 does not select break punches at all, and its
/// Planned Hours are consequently never reduced by a break-shift duration.
///
/// The Oid column is a 1-based ROW SEQUENCE number in the base control, but Custom023 NEVER ASSIGNS IT
/// (there is no <c>dr[Oid] =</c> anywhere in its <c>Table1()</c>), so every legacy row showed a blank Oid.
/// <see cref="EntityDto{TPrimaryKey}.Id"/> is still stamped here because the Angular grid needs a stable
/// row key; the column is simply not exported.
///
/// Legacy emitted the punch/shift cells as PRE-FORMATTED STRINGS and the duration cells as
/// <c>TimeSpan</c>. Here punches and shift bounds are <see cref="TimeSpan"/> time-of-day values and the
/// durations are nullable (legacy left an unset duration cell as <c>DBNull</c>, which is null here, NOT
/// <c>TimeSpan.Zero</c>).
/// </summary>
public class LateEarlyLogCustom023ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>FullNameAr when Arabic, else FullNameEn.</summary>
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

    /// <summary>The day name, localized through legacy's hard-coded literal table.</summary>
    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "Shift Start". See the app service remarks on the STICKY SHIFT DEFECT: on a day with no
    /// resolvable Duty legacy re-used the previously resolved shift bounds, and so does this port.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    /// <summary>
    /// Legacy "Sign In". UNLIKE the base control, Custom023 suppresses this cell entirely on a week off
    /// (its guard is <c>te_PunchIn != null &amp;&amp; isWeekOff == false</c>, line 1063), so a punch made
    /// on a week off is not reported at all.
    /// </summary>
    public TimeSpan? SignIn { get; set; }

    /// <summary>Legacy "Sign Out". Suppressed on a week off -- see <see cref="SignIn"/>.</summary>
    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Early In". Custom023 computes this from the TIME-OF-DAY difference between the shift start
    /// and the punch (line 1068), not from the two full timestamps as the base control does -- so on a
    /// night shift the value does not wrap across midnight. See the app service remarks.
    /// </summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out". Time-of-day difference -- see <see cref="EarlyIn"/>.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In". Time-of-day difference -- see <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Legacy "Late Out". Time-of-day difference -- see <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateOut { get; set; }

    public TimeSpan? TotalHours { get; set; }

    /// <summary>
    /// Legacy "Planned Hours". Custom023 derives this from the RAW shift times with a night-shift
    /// inversion (<c>!isNightShift ? end - start : start - end</c>, line 1123) rather than from the
    /// resolved shift timestamps. See the app service remarks.
    /// </summary>
    public TimeSpan? PlannedHours { get; set; }

    public TimeSpan? OvertimeHours { get; set; }

    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// One of the hard-coded literals "Leave"/"No Return Date"/"Week Off"/"Rest Day"/"Public Holiday"
    /// (each with an Arabic twin), or "*", or "Absent", or "" -- Custom023 has a richer status ladder than
    /// the base control, which knows neither "Rest Day" nor "Absent" nor "*". See the app service remarks.
    /// </summary>
    public string Status { get; set; }

    public string LeaveType { get; set; }

    /// <summary>Set only for an absence with non-zero AbsenceHours.</summary>
    public TimeSpan? PartialLeaveHours { get; set; }

    public string PunchInAccessUnit { get; set; }

    public string PunchOutAccessUnit { get; set; }

    public string TimePolicyName { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }

    public string PunchInLocation { get; set; }

    public string PunchOutLocation { get; set; }
}
