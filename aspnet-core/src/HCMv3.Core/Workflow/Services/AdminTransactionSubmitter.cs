using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Runtime.Session;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Authorization;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
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
    private readonly IRequestNumberGenerator _requestNumberGenerator;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IPermissionChecker _permissionChecker;

    public AdminTransactionSubmitter(
        IRepository<WorkflowRequest> requestRepository,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeSessionService employeeSessionService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession abpSession,
        IRequestNumberGenerator requestNumberGenerator,
        IRepository<RequestLog> requestLogRepository,
        IPermissionChecker permissionChecker)
    {
        _requestRepository = requestRepository;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
        _employeeSessionService = employeeSessionService;
        _unitOfWorkManager = unitOfWorkManager;
        _abpSession = abpSession;
        _requestNumberGenerator = requestNumberGenerator;
        _requestLogRepository = requestLogRepository;
        _permissionChecker = permissionChecker;
    }

    public Task<int> CreateAndSubmitAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int? subTypeId = null,
        bool recordDirectly = false)
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
            subTypeId,
            recordDirectly);
    }

    public async Task<int> CreateAndSubmitForSubjectAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int subjectEmployeeId,
        int? subTypeId = null,
        bool recordDirectly = false)
        where TEntity : class, IEntity<int>, IWorkflowEnabledEntity
    {
        var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(requestType);

        if (recordDirectly && workflowEnabled)
        {
            return await RecordDirectlyAsync(
                repository, entity, requestType, requestNumberPrefix, subjectEmployeeId, subTypeId);
        }

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
            RequestNumber = await _requestNumberGenerator.NextAsync(requestNumberPrefix),
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

    /// <summary>
    /// Records a workflow-enabled transaction without sending it for approval.
    ///
    /// <para>
    /// Two gates, both checked here rather than in the screen so they hold for every caller. The TYPE must
    /// permit a bypass -- some transactions should never skip however senior the person entering them --
    /// and the USER must hold <c>RecordWithoutApproval</c>. Failing either is an error, not a quiet
    /// fallback to routing: a caller that asked to bypass and silently did not would leave HR believing a
    /// record was effective when it was sitting in somebody's inbox.
    /// </para>
    ///
    /// <para>
    /// A closed request and one log row are still written. The row is effective immediately, which is the
    /// point, but "who decided this did not need approval" has to survive -- a transaction with no workflow
    /// trace at all is indistinguishable from one that was approved. The action is its own type rather than
    /// Approved, because nobody approved it.
    /// </para>
    /// </summary>
    private async Task<int> RecordDirectlyAsync<TEntity>(
        IRepository<TEntity> repository,
        TEntity entity,
        string requestType,
        string requestNumberPrefix,
        int subjectEmployeeId,
        int? subTypeId)
        where TEntity : class, IEntity<int>, IWorkflowEnabledEntity
    {
        var transactionType = await _requestMasterResolver.GetTransactionTypeAsync(requestType);

        if (transactionType == null || !transactionType.AllowDirectEntry)
        {
            throw new UserFriendlyException(L("ThisTransactionTypeCannotBeRecordedWithoutApproval"));
        }

        if (!await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_Workflow_RecordWithoutApproval))
        {
            throw new AbpAuthorizationException(L("YouAreNotPermittedToRecordWithoutApproval"));
        }

        // Closed, not Approved: the row is final and visible to every ordinary query, and nothing is left
        // for anybody to act on. Approved would say a decision was taken.
        entity.WorkflowStatus = RequestStatus.Closed;

        var transactionId = await repository.InsertAndGetIdAsync(entity);

        var request = new WorkflowRequest
        {
            TenantId = _abpSession.TenantId ?? 0,
            EmployeeId = subjectEmployeeId,
            RequestType = requestType,
            TargetEntityId = transactionId,
            RequestSubTypeId = subTypeId,

            // No request number, deliberately. The central counter numbers REQUESTS -- things somebody
            // asked for and somebody else has to answer. This was recorded, not requested, so taking a
            // number would put a gap in the sequence and give a reference to something nobody can chase.
            RequestNumber = null,

            RequestStatus = RequestStatus.Closed,

            // Never entered the chain, so it never reached a step.
            WorkflowStep = 0
        };

        var requestId = await _requestRepository.InsertAndGetIdAsync(request);

        await _unitOfWorkManager.Current.SaveChangesAsync();

        entity.WorkflowRequestId = requestId;

        // Same fallback as the routed path: admin accounts are deliberately unlinked, so the subject stands
        // in when the person acting has no employee row of their own.
        var actingEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync() ?? subjectEmployeeId;

        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = _abpSession.TenantId ?? 0,
            WorkflowRequestId = requestId,
            ActionById = actingEmployeeId,
            ActionType = RequestActionType.RecordedDirectly,
            ActionDateTime = Clock.Now,
            WorkflowStep = 0
        });

        return transactionId;
    }

}
