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

    /// <inheritdoc/>
    public virtual Task OnSubmittedAsync(WorkflowTransactionContext context)
    {
        return SetWorkflowStatusAsync(context, RequestStatus.Open);
    }

    /// <inheritdoc/>
    public virtual Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        return SetWorkflowStatusAsync(context, RequestStatus.Approved);
    }

    /// <inheritdoc/>
    public virtual Task OnRejectedAsync(WorkflowTransactionContext context)
    {
        return SetWorkflowStatusAsync(context, RequestStatus.Rejected);
    }

    /// <inheritdoc/>
    public virtual Task OnSentBackAsync(WorkflowTransactionContext context)
    {
        return SetWorkflowStatusAsync(context, RequestStatus.Draft);
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
