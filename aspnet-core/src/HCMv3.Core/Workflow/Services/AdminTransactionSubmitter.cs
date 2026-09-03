using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Runtime.Session;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IAdminTransactionSubmitter"/>
public class AdminTransactionSubmitter : HCMv3DomainServiceBase, IAdminTransactionSubmitter
{
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _abpSession;

    public AdminTransactionSubmitter(
        IRepository<WorkflowRequest> requestRepository,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeSessionService employeeSessionService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession abpSession)
    {
        _requestRepository = requestRepository;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
        _employeeSessionService = employeeSessionService;
        _unitOfWorkManager = unitOfWorkManager;
        _abpSession = abpSession;
    }

    public Task<int> CreateAndSubmitAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int? subTypeId = null)
        where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity
    {
        // The employee the request is ABOUT, taken from the entity. HR is acting for them, so the approver
        // inbox and the employee's own request list both have to show it against them rather than against
        // whoever in HR typed it. The multi-employee overload below is the same code with that one value
        // supplied by the caller instead.
        return CreateAndSubmitForSubjectAsync(
            repository,
            entity,
            requestType,
            requestNumberPrefix,
            entity.EmployeeId,
            subTypeId);
    }

    public async Task<int> CreateAndSubmitForSubjectAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int subjectEmployeeId,
        int? subTypeId = null)
        where TEntity : class, IEntity<int>, IWorkflowEnabledEntity
    {
        var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(requestType);

        // Draft while workflow-managed, so the row is invisible to the rest of the system from the moment it
        // exists rather than from the moment it is submitted. Null with workflow off -- immediately
        // effective, exactly as before the type was routed.
        entity.WorkflowStatus = workflowEnabled ? RequestStatus.Draft : (RequestStatus?)null;

        var transactionId = await repository.InsertAndGetIdAsync(entity);

        if (!workflowEnabled)
        {
            return transactionId;
        }

        var request = new WorkflowRequest
        {
            TenantId = _abpSession.TenantId ?? 0,

            // The employee the request is ABOUT. Supplied by the caller: for a single-subject transaction it
            // is the entity's own employee (see the overload above), and for a header over many employees it
            // is the person who raised it, because there is no one person the batch is about.
            EmployeeId = subjectEmployeeId,

            RequestType = requestType,
            TargetEntityId = transactionId,
            RequestSubTypeId = subTypeId,
            RequestNumber = await GenerateRequestNumberAsync(requestNumberPrefix),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0
        };

        var requestId = await _requestRepository.InsertAndGetIdAsync(request);

        // Saved before submitting so the engine's lookups and the handler's status stamp both see rows that
        // exist.
        await _unitOfWorkManager.Current.SaveChangesAsync();

        // Falls back to the subject employee when the acting user has no employee link -- which is the
        // NORMAL case here, because admin accounts are deliberately never linked to an employee. Without the
        // fallback every HR-raised action would fail at submit for want of an acting employee id.
        var actingEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync() ?? subjectEmployeeId;

        await _workflowApprovalService.SubmitAsync(requestId, actingEmployeeId);

        return transactionId;
    }

    private async Task<string> GenerateRequestNumberAsync(string prefix)
    {
        var fullPrefix = prefix + "-" + DateTime.Today.Year + "-";

        var lastNumber = await Task.FromResult(_requestRepository.GetAll()
            .Where(r => r.RequestNumber.StartsWith(fullPrefix))
            .OrderByDescending(r => r.RequestNumber)
            .Select(r => r.RequestNumber)
            .FirstOrDefault());

        var next = 1;
        if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber.Substring(fullPrefix.Length), out var parsed))
        {
            next = parsed + 1;
        }

        return fullPrefix + next.ToString("D5");
    }
}
