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

    protected IRepository<TEntity> Repository => _repository;

    /// <summary>Must match this type's <c>WorkflowTransactionType.Code</c>.</summary>
    protected abstract string RequestType { get; }

    /// <summary>Short prefix for generated request numbers, e.g. "AST" giving AST-2026-00001.</summary>
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

        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            RequestType = RequestType,
            TargetEntityId = transactionId,
            RequestSubTypeId = subTypeId,
            RequestNumber = await GenerateRequestNumberAsync(),
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

    private async Task<string> GenerateRequestNumberAsync()
    {
        var prefix = RequestNumberPrefix + "-" + DateTime.Today.Year + "-";

        var lastNumber = await _requestRepository.GetAll()
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .Select(r => r.RequestNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber.Substring(prefix.Length), out var parsed))
        {
            next = parsed + 1;
        }

        return prefix + next.ToString("D5");
    }
}
