namespace HCMv3.Workflow;

/// <summary>
/// Grouping dimension used to resolve a <c>WorkflowRole</c>'s scope (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowRoleGroup</c>).
/// </summary>
public enum WorkflowRoleGroup
{
    None = 0,
    Company = 2,
    Department = 3,
    Branch = 4,
    CostCenter = 5
}
