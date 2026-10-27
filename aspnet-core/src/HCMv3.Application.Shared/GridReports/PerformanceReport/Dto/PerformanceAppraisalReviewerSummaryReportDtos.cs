using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Filter input for the Performance Appraisal Reviewer Summary analysis grid report (legacy customer
/// variant <c>PerformanceReport/uscRptPerformanceAppraisal_Custom012.ascx</c>). Unlike the base report,
/// this variant filters by <see cref="HCMv3.PerformanceManagement.PACycle"/> (legacy <c>cmbPAPeriod</c>
/// rebound to <c>PACycle</c>, not <c>PAPeriod</c>) rather than a date-range/PATimeline overlap.
/// </summary>
public class GetPerformanceAppraisalReviewerSummaryReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPAPeriod</c> (matched against <see cref="HCMv3.PerformanceManagement.PACycle.NameEn"/>).</summary>
    public string CycleName { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One row per employee with a reviewer-based 360 appraisal (<see cref="HCMv3.PerformanceManagement.PAEmployee"/>
/// model). Mirrors legacy Custom012 <c>Table1()</c>, EXCEPT for the fields that depended on the legacy
/// Workflow module (<c>PAEmployee.RequestStatus</c>/<c>WorkflowStep</c>/<c>RequestLog</c>): those are not
/// reproduced because <c>HCMv3.PerformanceManagement.PAEmployee</c> deliberately dropped the legacy
/// <c>WorkflowRequest</c> base class (see that entity's class remarks) -- an architectural decision made
/// before this report was ported, not a defect introduced here. Self/manager review scores instead come
/// straight from <see cref="HCMv3.PerformanceManagement.PAEmployeeReviewer"/>.
/// </summary>
public class PerformanceAppraisalReviewerSummaryReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    public string JobTitle { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string WorkLocation { get; set; }

    public string Country { get; set; }

    public string Manager { get; set; }

    public double SelfAssessmentScore { get; set; }

    /// <summary>Legacy "FinalScore" -- the current manager's average review score.</summary>
    public double FinalScore { get; set; }

    /// <summary>Legacy "FinalRating" -- the current manager's average review rating name.</summary>
    public string FinalRating { get; set; }

    public string AppraiseeStrengthAreas { get; set; }

    public string AppraiseeAreasOfImprovements { get; set; }

    public string OverallEvaluationSummaryAndTargetedTrainingNeeds { get; set; }
}
