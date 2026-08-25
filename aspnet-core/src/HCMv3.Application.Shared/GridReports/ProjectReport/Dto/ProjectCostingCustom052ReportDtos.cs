using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.ProjectReport.Dto;

/// <summary>
/// Filter input for the customer-specific Project Costing (Custom052) analysis grid report (legacy
/// <c>uscRptProjectCosting_Custom052.ascx</c>), a per-customer variant of
/// <see cref="GetProjectCostingReportInput"/>/<c>uscRptProjectCosting.ascx</c>: same Month/Year period
/// resolution, but filters by COST CENTER instead of Payroll Area (legacy <c>cmbPayrollArea</c> is rebound
/// to <c>CostCenter</c> on this screen despite the unchanged control name).
/// </summary>
public class GetProjectCostingCustom052ReportInput : GridReportInputBase
{
    public int Month { get; set; }

    public int Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c> (rebound to CostCenter on this variant), matched by Code.</summary>
    public string CostCenterCode { get; set; }

    protected override string DefaultSorting => "ProjectName ASC, EmployeeCode ASC";
}

/// <summary>
/// One (Project, Employee) row of the Custom052 costing pivot. Ports legacy <c>Table2()</c>
/// (uscRptProjectCosting_Custom052.ascx.cs:625-764), which diffs from the base
/// <c>uscRptProjectCosting.ascx.cs</c> in four ways:
/// <list type="bullet">
/// <item><description>Wage-type columns include BOTH Earning and Deduction billing types (base: Earning
/// only).</description></item>
/// <item><description>Two extra identity columns: <see cref="Designation"/> (current job title) and
/// <see cref="CostCenterName"/>.</description></item>
/// <item><description><see cref="TotalPay"/> replaces "NetPay": the wage-type sum EXCLUDING code
/// "9000".</description></item>
/// <item><description><see cref="WorkingDays"/> is added. MODEL ADAPTATION: legacy summed
/// <c>EmployeeProjectDetail.WorkDays</c> (a non-persistent legacy field -- HCMv3's
/// <see cref="ProjectManagement.EmployeeProjectDetail"/> deliberately does not persist it either, see its
/// class remarks) for ActualDays costing or <c>.Percentage</c> for Percentage costing. This port instead
/// sums the project register's own WageType-1000 ("Basic Pay") <c>Number</c> column -- the same
/// "working days on this project" figure the sibling <c>ProjectMonthwiseSummaryReport</c> already uses --
/// avoiding a dependency on data legacy itself never persisted.</description></item>
/// </list>
/// </summary>
public class ProjectCostingCustom052ReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string Designation { get; set; }

    public string CostCenterName { get; set; }

    public string ProjectCode { get; set; }

    public string ProjectName { get; set; }

    public List<ProjectWageTypeAmountDto> WageTypeAmounts { get; set; } = new();

    public double WorkingDays { get; set; }

    /// <summary>Legacy "TotalPay": sum of every <see cref="WageTypeAmounts"/> cell EXCEPT WageType code "9000".</summary>
    public double TotalPay { get; set; }

    public double ALCurrentMonthAccrual { get; set; }

    public double AnnualLeaveProvision { get; set; }

    public double IndemnityDays { get; set; }

    public double IndemnityProvision { get; set; }
}
