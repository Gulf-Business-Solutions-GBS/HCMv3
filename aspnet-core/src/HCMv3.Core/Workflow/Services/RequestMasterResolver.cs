using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.Workflow.BusinessRules;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IRequestMasterResolver"/>
/// <remarks>
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so this service uses the synchronous
/// <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> -- the same pattern as <c>RlsService</c> and
/// <c>EmployeeScopeProvider</c>.
/// </remarks>
[UnitOfWork]
public class RequestMasterResolver : HCMv3DomainServiceBase, IRequestMasterResolver
{
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<WorkflowGroup> _workflowGroupRepository;
    private readonly IRepository<WorkflowGroupDetail> _workflowGroupDetailRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IBusinessRuleRunner _businessRuleRunner;
    private readonly IRuleBaseObjectRegistry _ruleBaseObjectRegistry;
    private readonly IRuleEmployeeSnapshotBuilder _employeeSnapshotBuilder;
    private readonly IAbpSession _abpSession;

    public RequestMasterResolver(
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<WorkflowGroup> workflowGroupRepository,
        IRepository<WorkflowGroupDetail> workflowGroupDetailRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IBusinessRuleRunner businessRuleRunner,
        IRuleBaseObjectRegistry ruleBaseObjectRegistry,
        IRuleEmployeeSnapshotBuilder employeeSnapshotBuilder,
        IAbpSession abpSession)
    {
        _transactionTypeRepository = transactionTypeRepository;
        _workflowGroupRepository = workflowGroupRepository;
        _workflowGroupDetailRepository = workflowGroupDetailRepository;
        _requestMasterRepository = requestMasterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _businessRuleRunner = businessRuleRunner;
        _ruleBaseObjectRegistry = ruleBaseObjectRegistry;
        _employeeSnapshotBuilder = employeeSnapshotBuilder;
        _abpSession = abpSession;
    }

    public Task<WorkflowTransactionType> GetTransactionTypeAsync(string code)
    {
        if (code.IsNullOrWhiteSpace())
        {
            return Task.FromResult<WorkflowTransactionType>(null);
        }

        return Task.FromResult(_transactionTypeRepository.GetAll()
            .FirstOrDefault(t => t.Code == code));
    }

    public async Task<bool> IsWorkflowEnabledAsync(string requestType)
    {
        var transactionType = await GetTransactionTypeAsync(requestType);

        // An unregistered or retired type is not workflow-managed. Saving directly is the safe reading:
        // the alternative would be to block every save of a type nobody has catalogued yet.
        return transactionType != null && transactionType.IsActive && transactionType.IsWorkflowEnabled;
    }

    public Task<RequestMaster> ResolveAsync(WorkflowRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return ResolveAsync(request.RequestType, request.EmployeeId, request.RequestSubTypeId);
    }

    public async Task<RequestMaster> ResolveAsync(
        string requestType,
        int? employeeId,
        int? requestSubTypeId,
        int? customSubTypeId = null)
    {
        var transactionType = await GetTransactionTypeAsync(requestType);

        if (transactionType == null)
        {
            throw new UserFriendlyException(L("WorkflowTransactionTypeNotRegistered", requestType));
        }

        if (!transactionType.IsActive)
        {
            throw new UserFriendlyException(L("WorkflowTransactionTypeIsInactive", transactionType.NameEn));
        }

        if (!transactionType.IsWorkflowEnabled)
        {
            throw new UserFriendlyException(L("WorkflowIsNotEnabledForTransactionType", transactionType.NameEn));
        }

        RequestMaster requestMaster = null;

        // FIRST: ask the business rules. A tenant may have configured "if the applier is a manager, use the
        // senior chain" without a code change, and that decision has to outrank the ordinary group lookup
        // or it could never express an exception to it. A rule that matches nothing returns null here and
        // the original behaviour continues untouched, which is what makes arming one safe.
        requestMaster = await ResolveFromBusinessRulesAsync(
            requestType, employeeId, requestSubTypeId, customSubTypeId);

        if (requestMaster != null)
        {
            return requestMaster;
        }

        var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
        if (workflowSettings != null && workflowSettings.EnableWorkflowGroup)
        {
            var workflowGroupId = ResolveWorkflowGroupId(employeeId);

            if (workflowGroupId.HasValue)
            {
                requestMaster = ResolveFromWorkflowGroup(
                    workflowGroupId.Value, requestType, requestSubTypeId, customSubTypeId);
            }
        }

        // Falls back to the type's default chain. This is the catalogued replacement for legacy's compiled-in
        // "case \"LeaveRequest\": recCode = \"4\";" switch.
        if (requestMaster == null && transactionType.DefaultRequestMasterId.HasValue)
        {
            requestMaster = _requestMasterRepository.GetAll()
                .FirstOrDefault(m => m.Id == transactionType.DefaultRequestMasterId.Value);
        }

        if (requestMaster == null)
        {
            // Legacy returned null here and let the caller fail later with something unrelated. A request
            // that cannot be routed has no valid next state, so it fails at the point of misconfiguration.
            throw new UserFriendlyException(L("NoWorkflowConfigurationFoundForRequestType", transactionType.NameEn));
        }

        return requestMaster;
    }

    /// <summary>
    /// Runs the tenant's <c>OnRoute</c> rules and turns whatever chain code they name into a
    /// <see cref="RequestMaster"/>. Returns null when no rule is armed, no rule matches, or the request has
    /// no employee to describe -- all three meaning "the ordinary lookup should proceed".
    ///
    /// <para>
    /// The rule names a chain by CODE and this method resolves it, rather than letting rules hand back an
    /// id. Codes are what a configurer sees on the approval-chain screen and they survive a data refresh;
    /// ids do neither. It also means a rule naming a chain that does not exist fails HERE, with a message
    /// saying so, instead of returning null and being silently overridden by the default chain -- a routing
    /// rule that quietly does nothing is the single worst failure this feature could have.
    /// </para>
    /// </summary>
    private async Task<RequestMaster> ResolveFromBusinessRulesAsync(
        string requestType,
        int? employeeId,
        int? requestSubTypeId,
        int? customSubTypeId)
    {
        var baseObject = _ruleBaseObjectRegistry.Find(WorkflowRoutingRuleBaseObjectProvider.BaseObjectCode);

        if (baseObject == null || !employeeId.HasValue)
        {
            return null;
        }

        var today = DateTime.Today;

        var applier = await _employeeSnapshotBuilder.BuildAsync(employeeId.Value, today);

        if (applier == null)
        {
            return null;
        }

        var decision = new WorkflowRoutingDecision();

        var context = new RuleExecutionContext(baseObject, RuleEventType.OnRoute, today, _abpSession.TenantId)
            .WithParameter(
                WorkflowRoutingRuleBaseObjectProvider.RequestParameter,
                new RuleWorkflowRequestSnapshot
                {
                    RequestType = requestType,
                    RequestSubTypeId = requestSubTypeId,
                    CustomSubTypeId = customSubTypeId
                })
            .WithParameter(WorkflowRoutingRuleBaseObjectProvider.ApplierParameter, applier)
            .WithParameter(WorkflowRoutingRuleBaseObjectProvider.RoutingParameter, decision);

        await _businessRuleRunner.RunAsync(context, employeeId);

        if (!decision.IsResolved)
        {
            return null;
        }

        var requestMaster = _requestMasterRepository.GetAll()
            .FirstOrDefault(m => m.Code == decision.RequestMasterCode);

        if (requestMaster == null)
        {
            throw new UserFriendlyException(
                L("WorkflowRoutingRuleNamedUnknownChain", decision.RequestMasterCode));
        }

        return requestMaster;
    }

    /// <summary>
    /// The employee's group as of today, falling back to the tenant's default group. Mirrors legacy: read
    /// the current-dated org assignment, and if it names no group use the one flagged
    /// <see cref="WorkflowGroup.IsDefault"/>.
    /// </summary>
    private int? ResolveWorkflowGroupId(int? employeeId)
    {
        if (employeeId.HasValue)
        {
            var today = DateTime.Today;

            var assignedGroupId = _orgAssignmentRepository.GetAll()
                .Where(a => a.EmployeeId == employeeId.Value
                            && a.StartDate <= today
                            && a.EndDate >= today
                            && a.WorkflowGroupId.HasValue)
                .OrderByDescending(a => a.StartDate)
                .Select(a => a.WorkflowGroupId)
                .FirstOrDefault();

            if (assignedGroupId.HasValue)
            {
                return assignedGroupId;
            }
        }

        return _workflowGroupRepository.GetAll()
            .Where(g => g.IsDefault)
            .Select(g => (int?)g.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Three progressively looser matches within the group, most specific first. Legacy ran exactly these
    /// three in this order; the point is that a group can override the chain for one leave type without
    /// having to restate it for every other.
    /// </summary>
    private RequestMaster ResolveFromWorkflowGroup(
        int workflowGroupId,
        string requestType,
        int? requestSubTypeId,
        int? customSubTypeId)
    {
        var groupDetails = _workflowGroupDetailRepository.GetAll()
            .Where(d => d.WorkflowGroupId == workflowGroupId && d.RequestTypeName == requestType)
            .Select(d => new
            {
                d.RequestMasterId,
                d.RequestSubTypeId,
                d.CustomSubTypeId
            })
            .ToList();

        if (groupDetails.Count == 0)
        {
            return null;
        }

        int? matchedRequestMasterId = null;

        // 1. type + sub-type + custom sub-type
        if (requestSubTypeId.HasValue && customSubTypeId.HasValue)
        {
            matchedRequestMasterId = groupDetails
                .Where(d => d.RequestSubTypeId == requestSubTypeId && d.CustomSubTypeId == customSubTypeId)
                .Select(d => (int?)d.RequestMasterId)
                .FirstOrDefault();
        }

        // 2. type + sub-type
        if (!matchedRequestMasterId.HasValue && requestSubTypeId.HasValue)
        {
            matchedRequestMasterId = groupDetails
                .Where(d => d.RequestSubTypeId == requestSubTypeId)
                .Select(d => (int?)d.RequestMasterId)
                .FirstOrDefault();
        }

        // 3. type alone -- the group's catch-all for this transaction type
        if (!matchedRequestMasterId.HasValue)
        {
            matchedRequestMasterId = groupDetails
                .Where(d => !d.RequestSubTypeId.HasValue && !d.CustomSubTypeId.HasValue)
                .Select(d => (int?)d.RequestMasterId)
                .FirstOrDefault();
        }

        if (!matchedRequestMasterId.HasValue)
        {
            return null;
        }

        return _requestMasterRepository.GetAll()
            .FirstOrDefault(m => m.Id == matchedRequestMasterId.Value);
    }
}
