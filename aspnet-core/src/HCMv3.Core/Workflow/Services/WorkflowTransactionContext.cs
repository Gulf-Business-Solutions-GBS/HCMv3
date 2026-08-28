namespace HCMv3.Workflow.Services;

/// <summary>
/// Everything a transaction handler needs about the request being acted on, without the handler having to
/// re-query the engine's tables.
/// </summary>
public class WorkflowTransactionContext
{
    public WorkflowTransactionContext(WorkflowRequest request, int actingEmployeeId, string remark = null)
    {
        Request = request;
        ActingEmployeeId = actingEmployeeId;
        Remark = remark;
    }

    public WorkflowRequest Request { get; }

    /// <summary>The catalog code, e.g. "EmployeeAbsence".</summary>
    public string RequestType => Request?.RequestType;

    /// <summary>
    /// Primary key of the transaction row this request governs -- the <c>EmployeeAbsence.Id</c> for a leave
    /// request. Null only while a request exists before its target row does.
    /// </summary>
    public int? TargetEntityId => Request?.TargetEntityId;

    /// <summary>The employee the request is about (not necessarily the one acting).</summary>
    public int? EmployeeId => Request?.EmployeeId;

    /// <summary>The employee performing the current action -- submitter, approver, or rejecter.</summary>
    public int ActingEmployeeId { get; }

    /// <summary>The comment attached to the current action, if any.</summary>
    public string Remark { get; }
}

/// <summary>
/// The one-line description of a request, written once by its handler at submit time and stored on
/// <see cref="WorkflowRequest.SummaryEn"/>/<see cref="WorkflowRequest.SummaryAr"/>.
///
/// <para>
/// This is what lets the approver inbox list every request type in a single query. Without it the inbox
/// would have to reach into a different transaction table per row just to render a label.
/// </para>
/// </summary>
public class WorkflowSummary
{
    public WorkflowSummary(string summaryEn, string summaryAr)
    {
        SummaryEn = summaryEn;
        SummaryAr = summaryAr;
    }

    public string SummaryEn { get; }

    public string SummaryAr { get; }
}
