namespace HCMv3.Workflow;

/// <summary>
/// Approval mode for a <c>RequestDetail</c> workflow step (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.ApprovalRequire</c>).
/// </summary>
public enum ApprovalRequire
{
    Single = 0,
    All = 1,
    Parallel = 2
}
