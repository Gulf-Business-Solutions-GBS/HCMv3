using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Late/Early Log (Custom 033) analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom033.ascx</c>).
///
/// Same panel as the base control -- dtFrom/dtTo/cmbPayrollArea/txtEmpCode -- and, like every Late/Early
/// Log control, NO employee-status combo: the query hard-codes <c>Status == EmployeeStatus.Active</c>, so
/// the inherited <c>EmployeeStatus</c> is UNUSED and the Angular screen hides the field rather than
/// forking the shared filter panel.
/// </summary>
public class GetLateEarlyLogCustom033ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window -- whose PayrollArea has that code (lines 733-739).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable employee-by-employee then day-by-day. Employee code
    /// then date is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of the Late/Early Log (Custom 033) report -- one (employee, day) presence evaluation.
///
/// COLUMN SET (legacy lines 694-726, reading past the commented-out <c>table.Columns.Add</c> calls):
/// Oid, Code, Employee, AccessId, OrgLevel1..OrgLevel7, Date, WeekDay, Shift Start, Shift End, Sign In,
/// Sign Out, Total Hours, Status, PunchInAccessUnit, PunchOutAccessUnit, WorkLocation, Country.
///
/// This is by far the LEANEST of the family. Custom033 comments out Early In, Early Out, Late In, Late
/// Out, Planned Hours, Overtime Hours, Absence Hours, LeaveType, PartialLeaveHours, Time Policy,
/// PunchInLocation, PunchOutLocation and a never-populated <c>GeoSelfie</c> byte[] column, leaving a
/// report that answers only "when did this person arrive and leave, and for how long". None of those
/// properties exist on this DTO, because the legacy report cannot produce a value for any of them.
///
/// Its sibling <c>uscRptLateEarlyLog_Custom036</c> is the SAME report minus
/// <see cref="PunchInAccessUnit"/> and <see cref="PunchOutAccessUnit"/>; the two share an implementation
/// (see <c>LateEarlyLogGeoVariantReportAppServiceBase</c>).
///
/// Legacy emitted the punch/shift cells as PRE-FORMATTED STRINGS. Here they are <see cref="TimeSpan"/>
/// time-of-day values so that server-side sorting on them is correct.
/// </summary>
public class LateEarlyLogCustom033ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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
    /// Legacy "Sign In" -- the EARLIEST punch inside the day's flexi window, of either transaction type.
    /// See the app service remarks: Custom033 ignores the time policy's management status and the
    /// clock-in/clock-out windows entirely.
    /// </summary>
    public TimeSpan? SignIn { get; set; }

    /// <summary>
    /// Legacy "Sign Out" -- the LATEST punch inside the same window that is not the very same instant as
    /// <see cref="SignIn"/>. A day with exactly one punch therefore has a Sign In and no Sign Out.
    /// </summary>
    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Total Hours" -- Sign Out minus Sign In, with NO break deduction. Unlike every other
    /// variant, Custom033 writes this cell even when it is zero (line 1104), so it is non-nullable here.
    /// </summary>
    public TimeSpan TotalHours { get; set; }

    /// <summary>
    /// Legacy "Status" -- "Week Off" or "Public Holiday" (each with an Arabic twin), otherwise "". See the
    /// app service remarks: the "*" branch legacy wrote here is UNREACHABLE, and so is the "Leave" one.
    /// </summary>
    public string Status { get; set; }

    public string PunchInAccessUnit { get; set; }

    public string PunchOutAccessUnit { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }
}
