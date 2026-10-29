using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Threading;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowApproverResolver"/>
///
/// <remarks>
/// <para>
/// Ported from legacy <c>WorkFlowServices.GetApprover</c> and <c>GetEscalateApprover</c>. Four rules apply in
/// order, and all four had configuration columns in HCMv3 that nothing read until now -- an admin could set
/// "escalate to the HR Manager role, department scope" and the engine would quietly ignore every word of it.
/// Silently-ignored configuration is worse than absent configuration, because the screen says it is on.
/// </para>
///
/// <list type="number">
/// <item>WHO holds the step's role, or sits at the requested level of the manager chain.</item>
/// <item>NARROWED to the requester's own part of the organisation, when the step says so.</item>
/// <item>MINUS anyone who already signed an earlier signatory step, when this step is a signatory step.</item>
/// <item>Falling back to the chain's default approver, so a request is never left unroutable in silence.</item>
/// <item>SUBSTITUTED, finally, for anyone with active cover in place -- see below.</item>
/// </list>
///
/// <para>
/// DELEGATION. The last thing that happens to a candidate list is <see cref="IWorkflowDelegationResolver"/>
/// swapping out anybody who has arranged cover for the person covering them. It happens LAST, and it is not
/// gated on the stand-in holding the role, the workflow role, or the permission the delegator holds: the
/// delegation row -- routed and approved in its own right -- IS the authority. Requiring the stand-in to
/// pass the role check as well would make delegation a no-op in exactly the ordinary case, since the whole
/// reason a step landed on this manager is that nobody else holds their role.
/// </para>
///
/// <para>
/// ORG SCOPE, and where this deliberately differs from legacy. Legacy carried denormalised
/// <c>OrgLevel1..OrgLevel7</c> columns on Employee and matched them pairwise, with a system setting choosing
/// between that and the older <c>WorkflowRoleGroup</c> enum. HCMv3 has neither: organisation lives in
/// <c>EmployeeOrgAssignment</c> as a dated row. So the enum is mapped onto the fields that actually exist --
/// Department to the organisation unit, Branch to the work location, CostCenter to the cost centre -- and
/// Company narrows nothing, because in HCMv3 a company IS a tenant and every query is already tenant-scoped.
/// </para>
///
/// <para>
/// ORG UNIT TYPE SCOPE (legacy <c>ManageWorkflowSettings.EnableOrgUnitTypeInWorkflow</c>). When the setting is
/// on, legacy ignored the <c>WorkflowRoleGroup</c> enum entirely and instead matched the requester and each
/// role holder on the organisation unit they sit under at the LEVEL the step names in <c>WorkflowRoleOrgId</c>
/// (an <see cref="OrganizationUnitType"/>): <c>Level0</c> or no level means everyone qualifies. Legacy read the
/// level off the seven denormalised <c>OrgLevelN</c> columns; here <see cref="IOrgHierarchyService"/> walks the
/// ABP OrganizationUnit tree to the ancestor classified at that level, which is the same fact.
/// </para>
/// </remarks>
public class WorkflowApproverResolver : HCMv3DomainServiceBase, IWorkflowApproverResolver
{
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IWorkflowDelegationResolver _delegationResolver;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IOrgHierarchyService _orgHierarchyService;
    private readonly IIocResolver _iocResolver;
    private IWorkflowTransactionHandlerResolver _handlerResolver;

    public WorkflowApproverResolver(
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IWorkflowDelegationResolver delegationResolver,
        ISysSettingsAccessor sysSettingsAccessor,
        IOrgHierarchyService orgHierarchyService,
        IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _requestDetailRepository = requestDetailRepository;
        _requestLogRepository = requestLogRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _delegationResolver = delegationResolver;
        _sysSettingsAccessor = sysSettingsAccessor;
        _orgHierarchyService = orgHierarchyService;
    }

    /// <summary>
    /// The transaction handlers, looked up on first use rather than injected. Handlers send email, email looks
    /// up approvers, and approvers are resolved here: injecting the handlers would close that loop and stop the
    /// container building any of them. Only a "Person on Request" step needs them, so most requests never pay.
    /// </summary>
    private IWorkflowTransactionHandlerResolver HandlerResolver =>
        _handlerResolver ??= _iocResolver.Resolve<IWorkflowTransactionHandlerResolver>();

    public List<int> ResolveForStep(
        WorkflowRequest request,
        RequestMaster requestMaster,
        RequestDetail requestDetail,
        int? actingEmployeeId = null)
    {
        return ResolveStep(request, requestMaster, requestDetail, actingEmployeeId).Approvers;
    }

    public int? ResolveDelegatedFrom(
        WorkflowRequest request,
        RequestMaster requestMaster,
        RequestDetail requestDetail,
        int actingEmployeeId)
    {
        // Cheap way out for the overwhelming majority of approvals: if nobody has delegated to this person
        // at all, no amount of re-resolving the step will change the answer.
        if (!_delegationResolver.HasActiveDelegationTo(actingEmployeeId))
        {
            return null;
        }

        var resolved = ResolveStep(request, requestMaster, requestDetail);

        return resolved.DelegatedFrom.TryGetValue(actingEmployeeId, out var delegatorId)
            ? delegatorId
            : (int?)null;
    }

    /// <summary>
    /// The step's approvers together with the substitutions that produced them.
    ///
    /// <para>
    /// Substitution runs BEFORE the signatory exclusion, not after. A stand-in who already signed an earlier
    /// signatory step must be excluded from this one on their own name -- the log records who actually
    /// clicked -- and doing it the other way round would let one person sign twice by covering for the
    /// second signatory.
    /// </para>
    /// <para>
    /// The default-approver fallback is substituted too. A chain's nominated last resort is a person like
    /// any other, and "the fallback is on leave" is the exact circumstance in which the fallback is being
    /// used.
    /// </para>
    /// </summary>
    private StepApprovers ResolveStep(
        WorkflowRequest request,
        RequestMaster requestMaster,
        RequestDetail requestDetail,
        int? actingEmployeeId = null)
    {
        var originalCandidates = ResolveCandidates(request, requestDetail);

        var candidates = _delegationResolver.ApplySubstitution(originalCandidates);
        var delegatedFrom = _delegationResolver.BuildSubstitutionMap(originalCandidates);

        if (requestDetail != null && requestDetail.SignatoryStep)
        {
            candidates = ExcludePreviousSignatories(
                request, requestMaster, requestDetail.Step, candidates, actingEmployeeId);
        }

        if (candidates.Count > 0)
        {
            return new StepApprovers(candidates, delegatedFrom);
        }

        // A "Person on Request" step that names nobody is skipped by the engine, not handed to the fallback:
        // a leave without a delegate has no handover for anyone to accept, least of all the chain's catch-all.
        if (requestDetail?.ApproverPerson == WorkflowApproverPersonType.PersonOnRequest)
        {
            return new StepApprovers(new List<int>(), new Dictionary<int, int>());
        }

        // Last resort, exactly as legacy: better one nominated fallback than a request nobody can act on.
        if (requestMaster?.DefaultApproverId != null)
        {
            var fallback = new List<int> { requestMaster.DefaultApproverId.Value };

            return new StepApprovers(
                _delegationResolver.ApplySubstitution(fallback),
                _delegationResolver.BuildSubstitutionMap(fallback));
        }

        return new StepApprovers(new List<int>(), new Dictionary<int, int>());
    }

    private sealed class StepApprovers
    {
        public StepApprovers(List<int> approvers, IReadOnlyDictionary<int, int> delegatedFrom)
        {
            Approvers = approvers;
            DelegatedFrom = delegatedFrom;
        }

        /// <summary>Who the step goes to, after substitution.</summary>
        public List<int> Approvers { get; }

        /// <summary>Stand-in employee id -> the person they are standing in for.</summary>
        public IReadOnlyDictionary<int, int> DelegatedFrom { get; }
    }

    public List<int> ResolveForEscalation(
        WorkflowRequest request,
        RequestDetail requestDetail,
        IReadOnlyCollection<int> currentApproverIds)
    {
        if (requestDetail?.EscalateWorkflowRoleId == null)
        {
            return new List<int>();
        }

        var candidates = RoleHolders(requestDetail.EscalateWorkflowRoleId.Value);

        candidates = NarrowToRequesterScope(
            request.EmployeeId,
            candidates,
            requestDetail.EscalateWorkflowRoleGroup,
            requestDetail.EscalateWorkflowRoleOrgId);

        // Substituted the same way an ordinary step is. An escalation that ignored cover would hand the
        // stalled request to the one category of person guaranteed not to act on it -- somebody who is away
        // -- and then start the escalation clock again from zero.
        candidates = _delegationResolver.ApplySubstitution(candidates);

        // The people who have already failed to act are precisely who this must not land on again. Applied
        // AFTER substitution so that a stand-in who is already sitting on the request is excluded under the
        // name the request actually knows them by.
        return candidates.Where(id => !currentApproverIds.Contains(id)).ToList();
    }

    private List<int> ResolveCandidates(WorkflowRequest request, RequestDetail requestDetail)
    {
        if (requestDetail == null)
        {
            return new List<int>();
        }

        if (requestDetail.ApproverPerson == WorkflowApproverPersonType.PersonOnRequest)
        {
            return PersonNamedOnRequest(request, requestDetail.RequestPersonField);
        }

        if (requestDetail.ApproverPerson == WorkflowApproverPersonType.Person)
        {
            if (!requestDetail.WorkflowRoleId.HasValue)
            {
                return new List<int>();
            }

            var holders = RoleHolders(requestDetail.WorkflowRoleId.Value);

            return NarrowToRequesterScope(
                request.EmployeeId, holders, requestDetail.WorkflowRoleGroup, requestDetail.WorkflowRoleOrgId);
        }

        var candidates = new List<int>();

        var level = ManagerLevelOf(requestDetail.ApproverPerson);
        if (level > 0 && request.EmployeeId.HasValue)
        {
            var managerId = WalkManagerChain(request.EmployeeId.Value, level);

            if (managerId.HasValue)
            {
                candidates.Add(managerId.Value);
            }
        }

        // A manager step that ALSO names a workflow role goes to both: the requester's manager and whoever
        // holds the role. The step's Approval Required setting then decides whether one of them is enough
        // (Single) or both must sign (All). Before this the role was silently ignored on a manager step,
        // although the screen showed it as set.
        if (level > 0 && requestDetail.WorkflowRoleId.HasValue)
        {
            var holders = NarrowToRequesterScope(
                request.EmployeeId,
                RoleHolders(requestDetail.WorkflowRoleId.Value),
                requestDetail.WorkflowRoleGroup,
                requestDetail.WorkflowRoleOrgId);

            candidates.AddRange(holders.Where(id => !candidates.Contains(id)));
        }

        return candidates;
    }

    /// <summary>
    /// The employee the request names in <paramref name="fieldCode"/>, asked of the type's own handler, since
    /// only it knows which column of which row that is. Nobody when the type has no handler or no such
    /// field, when the field is empty, or when it names the requester -- approving your own request is not
    /// an approval.
    ///
    /// <para>
    /// Not narrowed by org scope and not tied to a role: the requester chose this person for this request,
    /// and that choice is the whole authority of the step.
    /// </para>
    /// </summary>
    private List<int> PersonNamedOnRequest(WorkflowRequest request, string fieldCode)
    {
        // The step names a field of ONE transaction type. A chain shared with other types skips it for them.
        if (string.IsNullOrWhiteSpace(request?.RequestType)
            || !WorkflowRequestPersonFields.TryParse(fieldCode, out var fieldRequestType, out var propertyName)
            || !string.Equals(fieldRequestType, request.RequestType, StringComparison.OrdinalIgnoreCase))
        {
            return new List<int>();
        }

        var handler = HandlerResolver.Resolve(request.RequestType);
        if (handler == null || !handler.RequestPersonFields.Contains(propertyName))
        {
            return new List<int>();
        }

        var personId = AsyncHelper.RunSync(() =>
            handler.GetRequestPersonAsync(new WorkflowTransactionContext(request, request.EmployeeId ?? 0), propertyName));

        if (!personId.HasValue || personId == request.EmployeeId)
        {
            return new List<int>();
        }

        return new List<int> { personId.Value };
    }

    /// <summary>
    /// PersonManager is level 1; ManagerLevel2..7 are 2..7. Returns 0 for None, which resolves nobody and
    /// falls through to the default approver.
    /// </summary>
    private static int ManagerLevelOf(WorkflowApproverPersonType approverPerson)
    {
        switch (approverPerson)
        {
            case WorkflowApproverPersonType.PersonManager: return 1;
            case WorkflowApproverPersonType.ManagerLevel2: return 2;
            case WorkflowApproverPersonType.ManagerLevel3: return 3;
            case WorkflowApproverPersonType.ManagerLevel4: return 4;
            case WorkflowApproverPersonType.ManagerLevel5: return 5;
            case WorkflowApproverPersonType.ManagerLevel6: return 6;
            case WorkflowApproverPersonType.ManagerLevel7: return 7;
            default: return 0;
        }
    }

    /// <summary>
    /// Climbs the reporting line the requested number of steps.
    ///
    /// <para>
    /// Returns null rather than the highest reachable manager when the chain runs out. Substituting whoever
    /// happens to be at the top would route the request to somebody the configuration never nominated, and
    /// they would have no way of knowing that is why it reached them. Null falls through to the chain's
    /// default approver, which somebody did nominate.
    /// </para>
    /// <para>
    /// The visited set guards against a cycle in the reporting data (A reports to B reports to A). Without it
    /// a data-entry mistake would spin this loop until the level count ran out -- harmless here only because
    /// the count is small, so it is stopped explicitly rather than left to luck.
    /// </para>
    /// </summary>
    private int? WalkManagerChain(int employeeId, int levels)
    {
        var today = DateTime.Today;
        var visited = new HashSet<int> { employeeId };
        var currentId = employeeId;

        for (var step = 0; step < levels; step++)
        {
            var managerId = _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == currentId
                            && m.ManagerId != m.EmployeeId
                            && m.StartDate <= today
                            && m.EndDate >= today)
                .OrderByDescending(m => m.StartDate)
                .Select(m => (int?)m.ManagerId)
                .FirstOrDefault();

            if (!managerId.HasValue || !visited.Add(managerId.Value))
            {
                return null;
            }

            currentId = managerId.Value;
        }

        return currentId;
    }

    public List<int> ResolveRoleHolders(
        int workflowRoleId, WorkflowRoleGroup roleGroup, int? requesterEmployeeId, int? workflowRoleOrgId = null)
    {
        // The org-unit level is a notification group's Org Unit Level (legacy NotificationGroup.WorkflowRoleOrg).
        // A null level narrows nothing when the org-unit-type setting is on, exactly as legacy.
        return NarrowToRequesterScope(requesterEmployeeId, RoleHolders(workflowRoleId), roleGroup, workflowRoleOrgId);
    }

    private List<int> RoleHolders(int workflowRoleId)
    {
        var today = DateTime.Today;

        return _employeeWorkflowRoleRepository.GetAll()
            .Where(r => r.WorkflowRoleId == workflowRoleId
                        && r.StartDate <= today
                        && r.EndDate >= today)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Keeps only the role holders who share the requester's slice of the organisation.
    ///
    /// <para>
    /// Narrowing to nothing is treated as no narrowing. If a scope leaves no candidates -- nobody in that
    /// department holds the role, or the requester has no current assignment to compare against -- routing to
    /// the wider group is far better than the alternative, which is a request that cannot move at all. The
    /// approver can always reassign; a stuck request needs someone to notice it first.
    /// </para>
    /// </summary>
    private List<int> NarrowToRequesterScope(
        int? requesterEmployeeId,
        List<int> candidateEmployeeIds,
        WorkflowRoleGroup roleGroup,
        int? workflowRoleOrgId)
    {
        if (candidateEmployeeIds.Count == 0 || !requesterEmployeeId.HasValue)
        {
            return candidateEmployeeIds;
        }

        // Legacy: the org-unit-type setting REPLACES the role-group scope; it does not add to it.
        if (IsOrgUnitTypeScopingOn())
        {
            return NarrowToRequesterOrgLevel(requesterEmployeeId.Value, candidateEmployeeIds, workflowRoleOrgId);
        }

        if (roleGroup == WorkflowRoleGroup.None || roleGroup == WorkflowRoleGroup.Company)
        {
            return candidateEmployeeIds;
        }

        var requesterScope = CurrentScopeOf(requesterEmployeeId.Value, roleGroup);
        if (requesterScope == null)
        {
            return candidateEmployeeIds;
        }

        var narrowed = candidateEmployeeIds
            .Where(id => CurrentScopeOf(id, roleGroup) == requesterScope)
            .ToList();

        return narrowed.Count > 0 ? narrowed : candidateEmployeeIds;
    }

    /// <summary>Legacy <c>ManageWorkflowSettings.EnableOrgUnitTypeInWorkflow</c>; off when no settings row exists.</summary>
    private bool IsOrgUnitTypeScopingOn()
    {
        var settings = AsyncHelper.RunSync(() => _sysSettingsAccessor.GetWorkflowSettingsAsync());

        return settings != null && settings.EnableOrgUnitTypeInWorkflow;
    }

    /// <summary>
    /// Legacy <c>GetWorkflowRollGroupApprover</c>, org-unit-type branch: keeps the role holders whose
    /// organisation unit at the step's level is the requester's. No level, or <c>Level0</c>, keeps everyone,
    /// exactly as legacy did. Two people with NO unit at that level match each other, as legacy's pairwise
    /// column comparison did; narrowing to nothing falls back to the full list, as every other scope here does.
    /// </summary>
    private List<int> NarrowToRequesterOrgLevel(int requesterEmployeeId, List<int> candidateEmployeeIds, int? workflowRoleOrgId)
    {
        if (!workflowRoleOrgId.HasValue)
        {
            return candidateEmployeeIds;
        }

        var levelCode = _organizationUnitTypeRepository.GetAll()
            .Where(t => t.Id == workflowRoleOrgId.Value)
            .Select(t => t.Code)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(levelCode) || levelCode == OrganizationUnitType.Level0Code)
        {
            return candidateEmployeeIds;
        }

        var requesterUnit = OrgUnitAtLevel(requesterEmployeeId, levelCode);

        var narrowed = candidateEmployeeIds
            .Where(id => OrgUnitAtLevel(id, levelCode) == requesterUnit)
            .ToList();

        return narrowed.Count > 0 ? narrowed : candidateEmployeeIds;
    }

    private long? OrgUnitAtLevel(int employeeId, string levelCode) =>
        AsyncHelper.RunSync(() => _orgHierarchyService.GetOrgUnitAtLevelForEmployeeAsync(employeeId, levelCode));

    /// <summary>
    /// The employee's current value for whichever organisational dimension the scope names, or null when
    /// they hold no current assignment.
    /// </summary>
    private long? CurrentScopeOf(int employeeId, WorkflowRoleGroup roleGroup)
    {
        var today = DateTime.Today;

        var assignment = _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= today && a.EndDate >= today)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new { a.OrganizationUnitId, a.WorkLocationId, a.CostCenterId })
            .FirstOrDefault();

        if (assignment == null)
        {
            return null;
        }

        switch (roleGroup)
        {
            case WorkflowRoleGroup.Department:
                return assignment.OrganizationUnitId;
            case WorkflowRoleGroup.Branch:
                return assignment.WorkLocationId;
            case WorkflowRoleGroup.CostCenter:
                return assignment.CostCenterId;
            default:
                return null;
        }
    }

    /// <summary>
    /// Drops anyone who already signed an earlier signatory step on this same request.
    ///
    /// <para>
    /// The point of a multi-signatory chain is that several DIFFERENT people put their name to the document.
    /// One person holding two of the roles would otherwise satisfy both steps by clicking twice, which looks
    /// like two approvals in the log and is one in substance.
    /// </para>
    /// <para>
    /// The person acting right now is dropped too, whatever kind of step they acted on -- exactly as legacy
    /// dropped the logged-in user. It is also the only way to catch them: their approval of the previous step
    /// has been added to the log but not yet saved, so the log query below cannot see it.
    /// </para>
    /// </summary>
    private List<int> ExcludePreviousSignatories(
        WorkflowRequest request,
        RequestMaster requestMaster,
        int currentStep,
        List<int> candidates,
        int? actingEmployeeId)
    {
        if (requestMaster == null || candidates.Count == 0)
        {
            return candidates;
        }

        var earlierSignatorySteps = _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id && d.Step < currentStep && d.SignatoryStep)
            .Select(d => d.Step)
            .ToList();

        var alreadySigned = earlierSignatorySteps.Count == 0
            ? new List<int>()
            : _requestLogRepository.GetAll()
                .Where(l => l.WorkflowRequestId == request.Id
                            && l.ActionById.HasValue
                            && earlierSignatorySteps.Contains(l.WorkflowStep)
                            && l.ActionType == RequestActionType.Approved)
                .Select(l => l.ActionById.Value)
                .Distinct()
                .ToList();

        if (actingEmployeeId.HasValue)
        {
            alreadySigned.Add(actingEmployeeId.Value);
        }

        if (alreadySigned.Count == 0)
        {
            return candidates;
        }

        var remaining = candidates.Where(id => !alreadySigned.Contains(id)).ToList();

        // Excluding everybody would stall the request. A signatory step that cannot find a fresh signatory
        // is a configuration problem for a human to see, not a reason to route to nobody -- so the caller's
        // default-approver fallback gets a chance instead.
        return remaining;
    }
}
