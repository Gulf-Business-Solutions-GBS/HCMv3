namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// The manpower planning module's own approval vocabulary (ported from the legacy generic
/// <c>RequestStatus</c> used by <c>Workflow.WorkflowRequest</c>, which the legacy
/// <c>ManpowerPlanning</c> inherited from).
///
/// <para>
/// DERIVED, NEVER STORED. This is a presentation-level projection of
/// <c>IWorkflowEnabledEntity.WorkflowStatus</c>, computed by
/// <c>ManpowerPlanning.FromWorkflowStatus</c>. Nothing writes it -- the workflow engine is the single
/// source of truth for whether a plan is approved, and this enum only renames what the engine already
/// decided so that existing screens and report filters keep the vocabulary they were written against.
/// </para>
///
/// <para>
/// It was a stored column, alongside a bespoke Approve/Reject pair on
/// <c>ManpowerPlanningsAppService</c>, for exactly as long as the workflow module did not exist. Keeping it
/// stored once routing arrived would have given one plan two approval states with two independent writers,
/// which is the drift this module was routed to eliminate.
/// </para>
/// </summary>
public enum ManpowerPlanningStatus
{
    /// <summary>Not routed at all, or raised and not yet submitted. WorkflowStatus null or Draft.</summary>
    Draft = 0,

    /// <summary>In flight, sitting with an approver. WorkflowStatus Open (or Transferred).</summary>
    Submitted = 1,

    /// <summary>The chain completed. WorkflowStatus Approved or Closed.</summary>
    Approved = 2,

    /// <summary>Declined. WorkflowStatus Rejected.</summary>
    Rejected = 3
}
