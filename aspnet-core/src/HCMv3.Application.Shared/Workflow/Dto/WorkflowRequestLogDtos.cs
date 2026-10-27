using System;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Asks for one request's action trail as a grid.
/// </summary>
public class GetWorkflowRequestLogInput
{
    public int WorkflowRequestId { get; set; }
}

/// <summary>
/// One row of the request log, as legacy's "Request Log" tab presented it.
///
/// <para>
/// This is the same data the conversation thread already renders, in the other shape. The thread answers
/// "what happened on this request" for a person reading it once; the grid answers "how long did each step
/// take, on which platform, by whom" for somebody comparing many requests or exporting them. The user asked
/// for the legacy functionality to be <i>reportable and exportable</i>, and a chat thread is neither.
/// </para>
///
/// <para>
/// Action date and action time were two columns in legacy and stay two here. One timestamp would be tidier
/// and would also stop anyone sorting by time-of-day or filtering a day's activity in Excel, which is most
/// of why the split existed.
/// </para>
/// </summary>
public class WorkflowRequestLogEntryDto
{
    public int Id { get; set; }

    public int? ActionByEmployeeId { get; set; }

    /// <summary>Employee code. What makes a row unambiguous when two people share a name.</summary>
    public string ActionByCode { get; set; }

    public string ActionByNameEn { get; set; }

    public string ActionByNameAr { get; set; }

    public string ActionByPhotoUrl { get; set; }

    /// <summary>The whole timestamp. The client splits it for display; the export writes both columns.</summary>
    public DateTime ActionDateTime { get; set; }

    public RequestActionType ActionType { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>
    /// Days this action sat waiting, counted from the previous action rather than from submission -- see
    /// <c>WorkflowApprovalService.CalculateActionDaysAsync</c>.
    /// </summary>
    public int ActionDays { get; set; }

    public WorkflowPlatform Platform { get; set; }

    public string Comment { get; set; }
}
