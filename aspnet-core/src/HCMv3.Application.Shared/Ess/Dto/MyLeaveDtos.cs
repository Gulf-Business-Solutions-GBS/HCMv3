using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>One absence type's balance, as the employee sees it.</summary>
public class MyLeaveBalanceDto
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public double Accrued { get; set; }

    /// <summary>Consumed by approved leave.</summary>
    public double Used { get; set; }

    /// <summary>Submitted but not yet decided. Shown separately so the employee can see why bookable is
    /// lower than the balance they expected.</summary>
    public double Pending { get; set; }

    public double Remaining { get; set; }

    /// <summary>What they may still apply for. The number the apply form validates against.</summary>
    public double Bookable { get; set; }
}

public class MyLeaveDto : EntityDto<int>
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalLeaveDays { get; set; }

    public string Comments { get; set; }

    /// <summary>Null when the leave was entered directly by HR rather than requested.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    /// <summary>Whether the employee may still withdraw it -- false once it is approved or already
    /// decided.</summary>
    public bool CanCancel { get; set; }
}

public class GetMyLeavesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>When false (the default) only in-flight and future leave is listed, which is what an
    /// employee usually wants; true includes their full history.</summary>
    public bool IncludeHistory { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}

public class ApplyForLeaveInput
{
    [Range(1, int.MaxValue)]
    public int AbsenceTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}

/// <summary>
/// What happened to an application. The two outcomes are genuinely different and the UI must say which:
/// routed for approval, or effective immediately.
/// </summary>
public class ApplyForLeaveResultDto
{
    public int EmployeeAbsenceId { get; set; }

    /// <summary>Null when the type has workflow switched off -- the leave is simply recorded.</summary>
    public int? WorkflowRequestId { get; set; }

    /// <summary>
    /// False when the type is not workflow-enabled, so the screen can say "recorded" rather than implying
    /// an approval that will never come.
    /// </summary>
    public bool SubmittedForApproval { get; set; }
}
