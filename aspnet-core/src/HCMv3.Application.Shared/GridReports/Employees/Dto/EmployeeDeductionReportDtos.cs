using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Deduction analysis grid report (legacy
/// <c>uscRptEmployeeDeduction.ascx</c>).
///
/// ATYPICAL FILTER PANEL: alone among this batch, this report does NOT use the family's standard
/// From/To/EmployeeCode/EmployeeStatus filters. Legacy's panel (uscRptEmployeeDeduction.ascx:15-34) is
/// Month, Year, Run Type and Payroll Area, and <c>GetParameterStr</c> (lines 534-548) logs exactly those
/// four. The inherited base fields are therefore unused and the Angular screen hides all of them.
/// </summary>
public class GetEmployeeDeductionReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>txtMonth</c>. Null/blank means the CURRENT month (legacy lines 484-486 defaulted to
    /// <c>DateTime.Now.Month</c>).
    /// </summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>
    /// Legacy <c>txtYear</c>. Null/blank means the CURRENT year (legacy lines 486-488 defaulted to
    /// <c>DateTime.Now.Year</c>).
    /// </summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types".</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, which held the payroll area CODE and looked its Oid up before
    /// filtering (lines 651-659). This port takes the id directly -- same filter, one query fewer.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy grid was unsorted; employee code is the deterministic default for server paging.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptEmployeeDeduction.ascx.cs:552-739).
///
/// Legacy column set: OID, Code, Employee, JobTitle, OrgLevel1..OrgLevel7, Payroll Area,
/// Late &amp; Early Deduction, Late &amp; Early Minutes, Unpaid Leave Deduction, Unpaid Leave Days,
/// Loan Recovery Deduction, Loan Recovery Days, Late Early Deduction After Monthly Tolerance,
/// Late Early After Monthly Tolerance Days.
///
/// Each amount/number pair is the SUM over one wage type of the employee's salary register detail rows
/// for the requested period: 2000 = late/early, 2007 = unpaid leave, 2001 = loan recovery,
/// 2010 = late/early after the monthly tolerance (legacy lines 683-690).
/// </summary>
public class EmployeeDeductionReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- legacy used FullNameEn (line 678).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "JobTitle" column, taken from the org assignment current as of TODAY (line 662).</summary>
    public string JobTitle { get; set; }

    /// <summary>Legacy "Payroll Area" column, from the same current org assignment.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy "Late &amp; Early Deduction" -- SUM(Amount) over wage type 2000.</summary>
    public double LateEarlyDeduction { get; set; }

    /// <summary>Legacy "Late &amp; Early Minutes" -- SUM(Number) over wage type 2000.</summary>
    public double LateEarlyMinutes { get; set; }

    /// <summary>Legacy "Unpaid Leave Deduction" -- SUM(Amount) over wage type 2007.</summary>
    public double UnpaidLeaveDeduction { get; set; }

    /// <summary>Legacy "Unpaid Leave Days" -- SUM(Number) over wage type 2007.</summary>
    public double UnpaidLeaveDays { get; set; }

    /// <summary>Legacy "Loan Recovery Deduction" -- SUM(Amount) over wage type 2001.</summary>
    public double LoanRecoveryDeduction { get; set; }

    /// <summary>Legacy "Loan Recovery Days" -- SUM(Number) over wage type 2001.</summary>
    public double LoanRecoveryDays { get; set; }

    /// <summary>Legacy "Late Early Deduction After Monthly Tolerance" -- SUM(Amount) over wage type 2010.</summary>
    public double LateEarlyAfterMonthlyTolerance { get; set; }

    /// <summary>Legacy "Late Early After Monthly Tolerance Days" -- SUM(Number) over wage type 2010.</summary>
    public double LateEarlyAfterMonthlyToleranceDays { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }
}
