namespace HCMv3.Workflow;

/// <summary>
/// Originating platform of a workflow action (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.Platform</c>, renamed here with a
/// <c>Workflow</c> prefix to avoid the bare, generic <c>Platform</c> enum name colliding with
/// other modules' schemas in NSwag).
/// </summary>
public enum WorkflowPlatform
{
    Core = 0,
    ESS = 1,
    Mobile = 2
}
