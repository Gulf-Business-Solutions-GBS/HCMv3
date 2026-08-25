using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Leave Provision report -- port of legacy <c>uscRptLeaveProvision.ascx(.cs)</c>,
/// <c>Table1()</c> (the ACTIVE-employee path; the dead <c>Table1_Opt</c>/<c>Table1_Old</c> and the separate
/// <c>Table_InActive</c> summary path are NOT ported -- same simplification as
/// <see cref="GetIndemnityProvisionReportInput"/>). <see cref="GridReportInputBase.FromDate"/>/
/// <see cref="GridReportInputBase.ToDate"/> replace the legacy month/year pickers, resolved to month-end
/// boundaries; every regular-run month end in between is one output row per employee.
/// </summary>
public class GetLeaveProvisionReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPayrollArea</c> selected Code. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc, LeaveDaysCalculationPeriod asc";
}

public class LeaveProvisionReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    public string LeaveDaysCalculationPeriod { get; set; }

    public string PayrollAreaName { get; set; }

    public string CostCenterName { get; set; }

    public string JobTitleName { get; set; }

    public double BasicSalary { get; set; }

    public double Allowance { get; set; }

    public double TotalSalary { get; set; }

    public string CurrencyName { get; set; }

    public double SalaryInProvision { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public double SystemOpeningBalanceDays { get; set; }

    public double OpeningBalanceDays { get; set; }

    public double OpeningBalanceAmount { get; set; }

    public double CurrentMonthAccrual { get; set; }

    public double CurrentMonthAccrualAmount { get; set; }

    public double CurrentMonthPaidLeave { get; set; }

    public double CurrentMonthPaidLeaveAmount { get; set; }

    public double CurrentMonthPaidLeaveArrear { get; set; }

    public double CurrentMonthPaidLeaveArrearAmount { get; set; }

    public double CurrentMonthAdjustment { get; set; }

    public double CurrentMonthAdjustmentAmount { get; set; }

    /// <summary>Only meaningful (legacy only showed the column) when <c>ForfeitRule == Monthly</c>.</summary>
    public double CurrentMonthForfeit { get; set; }

    public double LeaveUsage { get; set; }

    public double CurrentMonthUnpaidLeave { get; set; }

    public double TotalUnpaidLeave { get; set; }

    public double AbsentDays { get; set; }

    public double ClosingBalance { get; set; }

    public double ClosingBalanceAmount { get; set; }
}
