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
}
