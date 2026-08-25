using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.ProjectReport.Dto;

/// <summary>
/// Filter input for the Project Costing analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectCosting.ascx</c>).
///
/// Legacy filters by MONTH/YEAR (resolved to the last day of that month, "EndDate"), not a date range, so
/// <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/> are unused -- same
/// convention as <see cref="ProjectWiseAccrual.GetProjectWiseAccrualReportInput"/> (sibling report, same
/// legacy folder).
/// </summary>
public class GetProjectCostingReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c> (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>.</summary>
    public int Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, matched by Code.</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "ProjectName ASC, EmployeeCode ASC";
}

/// <summary>
/// One (Project, Employee) row of the costing pivot. Ports legacy <c>Table2()</c>
/// (uscRptProjectCosting.ascx.cs:603-718) -- the employee-level drill target; legacy's project-only
/// rollup (<c>Table1()</c>) is the same shape with the employee columns blank and is not duplicated here,
/// same simplification as the sibling <c>ProjectWiseAccrualReport</c>.
///
/// ATYPICAL FOR THIS FAMILY: the wage-type columns are a PIVOT (legacy built one DataTable column per
/// Earning-billing-type wage type found in the period, lines 506-524) -- see
/// <see cref="ProjectWageTypeAmountDto"/> and <c>EmployeeSalaryChangeReportRowDto</c> for the shared
/// pattern.
/// </summary>
public class ProjectCostingReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string ProjectCode { get; set; }

    public string ProjectName { get; set; }

    /// <summary>The dynamic wage-type pivot block. Same wage types, same order, on every row.</summary>
    public List<ProjectWageTypeAmountDto> WageTypeAmounts { get; set; } = new();

    /// <summary>Legacy "NetPay": the sum of every <see cref="WageTypeAmounts"/> cell.</summary>
    public double NetPay { get; set; }

    /// <summary>Legacy "ALCurrentMonthAccrual": sum of WageType code 4010 (Number, not Amount).</summary>
    public double ALCurrentMonthAccrual { get; set; }

    /// <summary>Legacy "AnnualLeaveProvision": sum of WageType code 3001.</summary>
    public double AnnualLeaveProvision { get; set; }

    /// <summary>Legacy "IndemnityDays": sum of WageType code 4004 (Number, not Amount).</summary>
    public double IndemnityDays { get; set; }

    /// <summary>Legacy "IndemnityProvision": sum of WageType code 3000.</summary>
    public double IndemnityProvision { get; set; }
}

/// <summary>One wage-type cell of the <see cref="ProjectCostingReportRowDto.WageTypeAmounts"/> pivot.</summary>
public class ProjectWageTypeAmountDto
{
    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public double Amount { get; set; }
}
