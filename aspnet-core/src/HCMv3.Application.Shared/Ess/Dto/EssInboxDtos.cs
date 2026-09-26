using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Which tab is open. Null keeps the original behaviour -- approvals and feedback asks together -- so
    /// a caller that predates the tabs is unaffected.
    /// </summary>
    public EssInboxTab? Tab { get; set; }

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

/// <summary>
/// Which slice of the inbox to show.
///
/// <para>
/// Four rather than the six the reference product offers. "Reassigned to others" and "requests for
/// information" split hairs nobody acts on; what remains are the four questions somebody actually asks of
/// their inbox -- what needs me, what did I push back, what am I still waiting on, and what was I asked
/// about.
/// </para>
/// </summary>
public enum EssInboxTab
{
    /// <summary>Waiting on this person to decide. The default, and the only one with a badge.</summary>
    AwaitingMe = 0,

    /// <summary>Asked for their opinion, and they have not given it yet.</summary>
    Feedback = 1,

    /// <summary>
    /// They sent it back to the requester and it is still open. Worth its own tab because a send-back
    /// leaves the approver's inbox entirely -- without this, the thing they were most recently involved in
    /// is the one they can no longer find.
    /// </summary>
    SentBack = 2,

    /// <summary>
    /// They approved it and it is still moving through later steps. Not actionable, which is the point: it
    /// answers "did that go through?" without anybody having to ask the next approver.
    /// </summary>
    Approved = 3
}

/// <summary>How many rows each tab holds, so the tab strip can say so before it is opened.</summary>
public class EssInboxCountsDto
{
    public int AwaitingMe { get; set; }

    public int Feedback { get; set; }

    public int SentBack { get; set; }

    public int Approved { get; set; }
}

public class BulkApproveInput
{
    /// <summary>
    /// The requests to approve. Capped because this is a convenience for clearing a morning's inbox, not a
    /// batch job -- and every one of them runs the full approval engine, transitions included.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public List<int> WorkflowRequestIds { get; set; } = new List<int>();

    /// <summary>
    /// One comment applied to all of them, if given. Optional, and its absence is what makes a step that
    /// demands a comment get skipped rather than approved.
    /// </summary>
    [StringLength(4000)]
    public string Comment { get; set; }
}

/// <summary>What happened to one request in a bulk approval.</summary>
public enum BulkApproveOutcome
{
    Approved = 0,

    /// <summary>
    /// The step requires a comment and none was given. Skipped rather than failed: nothing went wrong, the
    /// request simply cannot be approved without somebody saying something, and it stays in the inbox for
    /// them to open properly.
    /// </summary>
    SkippedCommentRequired = 1,

    /// <summary>The engine refused it. The reason is carried in the message.</summary>
    Failed = 2
}

public class BulkApproveItemResultDto
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public BulkApproveOutcome Outcome { get; set; }

    /// <summary>Why, when it was not approved. Empty on success.</summary>
    public string Message { get; set; }
}

/// <summary>
/// The outcome of a bulk approval, row by row.
///
/// <para>
/// Deliberately not a throw-on-first-failure batch. Approving twenty requests where the eleventh is
/// refused must leave the other nineteen approved and say which one was not -- the alternative is an
/// all-or-nothing operation that fails for a reason the user cannot see and cannot fix from the list.
/// </para>
/// </summary>
public class BulkApproveResultDto
{
    public int ApprovedCount { get; set; }

    public int SkippedCount { get; set; }

    public int FailedCount { get; set; }

    public List<BulkApproveItemResultDto> Items { get; set; } = new List<BulkApproveItemResultDto>();
}
