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
    Transferred = 5
}
