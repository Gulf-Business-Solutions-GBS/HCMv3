namespace HCMv3.Workflow;

/// <summary>
/// Security level for a <c>RequestMaster</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.SecurityLevel</c>, renamed with a
/// <c>Workflow</c> prefix here to avoid a bare, easily-collided enum name across modules).
/// </summary>
public enum WorkflowSecurityLevel
{
    EmployeeOnly = 0,
    EmployeeManager = 1,
    EmployeeDepartment = 2,
    Everyone = 3
}
