namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Simplified approval status for <c>ManpowerPlanning</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanning</c>, which inherited from
/// <c>Workflow.WorkflowRequest</c> and used a generic <c>RequestStatus</c> enum + full multi-step approval
/// workflow engine).
///
/// DELIBERATE SIMPLIFICATION: the Workflow module (WorkflowRequest/RequestMaster/WorkflowStep/
/// WorkFlowApprover/RequestLog/DocAttachment/etc.) is not ported anywhere in this codebase yet, so
/// <c>ManpowerPlanning</c> is ported as a standalone entity (not a WorkflowRequest subclass) with this
/// small dedicated status enum standing in for the legacy <c>RequestStatus</c> values actually used by this
/// module's logic (Draft, Approved, Rejected). <c>Submitted</c> is added so the UI has a distinct
/// "awaiting approval" state without needing the full step-based workflow engine.
/// </summary>
public enum ManpowerPlanningStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}
