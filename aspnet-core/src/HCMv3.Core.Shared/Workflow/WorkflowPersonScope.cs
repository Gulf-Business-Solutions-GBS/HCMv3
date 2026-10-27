namespace HCMv3.Workflow;

/// <summary>
/// Who is allowed to raise a request at a given <c>RequestDetail</c> step (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.Person</c>, renamed here with a
/// <c>Workflow</c> prefix to avoid the bare, generic <c>Person</c> enum name colliding with
/// other modules' schemas in NSwag).
/// </summary>
public enum WorkflowPersonScope
{
    None = 0,
    Themselves = 1,
    Department = 2,
    SelfSubordinates = 3,
    All = 4
}
