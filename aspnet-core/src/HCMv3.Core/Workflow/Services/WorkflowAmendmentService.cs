using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowAmendmentService"/>
public class WorkflowAmendmentService : HCMv3DomainServiceBase, IWorkflowAmendmentService
{
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRequestNumberGenerator _requestNumberGenerator;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IWorkflowApprovalService _approvalService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _abpSession;

    public WorkflowAmendmentService(
        IRepository<WorkflowRequest> requestRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IWorkflowApprovalService approvalService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession abpSession,
        IRequestNumberGenerator requestNumberGenerator)
    {
        _requestNumberGenerator = requestNumberGenerator;
        _requestRepository = requestRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _handlerResolver = handlerResolver;
        _approvalService = approvalService;
        _unitOfWorkManager = unitOfWorkManager;
        _abpSession = abpSession;
    }

    public async Task<bool> IsAmendmentEnabledAsync(string requestType)
    {
        if (string.IsNullOrWhiteSpace(requestType))
        {
            return false;
        }

        var handler = _handlerResolver.Resolve(requestType);
        if (handler == null || !handler.SupportsAmendment)
        {
            return false;
        }

        var type = await _transactionTypeRepository.GetAll()
            .FirstOrDefaultAsync(t => t.Code == requestType);

        // With workflow off there is nothing to route the change through, and the ordinary edit screen is
        // the right place to change the row.
        return type != null && type.IsActive && type.IsWorkflowEnabled;
    }

    public async Task<bool> CanAmendAsync(string requestType, int targetEntityId)
    {
        if (!await IsAmendmentEnabledAsync(requestType))
        {
            return false;
        }

        return !await HasAmendmentInFlightAsync(requestType, targetEntityId);
    }

    public async Task<int> CreateAndSubmitAsync(
        string requestType,
        int targetEntityId,
        int employeeId,
        string amendmentJson,
        int actingEmployeeId,
        int? requestSubTypeId = null)
    {
        if (string.IsNullOrWhiteSpace(requestType))
        {
            throw new ArgumentException("A request type is required.", nameof(requestType));
        }

        var handler = _handlerResolver.Resolve(requestType);

        if (handler == null || !handler.SupportsAmendment)
        {
            throw new UserFriendlyException(L("AmendmentsAreNotSupportedForThisTransactionType", requestType));
        }

        // One at a time. Two amendments in flight against the same row would apply in approval order rather
        // than the order they were raised, and the second would overwrite the first without its approver
        // ever having seen the values they were actually agreeing to.
        if (await HasAmendmentInFlightAsync(requestType, targetEntityId))
        {
            throw new UserFriendlyException(L("AnAmendmentIsAlreadyAwaitingApprovalForThisRecord"));
        }

        var request = new WorkflowRequest
        {
            TenantId = _abpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            RequestType = requestType,
            TargetEntityId = targetEntityId,
            RequestSubTypeId = requestSubTypeId,
            // The same central counter every other request draws from. An amendment is a request like any
            // other, and a scheme of its own only made it look like a different kind of thing.
            RequestNumber = await _requestNumberGenerator.NextAsync("AMD"),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0,
            IsAmendment = true,
            AmendmentJson = amendmentJson
        };

        var requestId = await _requestRepository.InsertAndGetIdAsync(request);

        // Saved before the snapshot and the submit, so both see a row that exists.
        await _unitOfWorkManager.Current.SaveChangesAsync();

        var context = new WorkflowTransactionContext(request, actingEmployeeId);

        // Taken now rather than at approval: by the time an approver looks, the "before" they need to see is
        // the state at the moment the change was asked for.
        request.SnapshotJson = await handler.BuildSnapshotAsync(context);
        await _requestRepository.UpdateAsync(request);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        await _approvalService.SubmitAsync(requestId, actingEmployeeId);

        return requestId;
    }

    private async Task<bool> HasAmendmentInFlightAsync(string requestType, int targetEntityId)
    {
        return await _requestRepository.GetAll()
            .AnyAsync(r => r.IsAmendment
                           && r.RequestType == requestType
                           && r.TargetEntityId == targetEntityId
                           && (r.RequestStatus == RequestStatus.Open || r.RequestStatus == RequestStatus.Draft));
    }

}
