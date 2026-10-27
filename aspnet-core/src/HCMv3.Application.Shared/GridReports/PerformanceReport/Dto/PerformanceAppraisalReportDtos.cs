using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Filter input for the Performance Appraisal analysis grid report (legacy
/// <c>PerformanceReport/uscRptPerformanceAppraisal.ascx</c>). <c>dtFrom</c>/<c>dtTo</c> select
/// <see cref="HCMv3.PerformanceManagement.PAPeriod"/>s whose <see cref="HCMv3.PerformanceManagement.PATimeline"/>
/// window overlaps the requested range.
/// </summary>
public class GetPerformanceAppraisalReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPAPeriod</c> (matched against <see cref="HCMv3.PerformanceManagement.PAPeriod.Name"/>).</summary>
    public string PeriodName { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c>'s column set (uscRptPerformanceAppraisal.ascx.cs:494-683):
/// submission-status booleans per appraisal stage plus the overall/moderated score.
/// </summary>
public class PerformanceAppraisalReportRowDto : EntityDto<int>
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

    public string PayrollArea { get; set; }

    public string Period { get; set; }

    public bool PlanSubmittedByAppraisee { get; set; }

    public bool PlanSubmittedByAppraiser { get; set; }

    public bool AnnualReviewSubmittedByAppraisee { get; set; }

    public bool AnnualReviewSubmittedByAppraiser { get; set; }

    public double OverallScore { get; set; }

    public double ModeratedScore { get; set; }
}
