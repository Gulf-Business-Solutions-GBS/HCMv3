using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One request waiting on the current user, whatever kind of transaction it is.
///
/// <para>
/// Every field here comes from <c>WorkflowRequest</c> and <c>Employee</c> alone -- nothing is read from the
/// transaction tables. That is what <c>SummaryEn</c>/<c>SummaryAr</c> are for, and it is why the inbox is a
/// single indexed query rather than a fan-out across a table per type.
/// </para>
/// </summary>
public class EssInboxItemDto : EntityDto<int>
{
    /// <summary>Catalog code, e.g. "EmployeeAbsence" -- used to pick an icon and to route to the detail.</summary>
    public string RequestType { get; set; }

    public string RequestTypeNameEn { get; set; }

    public string RequestTypeNameAr { get; set; }

    public string RequestNumber { get; set; }

    /// <summary>The one-line description the type's handler wrote at submit time.</summary>
    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public int? RequesterEmployeeId { get; set; }

    public string RequesterNameEn { get; set; }

    public string RequesterNameAr { get; set; }

    public string RequesterPhotoUrl { get; set; }

    public DateTime? SubmittedOn { get; set; }

    public int WorkflowStep { get; set; }

    public int TotalSteps { get; set; }

    /// <summary>False when the step's configuration forbids it, so the screen does not offer a button that
    /// would be refused.</summary>
    public bool CanReject { get; set; }

    public bool CanSendBack { get; set; }

    /// <summary>
    /// Legacy <c>ViewBag.EnableTravelcost</c>: true when this is a travel request sitting at the workflow step named by
    /// <c>ManageWorkflowSettings.EnableTravelCostOnWorkflowstep</c>, so the approver may enter the travel cost.
    /// </summary>
    public bool EnableTravelCost { get; set; }

    /// <summary>
    /// What this row wants from the reader.
    ///
    /// <para>
    /// The inbox used to hold one kind of thing, so it needed no discriminator. It now also carries
    /// requests somebody has been asked to comment on, and the difference matters before the row is even
    /// opened: an approval is a decision this person must make, and a feedback request is an opinion
    /// somebody would like. Showing them identically would have people approving things they were only
    /// asked about -- or, far more likely, ignoring an ask because it looked like a duplicate approval.
    /// </para>
    /// </summary>
    public EssInboxItemKind Kind { get; set; }

    /// <summary>Who asked for the feedback. Null on an approval row, where nobody had to ask.</summary>
    public int? FeedbackRequestedByEmployeeId { get; set; }

    public string FeedbackRequestedByNameEn { get; set; }

    public string FeedbackRequestedByNameAr { get; set; }

    /// <summary>What they wanted looked at, if they said.</summary>
    public string FeedbackNote { get; set; }
}

/// <summary>Which of the two things an inbox row is.</summary>
public enum EssInboxItemKind
{
    /// <summary>The request is waiting on this person to decide it.</summary>
    Approval = 0,

    /// <summary>
    /// An approver has asked this person what they think. They can read it and comment; they cannot
    /// approve it, and the approver is not waiting on them.
    /// </summary>
    FeedbackRequest = 1
}

public class GetMyInboxInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter to one transaction type.</summary>
    public string RequestType { get; set; }

    /// <summary>
    /// Optional filter to approvals or to feedback requests. Null returns both, which is what the
    /// unfiltered inbox shows.
    /// </summary>
    public EssInboxItemKind? Kind { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "CreationTime";
        }
    }
}

public class ApproveRequestInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }

    [StringLength(4000)]
    public string Comment { get; set; }
}

public class RejectRequestInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }

    /// <summary>
    /// Required, unlike on approval. A rejection the requester cannot understand is one they will simply
    /// resubmit unchanged.
    /// </summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Comment { get; set; }
}

public class SendBackRequestInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }

    /// <summary>Required: the requester is being asked to change something and needs to know what.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Comment { get; set; }
}
