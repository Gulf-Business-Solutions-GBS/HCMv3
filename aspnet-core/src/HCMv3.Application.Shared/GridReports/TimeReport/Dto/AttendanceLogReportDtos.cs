using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Attendance Log analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptAttendanceLog.ascx</c>).
///
/// Legacy's filter panel is dtFrom / dtTo / cmbPayrollArea / txtEmpCode
/// (uscRptAttendanceLog.ascx.cs:56-71, 537-552).
///
/// INHERITED-BUT-UNUSED: <see cref="GridReportInputBase.EmployeeStatus"/>. This screen has NO
/// <c>cmbEmpStatus</c> at all -- <c>Table1()</c> never filters on <c>Employee.Status</c>, even though it
/// projects it into an (also unused) local. See <see cref="AttendanceLogReportRowDto.Status"/>.
/// </summary>
public class GetAttendanceLogReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, whose ValueField was <c>PayrollArea.Code</c>
    /// (uscRptAttendanceLog.ascx.cs:196). Null/blank means "all payroll areas".
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy grid key was "Code" (<c>GetKey()</c>, line 397) and rows arrived in TimeRegister order;
    /// (Code, Date) is the stable equivalent for server-side paging.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DayDate ASC";
}

/// <summary>
/// One row of the Attendance Log report -- one <c>TimeRegister</c> (one employee-day).
/// Mirrors legacy <c>Table1()</c>'s 20-column DataTable (uscRptAttendanceLog.ascx.cs:514-533):
/// OID, Code, Employee, Status, Country, WorkLocation, OrgLevel1..7, DutyPeriod, Date, WeekDay,
/// SignIn, SignOut, WorkingHours, OverTime.
/// </summary>
public class AttendanceLogReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column (line 602) and the grid key.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column (line 603), Ar/En by request culture (line 570).</summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Status" column (line 626) -- NOT the employee's employment status. Legacy wrote the
    /// day's LEAVE TYPE name, or the literal "Absent" when there is no leave and the day counts as an
    /// absence, or "" otherwise: <c>dr[Status] = rec.LeaveType == "" ? rec.Absent : rec.LeaveType;</c>.
    /// The employee's real <c>Status</c> is projected at line 593 and then never written anywhere -- a
    /// legacy dead column, not ported.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Legacy "Country" column (line 637): the country of the PayrollArea on the org assignment current
    /// AS OF TODAY (legacy <c>dsOrgAss</c>, lines 558-560, keyed on <c>DateTime.Now</c> -- not on the
    /// report's date range, and not on the row's own day).
    /// </summary>
    public string CountryName { get; set; }

    /// <summary>Legacy "WorkLocation" column (line 638), from the same as-of-today assignment.</summary>
    public string WorkLocationName { get; set; }

    /// <summary>Legacy "DutyPeriod" column (line 615), Ar/En by request culture.</summary>
    public string DutyPeriodName { get; set; }

    /// <summary>Legacy "Date" column (line 604) -- <c>TimeRegister.DayDate</c>.</summary>
    public DateTime DayDate { get; set; }

    /// <summary>
    /// Legacy "WeekDay" column (line 625): <c>DayDate.DayOfWeek.ToString()</c>, i.e. the INVARIANT
    /// English day name, never localized.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "SignIn" column (line 605): <c>ActualIn</c> as "HH:mm"
    /// (<c>DateTimeExtension.TimeFormat</c>), or null when there is no punch.
    /// </summary>
    public string SignIn { get; set; }

    /// <summary>Legacy "SignOut" column (line 606) -- <c>ActualOut</c>, same rules as SignIn.</summary>
    public string SignOut { get; set; }

    /// <summary>
    /// Legacy "WorkingHours" column (lines 629-636): <c>ActualWorkingMins</c> rendered "HH:mm:ss", but
    /// forced to "00:00:00" whenever EITHER punch is missing.
    /// </summary>
    public string WorkingHours { get; set; }

    /// <summary>
    /// Legacy "OverTime" column (lines 617-624): <c>OvertimeMinutes</c> rendered "HH:mm:ss", forced to
    /// "00:00:00" whenever either punch is missing.
    /// </summary>
    public string OverTime { get; set; }

    /// <summary>
    /// Raw <c>TimeRegister.ActualIn</c> behind <see cref="SignIn"/>. Legacy had no such column, but the
    /// four time columns above are FORMATTED STRINGS that SQL cannot produce, so the raw values travel
    /// with the row and the strings are stamped after materialization. Clients should render
    /// <see cref="SignIn"/>/<see cref="SignOut"/>/<see cref="WorkingHours"/>/<see cref="OverTime"/>.
    /// </summary>
    public DateTime ActualIn { get; set; }

    /// <summary>Raw <c>TimeRegister.ActualOut</c> behind <see cref="SignOut"/>.</summary>
    public DateTime ActualOut { get; set; }

    /// <summary>Raw <c>TimeRegister.ActualWorkingMins</c> behind <see cref="WorkingHours"/>.</summary>
    public double ActualWorkingMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.OvertimeMinutes</c> behind <see cref="OverTime"/>.</summary>
    public double OvertimeMinutes { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>
    /// Leaf org unit of the as-of-today assignment the OrgLevel columns are stamped from -- see
    /// <c>EmployeeOrgLevelNameResolver</c> for why the seven levels cannot be part of the SQL projection.
    /// </summary>
    public long? OrganizationUnitId { get; set; }
}
