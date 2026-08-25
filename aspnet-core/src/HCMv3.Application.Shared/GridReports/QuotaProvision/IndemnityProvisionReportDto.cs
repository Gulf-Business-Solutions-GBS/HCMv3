using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Indemnity Provision report -- port of legacy <c>uscRptIndemnityProvision.ascx(.cs)</c>,
/// <c>Table1()</c> (the ACTIVE-employee path). <see cref="GridReportInputBase.FromDate"/>/
/// <see cref="GridReportInputBase.ToDate"/> replace the legacy <c>txtMonth</c>/<c>txtYear</c>/
/// <c>txtMonthTo</c>/<c>txtYearTo</c> month pickers: both are resolved to the LAST day of their month and
/// every regular-run month end in between is one output row per employee (legacy's <c>monthYearList</c>
/// loop, lines 979-996). When only <see cref="GridReportInputBase.FromDate"/> is supplied, ToDate defaults
/// to the same month (single-month report, matching the legacy <c>ShowPeriodInIndemnityProvision == false</c>
/// screen configuration).
///
/// NOT PORTED: legacy's separate <c>Table_InActive()</c> path (a per-employee "latest own register" summary
/// for <c>EmpStatus == "INACTIVE"</c>, a materially different row shape from the monthly grid) and the
/// <c>ManageReportSettings.PeriodMonthLimitInIndemnityProvision</c> hard validation -- this report simply
/// returns whatever period range is requested.
/// </summary>
public class GetIndemnityProvisionReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPayrollArea</c> selected Code. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc, CalculationPeriod asc";
}

public class IndemnityProvisionReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string PayrollAreaName { get; set; }

    public string CostCenterName { get; set; }

    public string JobTitleName { get; set; }

    public double BasicSalary { get; set; }

    public double Allowance { get; set; }

    public double TotalSalary { get; set; }

    public string CurrencyName { get; set; }

    public double SalaryInProvision { get; set; }

    public double IndemnityPerYear { get; set; }

    public string CalculationPeriod { get; set; }

    public double OpeningBalance { get; set; }

    public double OpeningBalanceAmount { get; set; }

    public double AbsentUnpaidOpeningBalance { get; set; }

    public double CurrentMonthAccrual { get; set; }

    public double CurrentAccrualAmount { get; set; }

    public double CurrentMonthUnpaidLeave { get; set; }

    public double TotalUnpaidLeave { get; set; }

    public double AbsentDays { get; set; }

    public double TotalAbsentDays { get; set; }

    public double ClosingBalance { get; set; }

    public double ClosingBalanceAmount { get; set; }

    public double PaidAmount { get; set; }

    public double BalanceAmount { get; set; }

    public string FnfStatus { get; set; }
}
