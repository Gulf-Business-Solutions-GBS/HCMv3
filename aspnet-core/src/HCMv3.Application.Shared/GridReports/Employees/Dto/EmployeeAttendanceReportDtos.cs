using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Attendance analysis grid report (legacy
/// <c>uscRptEmployeeAttendance.ascx</c>). The legacy filter panel is exactly dtFrom/dtTo/cmbEmpStatus/
/// txtEmpCode, so no report-specific fields are added.
/// </summary>
public class GetEmployeeAttendanceReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no sort in <c>Table1()</c> (uscRptEmployeeAttendance.ascx.cs:473-594). Employee
    /// code ascending is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeAttendance.ascx.cs:473-594</c> column-for-column:
/// OID, Code, Employee, StartDate, EndDate, StartTime, EndTime, AttendanceType, AttendanceHours,
/// TotalAttendDays, Comments.
///
/// DEVIATION (legacy defect, deliberately NOT reproduced): legacy line 544 sets the EndDate cell from
/// <c>rec.StartDate</c> (<c>EndDate = rec.StartDate.ToString("dd-MM-yyyy")</c>), so the legacy grid's
/// "EndDate" column always displayed the START date. <see cref="EndDate"/> here is the real
/// <c>EmployeeAttendance.EndDate</c>.
///
/// Legacy emitted the date/time cells as PRE-FORMATTED STRINGS ("dd-MM-yyyy" and the app time format)
/// into string columns; they are real DateTime values here and formatted in the Angular grid instead,
/// which is what makes server-side sorting on them correct.
/// </summary>
public class EmployeeAttendanceReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>See the class remarks: the real end date, not legacy's copy of StartDate.</summary>
    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    /// <summary>Legacy "AttendanceType" column -- empty string (not null) when there is no type.</summary>
    public string AttendanceTypeName { get; set; }

    public double AttendanceHours { get; set; }

    public double TotalAttendDays { get; set; }

    public string Comments { get; set; }
}
