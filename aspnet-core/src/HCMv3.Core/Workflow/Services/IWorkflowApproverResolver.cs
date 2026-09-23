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

    /// <summary>
    /// Who <paramref name="actingEmployeeId"/> is standing in FOR on this step, or null when they are acting
    /// in their own right.
    ///
    /// <para>
    /// Exists so the audit trail can name both people. <see cref="ResolveForStep"/> hands the step to the
    /// stand-in and, having done so, the stand-in looks like any other approver -- which would quietly lose
    /// the fact that the chain nominated somebody else and a delegation is why it moved. Recomputed from the
    /// step's own configuration rather than guessed from "does this person hold any delegation", because a
    /// colleague who covers for one manager and is separately a real approver for a different chain must not
    /// have unrelated approvals logged as being on anyone's behalf.
    /// </para>
    /// </summary>
    /// <summary>
    /// Everyone currently holding <paramref name="workflowRoleId"/>, narrowed to the requester's own slice of
    /// the organisation when <paramref name="roleGroup"/> asks for it.
    ///
    /// <para>
    /// Exposed for the notification side, which sends to role holders exactly as the routing side picks them.
    /// A notification that reached a different set of people from the ones who can act on the request is a
    /// support call waiting to happen, so both read this one method.
    /// </para>
    /// </summary>
    List<int> ResolveRoleHolders(int workflowRoleId, WorkflowRoleGroup roleGroup, int? requesterEmployeeId);

    int? ResolveDelegatedFrom(
        WorkflowRequest request,
        RequestMaster requestMaster,
        RequestDetail requestDetail,
        int actingEmployeeId);
}
