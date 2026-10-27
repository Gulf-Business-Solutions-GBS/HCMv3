using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.ProjectReport.Dto;

/// <summary>
/// Filter input for the Project Monthwise Summary analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectMonthwiseSummary.ascx</c>).
/// Same Month/Year period resolution as the sibling <see cref="GetProjectCostingReportInput"/>.
/// </summary>
public class GetProjectMonthwiseSummaryReportInput : GridReportInputBase
{
    public int Month { get; set; }

    public int Year { get; set; }

    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One employee row of the monthwise summary pivot. Ports legacy <c>Table1()</c>
/// (uscRptProjectMonthwiseSummary.ascx.cs:490-701).
///
/// ATYPICAL FOR THIS FAMILY, AND A DELIBERATE REORGANIZATION of legacy's layout: legacy built THREE
/// separate dynamic column BLOCKS, one per metric, each with one column per project ("{ProjectCode} WD",
/// then every "{ProjectCode} OT", then every "{ProjectCode} AMT", lines 580-607). This port instead groups
/// the three metrics PER PROJECT into <see cref="ProjectAmounts"/> -- the same data, reorganized from
/// metric-then-project to project-then-metric, which reads more naturally as a table and avoids three
/// near-duplicate pivot-column lists. Totals are unaffected.
/// </summary>
public class ProjectMonthwiseSummaryReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy "Salary + Allowance": the employee's current EmployeeBasicPayDetail total.</summary>
    public double SalaryAllowance { get; set; }

    /// <summary>The dynamic per-project pivot block. Same projects, same order, on every row.</summary>
    public List<ProjectMonthwiseAmountDto> ProjectAmounts { get; set; } = new();

    public double TotalWorkingDays { get; set; }

    public double TotalOTHours { get; set; }

    /// <summary>Legacy "Salary": net Earning minus Deduction across every wage type not flagged DontImpactNetPay.</summary>
    public double Salary { get; set; }

    /// <summary>Legacy "OTAmount": sum of Amount over every wage type linked from an OvertimeType.</summary>
    public double OTAmount { get; set; }

    public double TotalAmount { get; set; }
}

/// <summary>One project's (WorkingDays, OTHours, Amount) triple -- see <see cref="ProjectMonthwiseSummaryReportRowDto"/> remarks.</summary>
public class ProjectMonthwiseAmountDto
{
    public string ProjectCode { get; set; }

    public string ProjectName { get; set; }

    /// <summary>Legacy "{Code} WD": sum of WageType-1000 Number for this project.</summary>
    public double WorkingDays { get; set; }

    /// <summary>Legacy "{Code} OT": sum of Number over every wage type linked from an OvertimeType, for this project.</summary>
    public double OTHours { get; set; }

    /// <summary>Legacy "{Code} AMT": net Earning minus Deduction Amount (excluding DontImpactNetPay), for this project.</summary>
    public double Amount { get; set; }
}
