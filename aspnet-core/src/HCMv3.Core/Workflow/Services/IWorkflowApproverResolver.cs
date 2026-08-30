using System.Collections.Generic;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Works out WHO a workflow step goes to.
///
/// <para>
/// Split out of <see cref="IWorkflowApprovalService"/> because approver resolution is the part of the engine
/// with real rules in it -- role holders narrowed by org scope, manager chains walked several levels,
/// signatories excluded from later signatory steps -- while the approval service itself is bookkeeping. It is
/// also the part escalation needs to reuse: an escalation that resolved approvers differently from a normal
/// step would route to somebody the configuration never nominated.
/// </para>
/// </summary>
public interface IWorkflowApproverResolver
{
    /// <summary>
    /// Approvers for a step, in the order they should be recorded. Empty when nothing matches, which the
    /// caller treats as a configuration failure rather than an approval.
    /// </summary>
    List<int> ResolveForStep(WorkflowRequest request, RequestMaster requestMaster, RequestDetail requestDetail);

    /// <summary>
    /// Approvers to escalate a stalled step TO.
    ///
    /// <para>
    /// Resolved from the step's own <c>EscalateWorkflowRole</c> rather than its ordinary role, and the people
    /// already sitting on the request are excluded -- escalating to the person who has not acted is what the
    /// feature exists to avoid.
    /// </para>
    /// </summary>
    List<int> ResolveForEscalation(
        WorkflowRequest request,
        RequestDetail requestDetail,
        IReadOnlyCollection<int> currentApproverIds);
}
