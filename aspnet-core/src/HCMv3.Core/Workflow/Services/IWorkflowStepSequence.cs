namespace HCMv3.Workflow.Services;

/// <summary>
/// The one place that answers "which <see cref="RequestDetail.Step"/> does a request go to next" -- on
/// submit, on send-back, on approval, and on escalation.
///
/// <para>
/// <b>Why this exists at all.</b> A <see cref="RequestDetail"/> row whose
/// <see cref="RequestDetail.ApproverPerson"/> is <see cref="WorkflowApproverPersonType.None"/> nominates
/// NOBODY to approve. In the legacy system that is exactly what step 1 was for: it carried
/// <see cref="RequestDetail.Person"/>, the rule for who may CREATE the request, and the approver columns
/// only appeared from step 2 onward (legacy <c>RequestDetail</c> lines 133-146 hide <c>Person</c> when
/// <c>Step &gt;= 2</c> and show <c>ApproverPerson</c> from step 2). So the chain of APPROVALS does not
/// necessarily begin at step 1, and asking for "step 1" or "current + 1" is not the same question as asking
/// for "the next step that actually has an approver".
/// </para>
///
/// <para>
/// <b>And why it is not simply "start at step 2".</b> Two configurations already in production data
/// (<c>LEAVE-STD</c>, <c>MANPOWER-STD</c>) have no creator row: their step 1 IS the first approver, with
/// <c>ApproverPerson = Person</c> and a real <c>WorkflowRoleId</c>. Hard-coding 2 would send both looking
/// for a step that does not exist and strand every request raised against them. The rule is therefore
/// "the lowest step that names an approver", which is 1 for those two and 2 for a chain with a creator row,
/// with no data migration and no renumbering either way.
/// </para>
/// </summary>
public interface IWorkflowStepSequence
{
    /// <summary>
    /// The step a newly submitted (or sent-back and resubmitted) request lands on: the lowest
    /// <see cref="RequestDetail.Step"/> for this master whose <see cref="RequestDetail.ApproverPerson"/> is
    /// not <see cref="WorkflowApproverPersonType.None"/>.
    ///
    /// <para>
    /// Falls back to 1 when the master nominates no approver on any row -- which includes the perfectly
    /// ordinary case of a master with no <see cref="RequestDetail"/> rows at all, routed entirely through
    /// <see cref="RequestMaster.DefaultApprover"/>. That fallback is what keeps every configuration that
    /// predates the creator convention behaving exactly as it did.
    /// </para>
    /// </summary>
    int GetFirstApproverStep(RequestMaster requestMaster);

    /// <summary>
    /// The next step an approval moves to, or <c>null</c> when there is none and the request is therefore
    /// complete. Skips any later row that nominates no approver, and never walks past the end of the chain.
    /// </summary>
    int? GetNextApproverStep(RequestMaster requestMaster, int currentStep);

    /// <summary>
    /// The master's creator row -- the lowest step with no approver -- or <c>null</c> when it has none.
    /// Its <see cref="RequestDetail.Person"/> says who may raise this request, and for whom.
    /// </summary>
    RequestDetail GetCreatorStep(RequestMaster requestMaster);
}
