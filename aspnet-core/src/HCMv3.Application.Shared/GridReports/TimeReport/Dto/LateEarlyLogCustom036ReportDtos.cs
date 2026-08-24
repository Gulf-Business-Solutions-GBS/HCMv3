using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Late/Early Log (Custom 036) analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom036.ascx</c>).
///
/// Same panel as the base control -- dtFrom/dtTo/cmbPayrollArea/txtEmpCode -- and, like every Late/Early
/// Log control, NO employee-status combo: the query hard-codes <c>Status == EmployeeStatus.Active</c>, so
/// the inherited <c>EmployeeStatus</c> is UNUSED and the Angular screen hides the field rather than
/// forking the shared filter panel.
/// </summary>
public class GetLateEarlyLogCustom036ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window -- whose PayrollArea has that code.
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable employee-by-employee then day-by-day. Employee code
    /// then date is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of the Late/Early Log (Custom 036) report -- one (employee, day) presence evaluation.
///
/// COLUMN SET (legacy lines 688-718, reading past the commented-out <c>table.Columns.Add</c> calls):
/// Oid, Code, Employee, AccessId, OrgLevel1..OrgLevel7, Date, WeekDay, Shift Start, Shift End, Sign In,
/// Sign Out, Total Hours, Status, WorkLocation, Country.
///
/// Custom036 IS Custom033 with two columns removed. A line-by-line diff of the two live
/// <c>Table1()</c> bodies shows their ONLY behavioural difference is that Custom036 drops
/// <c>PunchInAccessUnit</c> and <c>PunchOutAccessUnit</c> -- the column declarations, the two
/// <c>dr[...] = ""</c> blanking statements and the two <c>te_Punch*.AccessUnit</c> assignments -- and is
/// otherwise identical, whitespace aside. The two therefore share one implementation
/// (<c>LateEarlyLogGeoVariantReportAppServiceBase</c>) and differ only in their column list.
///
/// Everything said about Custom033's leanness applies here too: Early/Late In/Out, Planned Hours,
/// Overtime Hours, Absence Hours, LeaveType, PartialLeaveHours, Time Policy, PunchIn/OutLocation and a
/// never-populated <c>GeoSelfie</c> byte[] column are all commented out in the legacy control, so none of
/// them exists on this DTO.
/// </summary>
public class LateEarlyLogCustom036ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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
    /// See the app service remarks: Custom036 ignores the time policy's management status and the
    /// clock-in/clock-out windows entirely.
    /// </summary>
    public TimeSpan? SignIn { get; set; }

    /// <summary>
    /// Legacy "Sign Out" -- the LATEST punch inside the same window that is not the very same instant as
    /// <see cref="SignIn"/>. A day with exactly one punch therefore has a Sign In and no Sign Out.
    /// </summary>
    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Total Hours" -- Sign Out minus Sign In, with NO break deduction. Written even when zero
    /// (line 1028), so non-nullable here.
    /// </summary>
    public TimeSpan TotalHours { get; set; }

    /// <summary>
    /// Legacy "Status" -- "Week Off" or "Public Holiday" (each with an Arabic twin), otherwise "". See the
    /// app service remarks: the "*" branch legacy wrote here is UNREACHABLE, and so is the "Leave" one.
    /// </summary>
    public string Status { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }
}
