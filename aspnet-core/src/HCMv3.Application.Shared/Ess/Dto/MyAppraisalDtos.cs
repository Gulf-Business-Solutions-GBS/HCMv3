using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using HCMv3.PerformanceManagement;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>Which of the three appraisal lists to read.</summary>
public enum MyAppraisalListScope
{
    /// <summary>Appraisals about the signed-in employee.</summary>
    Mine = 0,

    /// <summary>Appraisals the signed-in employee is a reviewer or a current approver on.</summary>
    ToReview = 1,

    /// <summary>Appraisals about the signed-in employee's direct reports.</summary>
    Team = 2
}

public class GetMyAppraisalsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public MyAppraisalListScope Scope { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Id DESC";
        }
    }
}

/// <summary>One appraisal row in a self-service list.</summary>
public class MyAppraisalListDto : EntityDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    public string PACycleNameEn { get; set; }

    public string PACycleNameAr { get; set; }

    public string FormTypeNameEn { get; set; }

    public string FormTypeNameAr { get; set; }

    public string RequestNumber { get; set; }

    public int? WorkflowRequestId { get; set; }

    /// <summary>Null when the appraisal was recorded without workflow.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public DateTime CreationTime { get; set; }
}

/// <summary>What the signed-in employee may see and do on this appraisal. See <c>AppraisalReviewAccessRules</c>.</summary>
public class MyAppraisalAccessDto
{
    public bool IsOpen { get; set; }

    public bool IsAppraisee { get; set; }

    public bool IsApprover { get; set; }

    public bool IsReviewer { get; set; }

    public bool ReviewSubmitted { get; set; }

    public bool CanCopyKpi { get; set; }

    public bool CanSaveReview { get; set; }

    public bool CanSubmitReview { get; set; }

    public bool CanAddPeer { get; set; }

    public bool ShowKpiDetails { get; set; }

    public bool ShowRatings { get; set; }

    public bool ShowReviewers { get; set; }

    public bool ShowAverages { get; set; }

    /// <summary>Marks are entered instead of a rating (Appraisal Settings "Enable Marks In PA Review").</summary>
    public bool UseMarks { get; set; }
}

/// <summary>One KPI on the appraisal.</summary>
public class MyAppraisalKpiDto : EntityDto
{
    public string KpiNameEn { get; set; }

    public string KpiNameAr { get; set; }

    public string KpiCategoryNameEn { get; set; }

    public string KpiCategoryNameAr { get; set; }

    public string KpiDescription { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }
}

/// <summary>One reviewer's rating of one KPI.</summary>
public class MyAppraisalRatingLineDto : EntityDto
{
    public int PAEmployeeReviewerId { get; set; }

    public int ReviewerEmployeeId { get; set; }

    public string ReviewerNameEn { get; set; }

    public string ReviewerNameAr { get; set; }

    /// <summary>The line belongs to the signed-in employee, so it is theirs to fill in.</summary>
    public bool IsOwn { get; set; }

    public int PAEmployeeDetailId { get; set; }

    public string KpiNameEn { get; set; }

    public string KpiNameAr { get; set; }

    public string KpiCategoryNameEn { get; set; }

    public string KpiCategoryNameAr { get; set; }

    public string KpiDescription { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }

    public int? RatingId { get; set; }

    public string RatingNameEn { get; set; }

    public string RatingNameAr { get; set; }

    public double Marks { get; set; }

    public string Comments { get; set; }
}

/// <summary>One person on the reviewer list.</summary>
public class MyAppraisalReviewerDto : EntityDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>
    /// "Employee", "Employee Manager", a relationship type's name, or "Peer" -- as legacy ESS labelled them --
    /// already in the reader's language.
    /// </summary>
    public string ReviewerType { get; set; }

    public bool IsPeer { get; set; }

    public bool ReviewSubmitted { get; set; }

    public double AverageScore { get; set; }

    public double TotalMarks { get; set; }

    public string AverageRatingNameEn { get; set; }

    public string AverageRatingNameAr { get; set; }
}

/// <summary>A rating the signed-in employee may choose, with how many more lines may still carry it.</summary>
public class MyAppraisalRatingOptionDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string DescriptionEn { get; set; }

    public string DescriptionAr { get; set; }

    public bool IsCommentRequired { get; set; }

    public int MaxLimit { get; set; }

    /// <summary>
    /// How many more of the signed-in employee's lines may carry this rating (legacy
    /// <c>GetRatingBalanceLimit</c>): its limit less the lines already holding it, never below zero.
    /// </summary>
    public int RemainingLimit { get; set; }

    public double FromRange { get; set; }

    public double ToRange { get; set; }
}

/// <summary>Everything the appraisal review form shows for the signed-in employee.</summary>
public class MyAppraisalReviewDto : EntityDto
{
    public string RequestNumber { get; set; }

    public int? WorkflowRequestId { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public int WorkflowStep { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    public string PACycleNameEn { get; set; }

    public string PACycleNameAr { get; set; }

    public string FormTypeNameEn { get; set; }

    public string FormTypeNameAr { get; set; }

    public MyAppraisalAccessDto Access { get; set; }

    /// <summary>Set only when the averages may be shown.</summary>
    public double? OverallAverageScore { get; set; }

    public double? PeerAverageScore { get; set; }

    public double? EmployeeAverageScore { get; set; }

    public List<MyAppraisalKpiDto> Kpis { get; set; } = new();

    public List<MyAppraisalRatingLineDto> RatingLines { get; set; } = new();

    public List<MyAppraisalReviewerDto> Reviewers { get; set; } = new();

    public List<MyAppraisalRatingOptionDto> RatingOptions { get; set; } = new();

    /// <summary>The signed-in reviewer's own free-text boxes (legacy custom fields 1-3).</summary>
    public string CustomTextField1 { get; set; }

    public string CustomTextField2 { get; set; }

    public string CustomTextField3 { get; set; }
}

/// <summary>One rating line as the reviewer filled it in.</summary>
public class SaveMyAppraisalRatingLineInput
{
    /// <summary>The <c>PAEmployeeRating</c> id. Must be one of the signed-in employee's own lines.</summary>
    public int Id { get; set; }

    public int? RatingId { get; set; }

    [Range(0, 100000)]
    public double Marks { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}

/// <summary>Save Review / Submit Review: the signed-in reviewer's lines and free-text boxes.</summary>
public class SaveMyAppraisalReviewInput
{
    [Range(1, int.MaxValue)]
    public int PAEmployeeId { get; set; }

    public List<SaveMyAppraisalRatingLineInput> Lines { get; set; } = new();

    [StringLength(4000)]
    public string CustomTextField1 { get; set; }

    [StringLength(4000)]
    public string CustomTextField2 { get; set; }

    [StringLength(4000)]
    public string CustomTextField3 { get; set; }
}

/// <summary>What Submit Review did.</summary>
public class SubmitMyAppraisalReviewResultDto
{
    /// <summary>
    /// The submission was the last one outstanding on an "auto submit on last fill" step, so the step was
    /// approved as its first approver.
    /// </summary>
    public bool StepApproved { get; set; }
}

/// <summary>Copy KPI: where from.</summary>
public class CopyMyAppraisalKpiInput
{
    [Range(1, int.MaxValue)]
    public int PAEmployeeId { get; set; }

    public KpiFrom KpiFrom { get; set; }

    /// <summary>Required when copying from a job title.</summary>
    public int? KpiByJobTitleId { get; set; }

    /// <summary>Required when copying from an organization unit.</summary>
    public int? KpiByOrgUnitId { get; set; }

    /// <summary>Required when copying from another employee's appraisal in the same cycle.</summary>
    public int? SourceEmployeeId { get; set; }
}

/// <summary>One KPI set Copy KPI can take from.</summary>
public class MyAppraisalKpiSourceDto : EntityDto
{
    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

/// <summary>A person the screen can pick: a peer, or the employee to copy KPIs from.</summary>
public class MyAppraisalPersonDto : EntityDto
{
    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }
}

/// <summary>The three lists Copy KPI chooses from.</summary>
public class MyAppraisalKpiSourcesDto
{
    public List<MyAppraisalKpiSourceDto> JobTitles { get; set; } = new();

    public List<MyAppraisalKpiSourceDto> OrganizationUnits { get; set; } = new();

    /// <summary>Employees with an appraisal in the same cycle that has KPIs.</summary>
    public List<MyAppraisalPersonDto> Employees { get; set; } = new();
}

public class AddMyAppraisalPeerInput
{
    [Range(1, int.MaxValue)]
    public int PAEmployeeId { get; set; }

    [Range(1, int.MaxValue)]
    public int PeerEmployeeId { get; set; }
}

public class GetMyAppraisalPeerCandidatesInput
{
    [Range(1, int.MaxValue)]
    public int PAEmployeeId { get; set; }

    /// <summary>Code or name, optional.</summary>
    [StringLength(128)]
    public string Filter { get; set; }
}
