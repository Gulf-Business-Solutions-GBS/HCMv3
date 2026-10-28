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
    ManagerLevel7 = 8,

    /// <summary>
    /// The employee named in a field of the request itself -- for a leave, the person it hands the
    /// requester's responsibilities to, or a document withdrawal's guarantor. Which field is
    /// <c>RequestDetail.RequestPersonField</c>, a <see cref="WorkflowRequestPersonFields"/> code. A request that leaves the field empty, or names the
    /// requester, skips the step.
    /// </summary>
    PersonOnRequest = 9
}
