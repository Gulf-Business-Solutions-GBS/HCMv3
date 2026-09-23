using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class GetWorkflowRequestDetailInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }
}

/// <summary>One read-only row of the transaction, as its handler projected it.</summary>
public class WorkflowDetailFieldDto
{
    /// <summary>Already localized by the handler, which is the only code that knows what its columns mean.</summary>
    public string Label { get; set; }

    /// <summary>
    /// The transport form of the value, whose format is fixed by <see cref="Kind"/>: ISO-8601 for dates,
    /// invariant numerals for numbers and money, "true"/"false" for booleans, and the display string itself
    /// for text. Null means the field was not filled in.
    /// </summary>
    public string Value { get; set; }

    public WorkflowDetailFieldKind Kind { get; set; }
}

/// <summary>
/// One request, read the way its requester wrote it.
///
/// <para>
/// The endpoint behind this exists because the approver screen had no way to reach a transaction's fields at
/// all: the inbox row carries the one-line summary its handler froze at submit, and nothing else. An
/// approver was therefore deciding on a sentence. For an exit permit that sentence happened to mention the
/// permit number and the exit date; the start date it never mentioned was simply not obtainable, and for
/// most of the other twenty-nine types the sentence says even less.
/// </para>
///
/// <para>
/// <b>Generic on purpose.</b> A list of label/value pairs plus a kind is the most a single screen can render
/// for thirty transaction types without knowing any of them, and it is enough -- the client formats by kind
/// and lays out by order. The alternative, a typed detail DTO per type, is thirty endpoints and thirty
/// approval screens, which is what legacy had and what this design exists to avoid.
/// </para>
/// </summary>
public class WorkflowRequestDetailDto
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    /// <summary>Catalog code, e.g. "EmployeeExitPermit".</summary>
    public string RequestType { get; set; }

    public string RequestTypeNameEn { get; set; }

    public string RequestTypeNameAr { get; set; }

    /// <summary>The transaction row this request governs. Null while a request exists before its row does.</summary>
    public int? TargetEntityId { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public DateTime? SubmittedOn { get; set; }

    public int WorkflowStep { get; set; }

    // ------------------------------------------------------------------ who it is about

    public int? RequesterEmployeeId { get; set; }

    public string RequesterNameEn { get; set; }

    public string RequesterNameAr { get; set; }

    public string RequesterCode { get; set; }

    public string RequesterJobTitleEn { get; set; }

    public string RequesterJobTitleAr { get; set; }

    public string RequesterPhotoUrl { get; set; }

    // ------------------------------------------------------------------ what they submitted

    /// <summary>
    /// Every field of the transaction, in the order the handler chose -- which is the order the form
    /// captured them, so the approver reads the request the way it was written.
    /// </summary>
    public List<WorkflowDetailFieldDto> Fields { get; set; } = new List<WorkflowDetailFieldDto>();

    /// <summary>The note the requester typed when submitting, if their transaction keeps one.</summary>
    public string SubmissionComment { get; set; }

    /// <summary>
    /// True when the type has no projection of its own and this is the engine's fallback -- request number,
    /// submitted date and the summary. Lets the screen say so instead of implying the request has three
    /// fields.
    /// </summary>
    public bool IsDefaultProjection { get; set; }

    // ------------------------------------------------------------------ what this viewer may do

    /// <summary>True when the current user is the approver this request is presently waiting on, which is
    /// what entitles them to the decision buttons.</summary>
    public bool IsCurrentApprover { get; set; }

    public bool CanApprove { get; set; }

    public bool CanReject { get; set; }

    public bool CanSendBack { get; set; }
}
