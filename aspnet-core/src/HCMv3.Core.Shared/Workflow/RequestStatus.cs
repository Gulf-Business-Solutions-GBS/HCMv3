namespace HCMv3.Workflow;

/// <summary>
/// Status of a <c>WorkflowRequest</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.RequestStatus</c>).
/// </summary>
public enum RequestStatus
{
    Open = 0,
    Approved = 1,
    Rejected = 2,
    Closed = 3,
    Draft = 4,
    Transferred = 5,

    /// <summary>
    /// A transaction that WAS approved and has since been cancelled through an approved cancellation request.
    /// Not in legacy, which deleted the row. Kept as a status so the transaction stays in the employee's
    /// history; the <c>ApprovedOnly</c> filter treats it like any other non-approved state, so payroll, quota
    /// and reports stop seeing the row the moment it is set.
    /// </summary>
    Cancelled = 6
}
