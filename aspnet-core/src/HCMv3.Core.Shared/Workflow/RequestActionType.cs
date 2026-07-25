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
    SentBackToCreator = 10
}
