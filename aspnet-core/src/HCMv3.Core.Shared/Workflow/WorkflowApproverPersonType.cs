namespace HCMv3.Workflow;

/// <summary>
/// How the approver of a given <c>RequestDetail</c> workflow step is resolved (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.ApproverPerson</c>).
/// </summary>
public enum WorkflowApproverPersonType
{
    None = 0,
    Person = 1,
    PersonManager = 2,
    ManagerLevel2 = 3,
    ManagerLevel3 = 4,
    ManagerLevel4 = 5,
    ManagerLevel5 = 6,
    ManagerLevel6 = 7,
    ManagerLevel7 = 8
}
