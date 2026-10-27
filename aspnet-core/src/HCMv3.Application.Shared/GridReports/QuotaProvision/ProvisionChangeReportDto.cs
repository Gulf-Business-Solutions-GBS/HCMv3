using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Provision Change report -- port of legacy <c>uscRptProvisionChange.ascx(.cs)</c>,
/// <c>Table1()</c>. Legacy filters by month/year (not a free date range), matching
/// <see cref="GetEndOfServiceReportInput"/>'s shape.
/// </summary>
public class GetProvisionChangeReportInput : GridReportInputBase
{
    [Range(1, 12)]
    public int? Month { get; set; }

    [Range(1900, 3000)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c> selected Code. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy <c>cmbEmpStatus</c>: "ALL" (Active + Inactive-with-FnF), "ACTIVE" or "INACTIVE"
    /// (Inactive-with-FnF only). Null/blank defaults to "ALL", same as legacy's first combo item.
    /// </summary>
    public string EmployeeStatusFilter { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class ProvisionChangeReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string Status { get; set; }

    public DateTime JoiningDate { get; set; }

    public string PayrollAreaName { get; set; }

    public string CostCenterName { get; set; }

    public string JobTitleName { get; set; }

    public double BasicSalary { get; set; }

    public double Allowance { get; set; }

    public double TotalSalary { get; set; }

    public string CurrencyName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public double TotalAlDifferenceAmount { get; set; }

    public double TotalIndemnityDifferenceAmount { get; set; }
}
