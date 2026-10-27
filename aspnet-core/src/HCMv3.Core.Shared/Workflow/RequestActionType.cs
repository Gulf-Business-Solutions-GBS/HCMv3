namespace HCMv3.Workflow;

/// <summary>
/// Type of action recorded in a <c>RequestLog</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestActionType</c>).
/// </summary>
public enum RequestActionType
{
    Created = 0,
    Approved = 1,
    Rejected = 2,
    Escalated = 3,
    Read = 4,
    SentBack = 5,
    Closed = 6,
    Remarked = 7,
    Assigned = 8,
    Transferred = 9,
    SentBackToCreator = 10,

    /// <summary>
    /// Somebody with the authority to do so recorded this transaction without sending it for approval.
    ///
    /// <para>
    /// Its own action rather than a silent absence of one. A row that never went to an approver and leaves
    /// no trace of why is indistinguishable from one that was approved, and "who decided this did not need
    /// signing off" is exactly the question an audit asks. Recording it as Approved would answer that
    /// question with something untrue.
    /// </para>
    /// </summary>
    RecordedDirectly = 11
}
