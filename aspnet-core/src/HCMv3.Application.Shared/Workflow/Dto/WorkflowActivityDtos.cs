using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>What produced a feed entry. The UI picks a card style from this; it never needs to know which
/// table the row came from.</summary>
public enum WorkflowActivityKind
{
    /// <summary>An approval action -- submitted, approved, rejected, sent back. From <c>RequestLog</c>.</summary>
    Action = 0,

    /// <summary>A free-form comment or reply. From <c>WorkflowComment</c>.</summary>
    Comment = 1,

    /// <summary>Someone was looped in for ad-hoc feedback. From <c>WorkflowRemark</c>.</summary>
    FeedbackRequested = 2
}

/// <summary>
/// One entry in a request's activity feed.
///
/// <para>
/// The feed merges three tables into this single shape, sorted chronologically: the immutable action trail
/// (<c>RequestLog</c>), free-form discussion (<c>WorkflowComment</c>), and ad-hoc feedback assignments
/// (<c>WorkflowRemark</c>). Legacy showed only the first of those, as a Syncfusion data grid with columns
/// for Code / ActionBy / Jobtitle / Step / Date / Time / ActionType / Comment.
/// </para>
/// </summary>
public class WorkflowActivityDto
{
    public WorkflowActivityKind Kind { get; set; }

    /// <summary>Primary key within the source table. Only unique per <see cref="Kind"/>, so the client keys
    /// rows on Kind + Id.</summary>
    public int Id { get; set; }

    public int? ActorEmployeeId { get; set; }

    public string ActorNameEn { get; set; }

    public string ActorNameAr { get; set; }

    public string ActorJobTitleEn { get; set; }

    /// <summary>
    /// Where to fetch the author's photo. Served from a dedicated cached endpoint rather than inlined:
    /// <c>Employee.Photo</c> is a byte[] on a 70-column entity, and embedding it per entry would make a
    /// twenty-comment thread very heavy.
    /// </summary>
    public string ActorPhotoUrl { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>Set when <see cref="Kind"/> is <see cref="WorkflowActivityKind.Action"/>.</summary>
    public RequestActionType? ActionType { get; set; }

    /// <summary>The comment text, or an action's attached remark.</summary>
    public string Body { get; set; }

    public int? WorkflowStep { get; set; }

    /// <summary>True only for approver-only comments. The requester never receives these at all, so this
    /// flag exists to badge them for the approvers who can see them.</summary>
    public bool IsInternal { get; set; }

    public bool IsEdited { get; set; }

    public int? ParentCommentId { get; set; }

    /// <summary>Whether the current user may edit or delete this entry. Actions are immutable audit rows and
    /// are never editable; a comment is editable only by its author.</summary>
    public bool CanEdit { get; set; }
}

public class GetWorkflowActivityFeedInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }
}

public class WorkflowActivityFeedDto
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public RequestStatus RequestStatus { get; set; }

    /// <summary>Whether the current user may post a comment. False for a read-only viewer.</summary>
    public bool CanComment { get; set; }

    /// <summary>Whether the current user may post approver-only comments -- true for approvers, false for
    /// the employee who raised the request.</summary>
    public bool CanCommentInternally { get; set; }

    public List<WorkflowActivityDto> Activities { get; set; } = new List<WorkflowActivityDto>();
}

public class AddWorkflowCommentInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }

    [Required]
    [StringLength(WorkflowCommentConsts.MaxBodyLength, MinimumLength = 1)]
    public string Body { get; set; }

    /// <summary>Approver-only. Rejected for the employee who raised the request, who could otherwise post a
    /// comment they would then be unable to see.</summary>
    public bool IsInternal { get; set; }

    public int? ParentCommentId { get; set; }
}

public class EditWorkflowCommentInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [StringLength(WorkflowCommentConsts.MaxBodyLength, MinimumLength = 1)]
    public string Body { get; set; }
}

/// <summary>Mirrors <c>WorkflowComment.MaxBodyLength</c>; DTO validation attributes need a compile-time
/// constant reachable from Application.Shared, which cannot reference Core.</summary>
public static class WorkflowCommentConsts
{
    public const int MaxBodyLength = 4000;
}
