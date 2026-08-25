using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Leave Salary analysis grid report (legacy
/// <c>uscRptEmployeeLeaveSalary.ascx</c>). Adds the report-specific <c>cmbLeaveType</c> combo
/// (<see cref="LeaveTypeCode"/>). Legacy has no employee-status filter on this screen.
/// </summary>
public class GetEmployeeLeaveSalaryReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbLeaveType</c>: AbsenceType.Code. Null/empty means "all leave types".</summary>
    public string LeaveTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c> (uscRptEmployeeLeaveSalary.ascx.cs:479-734).
///
/// FIDELITY NOTE: legacy computes leave/salary split figures with a per-row, per-month loop against
/// <c>TimeRegister</c> and the employee's basic-pay rate; this port reproduces the same per-row
/// computation (see <c>EmployeeLeaveSalaryReportAppService.BuildRowsAsync</c>) rather than trying to force
/// it into one SQL projection.
/// </summary>
public class EmployeeLeaveSalaryReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string AbsenceTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string StartTime { get; set; }

    public string EndTime { get; set; }

    public double AbsenceHours { get; set; }

    public double LeaveDaysInMonthStart { get; set; }

    public double LeaveDaysInMonthEnd { get; set; }

    public double CalendarDays { get; set; }

    public double TotalLeaveDays { get; set; }

    public double QuotaBalance { get; set; }

    public double EmployeeSalaryInMonthStart { get; set; }

    public double EmployeeSalaryInMiddleMonth { get; set; }

    public double EmployeeSalaryInMonthEnd { get; set; }

    public double LeaveSalaryInMonthStart { get; set; }

    public double LeaveSalaryInMiddleMonth { get; set; }

    public double LeaveSalaryInMonthEnd { get; set; }

    public double TotalEmployeeSalary { get; set; }

    public double TotalLeaveSalary { get; set; }
}
