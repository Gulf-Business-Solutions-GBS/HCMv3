using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Loan analysis grid report (legacy <c>uscRptEmployeeLoan.ascx</c>).
/// Adds the report-specific <c>cmbLoanType</c> combo. Legacy has no employee-status filter on this screen.
/// </summary>
public class GetEmployeeLoanReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbLoanType</c>: LoanType.Code. Null/empty means "all loan types".</summary>
    public string LoanTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c> (uscRptEmployeeLoan.ascx.cs:499-683). Drill-down
/// <c>Table2()</c> (loan recovery detail on double-click) is NOT PORTED -- DevExpress drill-down UI
/// plumbing, consistent with the rest of this wave.
/// </summary>
public class EmployeeLoanReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }
    public long? OrganizationUnitId { get; set; }

    public string LoanTypeName { get; set; }

    /// <summary>Legacy "LoanPaymentDate" -- <c>EmployeeLoan.StartDate</c>.</summary>
    public DateTime LoanPaymentDate { get; set; }

    public DateTime RecoveryStartDate { get; set; }

    /// <summary>Legacy "RecoveryEndDate" -- <c>EmployeeLoan.EndDate</c>.</summary>
    public DateTime RecoveryEndDate { get; set; }

    public double FixedAmount { get; set; }

    public double DueAmount { get; set; }

    /// <summary>Legacy "RecoveryAmount" = DueAmount - BalanceDueAmount.</summary>
    public double RecoveryAmount { get; set; }

    public double BalanceDueAmount { get; set; }

    public string Comments { get; set; }

    public string CountryName { get; set; }

    /// <summary>
    /// FIDELITY NOTE: legacy's fill loop assigns this column from the employee's latest
    /// <c>EmployeeSeparation.SubmissionDate</c>, NOT <c>LastDayInService</c> as the column name implies
    /// (uscRptEmployeeLoan.ascx.cs:671). Reproduced exactly.
    /// </summary>
    public DateTime? LastDayInService { get; set; }
}
