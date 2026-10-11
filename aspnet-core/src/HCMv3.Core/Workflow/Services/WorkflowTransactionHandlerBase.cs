using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Base for a transaction type's workflow handler. Takes care of the one thing every handler must do
/// identically -- keeping <see cref="IWorkflowEnabledEntity.WorkflowStatus"/> on the transaction row in step
/// with the request -- so a concrete handler only writes what is genuinely specific to its type.
///
/// <para>
/// Keeping this in one place matters more than it looks. That column is what the <c>ApprovedOnly</c> filter
/// reads, so a handler that forgot to set it would leave a pending row visible to payroll and quota, which
/// is precisely the class of bug this whole design exists to make impossible. It is written here, once, and
/// a handler has to go out of its way to opt out.
/// </para>
///
/// <para>
/// A minimal handler therefore only implements <see cref="RequestType"/> and
/// <see cref="BuildSummaryAsync"/>; override the lifecycle hooks (calling <c>base</c>) to add side effects
/// such as deducting quota on approval.
/// </para>
///
/// <para>
/// <b>The amendment rule.</b> When <c>context.IsAmendment</c> is true, every lifecycle hook here leaves the
/// transaction row completely untouched except on approval, where the proposed values are applied. That
/// asymmetry is deliberate and it is the whole reason amendments are handled in the base rather than left to
/// each handler. An amendment targets a row that is already live: stamping it Open at submit would take an
/// approved leave away from payroll while somebody thinks about the change, and stamping it Rejected when the
/// change is declined would destroy a leave the employee is still entitled to. Refusing an edit must never
/// delete the thing being edited. Handlers get this right by inheriting it.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The transaction entity this handler governs, e.g. <c>EmployeeAbsence</c>.</typeparam>
public abstract class WorkflowTransactionHandlerBase<TEntity> : HCMv3DomainServiceBase, IWorkflowTransactionHandler, ITransientDependency
    where TEntity : class, IEntity<int>, IWorkflowEnabledEntity
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    protected WorkflowTransactionHandlerBase(
        IRepository<TEntity> repository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        Repository = repository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    protected IRepository<TEntity> Repository { get; }

    /// <inheritdoc/>
    public abstract string RequestType { get; }

    /// <inheritdoc/>
    public abstract Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context);

    /// <inheritdoc/>
    public virtual Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// The fallback projection: what the engine itself knows about any request, whatever its type.
    ///
    /// <para>
    /// Deliberately not empty, and deliberately not abstract. Making it abstract would have forced thirty
    /// handlers to be edited before any of them could be built, and an empty default would have given the
    /// approver a blank card -- worse than the one-line summary they had. This floor means adding the
    /// endpoint improved every type at once and each handler can then override at its own pace.
    /// </para>
    ///
    /// <para>
    /// The submission comment is left null here because it lives on the transaction row, whose shape only
    /// the concrete handler knows.
    /// </para>
    /// </summary>
    public virtual Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var request = context.Request;

        var detail = new WorkflowRequestDetail()
            .Add(WorkflowDetailField.Text(WorkflowDetailFieldNames.RequestNumber, L("RequestNumber"), request?.RequestNumber))
            .Add(WorkflowDetailField.DateTimeField(WorkflowDetailFieldNames.SubmittedOn, L("SubmittedOn"), request?.CreationTime))
            .Add(WorkflowDetailField.LongText(WorkflowDetailFieldNames.Summary, L("Summary"), CurrentSummary(request)));

        return Task.FromResult(detail);
    }

    /// <summary>
    /// The stored summary in the viewer's language. Arabic falls back to English rather than showing
    /// nothing: a handler written before the Arabic line was filled in should still describe the request.
    /// </summary>
    protected string CurrentSummary(WorkflowRequest request)
    {
        if (request == null)
        {
            return null;
        }

        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        return isArabic
            ? (string.IsNullOrWhiteSpace(request.SummaryAr) ? request.SummaryEn : request.SummaryAr)
            : request.SummaryEn;
    }

    /// <inheritdoc/>
    public virtual bool SupportsAmendment => false;

    /// <inheritdoc/>
    public virtual System.Collections.Generic.IReadOnlyCollection<WorkflowRequestAction> SupportedChangeActions =>
        SupportsAmendment
            ? new[] { WorkflowRequestAction.Amendment }
            : System.Array.Empty<WorkflowRequestAction>();

    /// <inheritdoc/>
    public virtual Task<string> BuildSnapshotAsync(WorkflowTransactionContext context)
    {
        return Task.FromResult<string>(null);
    }

    /// <inheritdoc/>
    public virtual Task ApplyAmendmentAsync(WorkflowTransactionContext context)
    {
        // Reached only if a handler declares SupportsAmendment without implementing this. Loud rather than
        // silent: an amendment that approves and changes nothing is worse than one that fails, because the
        // employee and the approver both believe the change took effect.
        throw new UserFriendlyException(L("AmendmentsAreNotSupportedForThisTransactionType", RequestType));
    }

    /// <inheritdoc/>
    public virtual Task OnSubmittedAsync(WorkflowTransactionContext context)
    {
        // An amendment leaves the row alone -- see the class remarks on the amendment rule.
        return context.IsAmendment
            ? Task.CompletedTask
            : SetWorkflowStatusAsync(context, RequestStatus.Open);
    }

    /// <inheritdoc/>
    public virtual Task OnStepActionAsync(WorkflowTransactionContext context, WorkflowStepAction action, int step)
    {
        // Nothing is tied to a step number for most types.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        // The row is already approved and live; what changes is what it says.
        return context.IsAmendment
            ? ApplyAmendmentAsync(context)
            : SetWorkflowStatusAsync(context, RequestStatus.Approved);
    }

    /// <inheritdoc/>
    public virtual Task OnRejectedAsync(WorkflowTransactionContext context)
    {
        // THE important one. Marking the row Rejected here would take an approved leave away from the
        // employee because somebody declined a change to it -- destroying a valid transaction as a side
        // effect of refusing an edit. A refused amendment simply leaves the row as it was.
        return context.IsAmendment
            ? Task.CompletedTask
            : SetWorkflowStatusAsync(context, RequestStatus.Rejected);
    }

    /// <inheritdoc/>
    public virtual Task OnSentBackAsync(WorkflowTransactionContext context)
    {
        return context.IsAmendment
            ? Task.CompletedTask
            : SetWorkflowStatusAsync(context, RequestStatus.Draft);
    }

    /// <summary>
    /// Loads the transaction row this request governs.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled for the lookup, and it has to be: while a request is in
    /// flight its row is Draft or Open, which is exactly what that filter hides. Without this a handler
    /// could never find the row it is about to approve. This is the legitimate case the filter's opt-out
    /// exists for.
    /// </para>
    /// </summary>
    protected virtual async Task<TEntity> GetTargetAsync(WorkflowTransactionContext context)
    {
        if (!context.TargetEntityId.HasValue)
        {
            return null;
        }

        var targetId = context.TargetEntityId.Value;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return await Task.FromResult(Repository.GetAll().FirstOrDefault(e => e.Id == targetId));
        }
    }

    /// <summary>
    /// Same as <see cref="GetTargetAsync"/> but throws when the row is missing, for the lifecycle points
    /// where its absence means the request is corrupt rather than merely unlinked.
    /// </summary>
    protected virtual async Task<TEntity> GetRequiredTargetAsync(WorkflowTransactionContext context)
    {
        var target = await GetTargetAsync(context);

        if (target == null)
        {
            throw new UserFriendlyException(L("WorkflowRequestHasNoTransactionRow", context.Request?.RequestNumber ?? string.Empty));
        }

        return target;
    }

    /// <summary>
    /// Writes the request's status onto the transaction row and links the two, which is what moves the row
    /// in or out of view for the rest of the system.
    /// </summary>
    protected virtual async Task SetWorkflowStatusAsync(WorkflowTransactionContext context, RequestStatus status)
    {
        var target = await GetTargetAsync(context);
        if (target == null)
        {
            // A request with no target row yet is legitimate (created before its transaction). Nothing to
            // stamp; the engine's own status remains the source of truth until the row exists.
            return;
        }

        target.WorkflowStatus = status;
        target.WorkflowRequestId = context.Request?.Id;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(target);
        }
    }
}
