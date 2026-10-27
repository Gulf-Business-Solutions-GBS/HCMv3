using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Filter input for the PA Employee Scorecard analysis grid report (legacy customer variant
/// <c>PerformanceReport/uscRptPAEmployee_Custom033.ascx</c>). Same filter set as the base
/// <see cref="GetPAEmployeeReportInput"/> -- FormType/Period/EmployeeCode -- the variant only changes the
/// column set and adds a weighted KPI-category scorecard.
/// </summary>
public class GetPAEmployeeScorecardReportInput : GridReportInputBase
{
    public string FormTypeCode { get; set; }

    public string PeriodCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy Custom033 <c>Table1()</c>'s column set: Code, Employee, JobTitle,
/// OrgLevel1-7, CostCenter, Manager, ManagerJobTitle, Behavior/Technical/Overall percentage+rating.
///
/// Legacy computes a weighted Behavior (KpiCategory code "1") and Technical (code "2") percentage per
/// PAEmployee from <c>PAEmployeeRating</c> rows, preferring the current manager's ratings when ANY manager
/// rating exists across the whole filtered batch (a legacy quirk: the manager-only/all-reviewers switch is
/// evaluated once for the batch, not per employee) and falling back to every reviewer's ratings otherwise.
/// </summary>
public class PAEmployeeScorecardReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string CostCenter { get; set; }

    public string Manager { get; set; }

    public string ManagerJobTitle { get; set; }

    public double BehaviorPercentage { get; set; }

    public string BehaviorRating { get; set; }

    public double TechnicalPercentage { get; set; }

    public string TechnicalRating { get; set; }

    public double OverallAveragePercentage { get; set; }

    public string OverallAverageRating { get; set; }
}
