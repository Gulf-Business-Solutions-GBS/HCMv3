using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <summary>
/// The parts of "an employee raises a request" that are identical for every transaction type: creating the
/// row hidden, raising the request, submitting it, and withdrawing it again.
///
/// <para>
/// Worth having as a base rather than five copies because these are exactly the steps that must not drift
/// between types. If one service forgot to set <c>WorkflowStatus = Draft</c> before inserting, that row
/// would be visible to payroll for the moment between insert and submit; if another checked ownership only
/// in its list query and not on the write, one employee could withdraw another's request. Both mistakes are
/// invisible in review and obvious in production, so they are written once.
/// </para>
///
/// <para>
/// What stays with the concrete service is what genuinely differs: the fields on the form, the lookup names
/// for display, and the request-number prefix. The business rules live further down still, in each type's
/// <c>IWorkflowTransactionHandler</c>, so they apply whether the request comes from here or from HR.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The transaction entity, e.g. <c>EmployeeAsset</c>.</typeparam>
public abstract class MyTransactionAppServiceBase<TEntity> : HCMv3AppServiceBase
    where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity
{
    private readonly IRepository<TEntity> _repository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;

    protected MyTransactionAppServiceBase(
        IRepository<TEntity> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
    {
        _repository = repository;
        _requestRepository = requestRepository;
        _employeeScopeProvider = employeeScopeProvider;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
    }

    /// <summary>
    /// Injected as a property rather than through the constructor, the way ABP supplies <c>ObjectMapper</c>
    /// and <c>AbpSession</c> to every application service. Twenty concrete services inherit this base, and
    /// a constructor parameter would mean editing all twenty to hand down a collaborator not one of them
    /// mentions -- churn that reads as if something about those services had changed, when nothing has.
    /// </summary>
    public IRequestNumberGenerator RequestNumberGenerator { get; set; }

    protected IRepository<TEntity> Repository => _repository;

    /// <summary>Must match this type's <c>WorkflowTransactionType.Code</c>.</summary>
    protected abstract string RequestType { get; }

    /// <summary>
    /// Short type tag put in front of the generated request number, e.g. "AST" giving AST-000042. It is a
    /// display hint only -- the digits come from one counter shared by every request type in the tenant, so
    /// two types cannot end up with the same number even if given the same tag.
    /// </summary>
    protected abstract string RequestNumberPrefix { get; }

    protected Task<int> GetMyEmployeeIdAsync() => _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

    /// <summary>
    /// Creates the row and, when the type is workflow-enabled, submits it for approval in the same call.
    ///
    /// <para>
    /// One step on purpose: an employee should never be left holding a draft they have to remember to send.
    /// Because it is a single unit of work, a handler that refuses the request at submit rolls the
    /// half-created row back with it, rather than leaving an orphan the employee cannot see or delete.
    /// </para>
    /// </summary>
    /// <param name="build">
    /// Builds the entity for the resolved employee. A factory rather than a ready-made entity because
    /// <see cref="IHasEmployeeId.EmployeeId"/> is get-only by design -- scoping only ever reads it -- so the
    /// employee has to be set where the concrete type is known. Passing it in also means a concrete service
    /// cannot accidentally attribute a request to whoever the form said.
    /// </param>
    /// <param name="subTypeId">Absence type, loan type and so on -- what the routing rules match against.</param>
    protected async Task<MyRequestSubmissionDto> CreateAndSubmitAsync(Func<int, TEntity> build, int? subTypeId)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        var entity = build(employeeId);

        var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(RequestType);

        // Draft while workflow-managed, so the row is invisible to payroll and reports from the moment it
        // exists rather than from the moment it is submitted. With workflow off it is left null, i.e.
        // immediately effective -- the pre-workflow behaviour, which is what makes switching workflow off
        // for a type a real option rather than a half-working one.
        entity.WorkflowStatus = workflowEnabled ? RequestStatus.Draft : (RequestStatus?)null;

        var transactionId = await _repository.InsertAndGetIdAsync(entity);

        if (!workflowEnabled)
        {
            return new MyRequestSubmissionDto
            {
                TransactionId = transactionId,
                SubmittedForApproval = false
            };
        }

        return await RaiseAndSubmitAsync(entity, transactionId, employeeId, subTypeId);
    }

    /// <summary>
    /// Files the row without sending it anywhere: the employee's own unfinished work, which they can come
    /// back to, change, and submit when they are ready.
    ///
    /// <para>
    /// <b>No <c>WorkflowRequest</c> is created.</b> A draft has no number, no approver and no step, so a
    /// request row for one would be three columns of nothing plus a number burned out of the shared
    /// sequence for something that may never be sent. Worse, it would be a row in <c>WorkflowRequests</c>
    /// that the inbox, the activity log and every "requests I have raised" query would have to learn to
    /// exclude -- each of them a place to forget. With no request row there is nothing to exclude: a draft
    /// is a transaction row wearing <c>WorkflowStatus = Draft</c>, which the <c>ApprovedOnly</c> filter
    /// already hides from payroll, quota and reports, and which no approver-side query can reach because
    /// approver-side queries all start from <c>WorkflowRequests</c>.
    /// </para>
    ///
    /// <para>
    /// Drafts are marked <see cref="RequestStatus.Draft"/> even when the type has workflow switched off.
    /// Saving a draft is a statement about this row, not about the type's configuration, and a draft left
    /// effective would be a half-finished record that payroll could act on. It becomes effective -- status
    /// null -- at submit, which is where the type's configuration is read.
    /// </para>
    /// </summary>
    protected async Task<MyRequestSubmissionDto> SaveDraftAsync(Func<int, TEntity> build)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        var entity = build(employeeId);

        entity.WorkflowStatus = RequestStatus.Draft;
        entity.WorkflowRequestId = null;

        var transactionId = await _repository.InsertAndGetIdAsync(entity);

        return new MyRequestSubmissionDto
        {
            TransactionId = transactionId,
            SubmittedForApproval = false,
            SavedAsDraft = true
        };
    }

    /// <summary>
    /// Fetches one of the employee's own drafts so the form can be reopened on it.
    ///
    /// <para>
    /// Returns the entity rather than a DTO because only the concrete service knows what its form needs;
    /// the ownership and still-a-draft checks are what belong here, and they are the parts that must not be
    /// reimplemented once per type.
    /// </para>
    /// </summary>
    protected async Task<TEntity> GetMyDraftAsync(int id)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return await LoadOwnDraftAsync(id, employeeId);
        }
    }

    /// <summary>
    /// Applies the form's changes to an existing draft. Still a draft afterwards -- editing is not
    /// submitting.
    /// </summary>
    protected async Task UpdateDraftAsync(int id, Action<TEntity> apply)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await LoadOwnDraftAsync(id, employeeId);

            apply(entity);

            // Re-stamped after the caller's changes, not before: a form that binds the whole entity could
            // otherwise post a status back and quietly promote its own draft.
            entity.WorkflowStatus = RequestStatus.Draft;
            entity.WorkflowRequestId = null;

            await _repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// Sends a saved draft into the approval chain -- the point at which it finally gets a request number
    /// and an approver, and the same code path a one-step create takes.
    /// </summary>
    protected async Task<MyRequestSubmissionDto> SubmitDraftAsync(int id, int? subTypeId)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await LoadOwnDraftAsync(id, employeeId);

            var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(RequestType);

            if (!workflowEnabled)
            {
                // Workflow is off for this type, so there is nobody to send it to and nothing to wait for:
                // submitting means making it effective, which is status null -- the same state a row
                // created directly would have had.
                entity.WorkflowStatus = null;
                await _repository.UpdateAsync(entity);

                return new MyRequestSubmissionDto
                {
                    TransactionId = entity.Id,
                    SubmittedForApproval = false
                };
            }

            return await RaiseAndSubmitAsync(entity, entity.Id, employeeId, subTypeId);
        }
    }

    /// <summary>
    /// Raises the <c>WorkflowRequest</c> over an already-inserted transaction row and submits it. Shared by
    /// the one-step create and the submit-a-draft path so the two cannot drift -- if they did, a request
    /// would route differently depending on whether the employee had saved it first, which is exactly the
    /// kind of difference nobody would think to test for.
    /// </summary>
    private async Task<MyRequestSubmissionDto> RaiseAndSubmitAsync(
        TEntity entity,
        int transactionId,
        int employeeId,
        int? subTypeId)
    {
        entity.WorkflowStatus = RequestStatus.Draft;

        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            RequestType = RequestType,
            TargetEntityId = transactionId,
            RequestSubTypeId = subTypeId,

            // Claimed here and nowhere earlier. A draft that is never sent costs no number, and a number
            // once issued belongs to exactly one request -- the generator increments the tenant's counter in
            // the database rather than reading the highest number and adding one.
            RequestNumber = await RequestNumberGenerator.NextAsync(RequestNumberPrefix),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0
        };

        var requestId = await _requestRepository.InsertAndGetIdAsync(request);

        // Saved before submitting so the engine's own lookups and the handler's status stamp both see rows
        // that actually exist.
        await CurrentUnitOfWork.SaveChangesAsync();

        await _workflowApprovalService.SubmitAsync(requestId, employeeId);

        return new MyRequestSubmissionDto
        {
            TransactionId = transactionId,
            WorkflowRequestId = requestId,
            SubmittedForApproval = true
        };
    }

    /// <summary>
    /// The three checks every draft operation needs, in the order that gives the most honest answer: does
    /// it exist, is it theirs, and is it still a draft.
    ///
    /// <para>
    /// Ownership is checked here rather than relying on a scoped list query, for the same reason
    /// <see cref="CancelAsync"/> checks it: these are writes, and a scoped list paired with an unscoped
    /// write is how one employee ends up editing another's request.
    /// </para>
    /// </summary>
    private async Task<TEntity> LoadOwnDraftAsync(int id, int employeeId)
    {
        var entity = await _repository.FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
        {
            throw new UserFriendlyException(L("RequestNotFound"));
        }

        if (entity.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyEditYourOwnRequests"));
        }

        if (!IsDraft(entity))
        {
            // Already in an approver's hands, or already effective. Either way the form is no longer the
            // way to change it -- that is an amendment, which is a different transaction entirely.
            throw new UserFriendlyException(L("ThisRequestIsNoLongerADraft"));
        }

        return entity;
    }

    /// <summary>
    /// A saved draft: still marked Draft AND carrying no workflow request. Both halves matter. The status
    /// alone is briefly true of a row mid-submission, and a request id alone would not tell Draft from
    /// Rejected -- so only the pair means "the employee still has this".
    /// </summary>
    protected static bool IsDraft(IWorkflowEnabledEntity entity)
    {
        return entity.WorkflowStatus == RequestStatus.Draft && entity.WorkflowRequestId == null;
    }

    /// <summary>
    /// The employee's own rows. The <c>ApprovedOnly</c> filter is disabled because seeing their own
    /// in-flight requests is the main reason they opened the page; it stays scoped to them by employee id
    /// regardless, which is the check that actually matters.
    /// </summary>
    protected IQueryable<TEntity> MyRowsQuery(int employeeId)
    {
        return _repository.GetAll().Where(e => e.EmployeeId == employeeId);
    }

    /// <summary>
    /// Withdraws a request while it is still the employee's to withdraw, closing the workflow request
    /// alongside it so nothing is left waiting in an approver's inbox for a row that no longer exists.
    /// </summary>
    protected async Task CancelAsync(int id)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.FirstOrDefaultAsync(e => e.Id == id);

            if (entity == null)
            {
                throw new UserFriendlyException(L("RequestNotFound"));
            }

            // Checked here as well as by the scoped list query, because this is a write: a scoped list
            // paired with an unscoped write is how one employee ends up withdrawing another's request.
            if (entity.EmployeeId != employeeId)
            {
                throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
            }

            if (!CanCancel(entity))
            {
                // Once approved it is a real transaction others may have acted on; changing it is an
                // amendment, not a withdrawal.
                throw new UserFriendlyException(L("ThisRequestCanNoLongerBeCancelled"));
            }

            if (entity.WorkflowRequestId.HasValue)
            {
                var request = await _requestRepository.FirstOrDefaultAsync(r => r.Id == entity.WorkflowRequestId.Value);
                if (request != null)
                {
                    request.RequestStatus = RequestStatus.Closed;
                    await _requestRepository.UpdateAsync(request);
                }
            }

            await _repository.DeleteAsync(entity);
        }
    }

    /// <summary>
    /// Only while the request is still undecided. A row with no workflow status was entered by HR rather
    /// than requested, so it is not the employee's to withdraw either.
    /// </summary>
    protected static bool CanCancel(IWorkflowEnabledEntity entity)
    {
        return entity.WorkflowStatus == RequestStatus.Draft || entity.WorkflowStatus == RequestStatus.Open;
    }

}
