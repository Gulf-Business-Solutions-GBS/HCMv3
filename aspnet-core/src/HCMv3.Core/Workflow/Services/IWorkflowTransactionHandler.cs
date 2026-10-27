using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Workflow.Services;

/// <summary>
/// The per-transaction-type half of workflow: validation before submit, the inbox label, and the side
/// effects that must happen exactly once when a request is approved or rejected.
///
/// <para>
/// This is the contract that keeps <see cref="WorkflowApprovalService"/> type-agnostic. In the legacy system
/// the equivalent logic lived inside <c>RequestProcessApproval</c> as ~2000 lines of
/// <c>if (request.RequestType == "AbsenceRequest") { ... } else if (...)</c>, one branch per concrete request
/// class, so the engine had to know about leave, loans, travel and thirty other features. Here the engine
/// knows only this interface, and each feature module owns its own handler.
/// </para>
///
/// <para><b>Registration.</b> Implementations are registered conventionally by ABP/Castle and injected as
/// <c>IEnumerable&lt;IWorkflowTransactionHandler&gt;</c> into
/// <see cref="IWorkflowTransactionHandlerResolver"/>. Adding a transaction type needs a handler, a catalog
/// row and a form -- no engine change. Mark implementations <c>ITransientDependency</c>; deriving from
/// <see cref="WorkflowTransactionHandlerBase{TEntity}"/> handles that and the status bookkeeping.</para>
///
/// <para><b>Name the class <c>{Something}WorkflowTransactionHandler</c>.</b> This is not cosmetic. ABP/Castle
/// binds a conventionally-registered class to its "default interfaces", which means interfaces whose name
/// matches the class name by suffix. <c>LeaveWorkflowHandler</c> does NOT match
/// <c>IWorkflowTransactionHandler</c>, so it registers only as itself and the resolver never sees it --
/// caught during this wave by the registration test below, after the handler had already been written under
/// the shorter name. The wage-type calculators follow the same rule for the same reason
/// (<c>AverageBonusWageTypeCalculator</c> : <c>IWageTypeCalculator</c>).</para>
///
/// <para><b>Registration failure is silent</b> -- the same trap documented on <c>IWageTypeCalculator</c>. If
/// conventional registration misses an implementation the injected collection is simply empty, the resolver
/// finds no handler, and requests route with no validation and no side effects at all. That is why
/// <c>WorkflowTransactionHandlerRegistration_Tests</c> asserts every implementation in the Core assembly
/// actually resolves from the container. Keep it green.</para>
/// </summary>
public interface IWorkflowTransactionHandler : IDomainService
{
    /// <summary>
    /// The catalog code this handler serves, matching <c>WorkflowTransactionType.Code</c> and
    /// <see cref="WorkflowRequest.RequestType"/>. Must be unique across handlers -- the resolver rejects
    /// duplicates rather than silently picking one.
    /// </summary>
    string RequestType { get; }

    /// <summary>
    /// Last chance to refuse a submission: quota, overlap, eligibility. Throw
    /// <c>UserFriendlyException</c> to reject. Runs before any approver is assigned, so a rejection here
    /// costs nobody an approval round-trip.
    /// </summary>
    Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context);

    /// <summary>Builds the inbox label for this request. See <see cref="WorkflowSummary"/>.</summary>
    Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context);

    /// <summary>
    /// Projects the transaction row as localized label/value pairs, so an approver reads the same fields the
    /// requester filled in. See <see cref="WorkflowRequestDetail"/> for why this exists alongside the
    /// summary.
    ///
    /// <para>
    /// <see cref="WorkflowTransactionHandlerBase{TEntity}"/> supplies a default that is never empty -- the
    /// summary, the request number and the submitted date -- so a type whose handler has not projected its
    /// own fields yet degrades to what the inbox already showed rather than to a blank card. Overriding it
    /// is how a type stops degrading.
    /// </para>
    /// </summary>
    Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context);

    /// <summary>
    /// The request completed its final approval. This is where the transaction becomes real: the handler
    /// marks the row approved and applies whatever must happen exactly once -- deducting leave quota,
    /// posting a payroll entry.
    ///
    /// <para>
    /// Called once per request, by the engine, inside the approving unit of work. If it throws, the approval
    /// is rolled back with it, which is the intended behaviour: a leave that could not deduct quota must not
    /// be recorded as approved.
    /// </para>
    /// </summary>
    Task OnApprovedAsync(WorkflowTransactionContext context);

    /// <summary>
    /// The request was rejected. The handler marks the row rejected, which keeps it invisible to the rest of
    /// the system under the <c>ApprovedOnly</c> filter, and reverses anything reserved at submit.
    /// </summary>
    Task OnRejectedAsync(WorkflowTransactionContext context);

    /// <summary>
    /// The request was sent back to its submitter for correction, returning the row to draft. Separate from
    /// rejection because the request is still alive and may be resubmitted.
    /// </summary>
    Task OnSentBackAsync(WorkflowTransactionContext context);

    /// <summary>The request was submitted into the approval chain; the row is no longer a draft.</summary>
    Task OnSubmittedAsync(WorkflowTransactionContext context);

    /// <summary>
    /// Somebody acted on the request at a step: the requester submitted it, or an approver approved it.
    ///
    /// <para>
    /// The per-step counterpart to <see cref="OnApprovedAsync"/>, which runs only on the FINAL approval. It
    /// exists for the rare rule that is tied to a step number rather than to the outcome -- legacy's
    /// "raise a passport withdrawal when the leave request is at step N" is the one that needed it.
    /// </para>
    ///
    /// <para>
    /// <b>Advisory, and it must stay that way.</b> The engine calls this after the action has been recorded
    /// and swallows anything it throws: a step observer can never refuse or roll back the action it is
    /// observing. For the same reason an implementation must not write through the current unit of work --
    /// a write that fails there would poison the approval it is riding on. Read, decide, and defer any
    /// writing until the action has committed. <see cref="WorkflowTransactionHandlerBase{TEntity}"/> supplies
    /// a no-op, so a handler with no such rule does nothing.
    /// </para>
    /// </summary>
    /// <param name="context">The request and who acted.</param>
    /// <param name="action">Whether this was a submission or an approval.</param>
    /// <param name="step">
    /// The step the action was taken AT, i.e. the step being left. For an approval it is the step the
    /// request sat at when the approver approved, whether or not that approval completed the step or the
    /// chain. For a submission it is the chain's creator step (the row naming who may raise the request), or
    /// 0 when the chain has none.
    /// </param>
    Task OnStepActionAsync(WorkflowTransactionContext context, WorkflowStepAction action, int step);

    /// <summary>
    /// Approvers have just been assigned to a step: on submission (the first approver step) and each time
    /// an approval moves the request on to the next step.
    ///
    /// <para>
    /// Exists for the 360 appraisal, whose legacy reviewers were added at exactly this moment
    /// (<c>WorkFlowServices.OperateApproverInfo</c>) -- not when the step is approved, which is what
    /// <see cref="OnStepActionAsync"/> reports. A send-back adds nothing here: it re-assigns no step until
    /// the request is resubmitted, and whatever was added before stays, as in legacy.
    /// </para>
    ///
    /// <para>
    /// <b>Unlike <see cref="OnStepActionAsync"/>, this one is part of the action.</b> It runs inside the same
    /// unit of work, after the approver rows are written, and may write; if it throws, the submission or
    /// approval rolls back with it. That is what legacy did, and it is the only safe reading for a rule that
    /// changes who scores an appraisal: a step must not open with half its reviewers.
    /// </para>
    /// </summary>
    /// <param name="context">The request and who acted.</param>
    /// <param name="step">The step the approvers were assigned to.</param>
    /// <param name="approverEmployeeIds">The employees now approving that step, in assignment order.</param>
    Task OnApproversAssignedAsync(
        WorkflowTransactionContext context,
        int step,
        System.Collections.Generic.IReadOnlyList<int> approverEmployeeIds);

    /// <summary>
    /// Whether this transaction type can be CHANGED through workflow after it has already been approved.
    ///
    /// <para>
    /// False by default, and that is the honest default rather than a cautious one: applying an amendment
    /// means writing specific fields onto a live row, which only the type's own handler can do correctly.
    /// A type that has not implemented <see cref="ApplyAmendmentAsync"/> refuses the amendment when it is
    /// raised, rather than accepting it and doing nothing when it is approved.
    /// </para>
    /// </summary>
    bool SupportsAmendment { get; }

    /// <summary>
    /// Which changes can be raised against an approved transaction of this type: an amendment, a
    /// cancellation, a return. Most types support none, and a few only one -- a passport withdrawal can be
    /// returned but not amended or cancelled. Setup screens read this so that a chain "for returns" is only
    /// offered on a type that has returns.
    /// </summary>
    System.Collections.Generic.IReadOnlyCollection<WorkflowRequestAction> SupportedChangeActions { get; }

    /// <summary>
    /// Captures the transaction row as it currently stands, for the approver to compare against what is
    /// being proposed. Returns null when there is nothing to snapshot.
    /// </summary>
    Task<string> BuildSnapshotAsync(WorkflowTransactionContext context);

    /// <summary>
    /// Writes an approved amendment's proposed values onto the transaction row.
    ///
    /// <para>
    /// Called instead of the usual approval bookkeeping, inside the approving unit of work, once. The row is
    /// already live and already Approved -- this changes what it says, not whether it counts.
    /// </para>
    /// </summary>
    Task ApplyAmendmentAsync(WorkflowTransactionContext context);
}

/// <summary>What was done at a step, for <see cref="IWorkflowTransactionHandler.OnStepActionAsync"/>.</summary>
public enum WorkflowStepAction
{
    /// <summary>The requester submitted (or resubmitted) the request.</summary>
    Submitted = 0,

    /// <summary>An approver approved at the step.</summary>
    Approved = 1
}
