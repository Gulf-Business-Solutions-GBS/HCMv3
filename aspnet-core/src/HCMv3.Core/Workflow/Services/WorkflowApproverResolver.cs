using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

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
/// </list>
///
/// <para>
/// ORG SCOPE, and where this deliberately differs from legacy. Legacy carried denormalised
/// <c>OrgLevel1..OrgLevel7</c> columns on Employee and matched them pairwise, with a system setting choosing
/// between that and the older <c>WorkflowRoleGroup</c> enum. HCMv3 has neither: organisation lives in
/// <c>EmployeeOrgAssignment</c> as a dated row. So the enum is mapped onto the fields that actually exist --
/// Department to the organisation unit, Branch to the work location, CostCenter to the cost centre -- and
/// Company narrows nothing, because in HCMv3 a company IS a tenant and every query is already tenant-scoped.
/// The <c>WorkflowRoleOrgId</c> column (an OrganizationUnitType) stays unread: it names a LEVEL in a
/// hierarchy that HCMv3 models as an ABP OrganizationUnit tree, and guessing at a mapping would be worse than
/// leaving one obviously absent.
/// </para>
/// </remarks>
public class WorkflowApproverResolver : HCMv3DomainServiceBase, IWorkflowApproverResolver
{
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;

    public WorkflowApproverResolver(
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RequestLog> requestLogRepository)
    {
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _requestDetailRepository = requestDetailRepository;
        _requestLogRepository = requestLogRepository;
    }

    public List<int> ResolveForStep(WorkflowRequest request, RequestMaster requestMaster, RequestDetail requestDetail)
    {
        var candidates = ResolveCandidates(request, requestDetail);

        if (requestDetail != null && requestDetail.SignatoryStep)
        {
            candidates = ExcludePreviousSignatories(request, requestMaster, requestDetail.Step, candidates);
        }

        if (candidates.Count > 0)
        {
            return candidates;
        }

        // Last resort, exactly as legacy: better one nominated fallback than a request nobody can act on.
        if (requestMaster?.DefaultApproverId != null)
        {
            return new List<int> { requestMaster.DefaultApproverId.Value };
        }

        return new List<int>();
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
            request,
            candidates,
            requestDetail.EscalateWorkflowRoleGroup);

        // The people who have already failed to act are precisely who this must not land on again.
        return candidates.Where(id => !currentApproverIds.Contains(id)).ToList();
    }

    private List<int> ResolveCandidates(WorkflowRequest request, RequestDetail requestDetail)
    {
        if (requestDetail == null)
        {
            return new List<int>();
        }

        if (requestDetail.ApproverPerson == WorkflowApproverPersonType.Person)
        {
            if (!requestDetail.WorkflowRoleId.HasValue)
            {
                return new List<int>();
            }

            var holders = RoleHolders(requestDetail.WorkflowRoleId.Value);

            return NarrowToRequesterScope(request, holders, requestDetail.WorkflowRoleGroup);
        }

        var level = ManagerLevelOf(requestDetail.ApproverPerson);
        if (level > 0 && request.EmployeeId.HasValue)
        {
            var managerId = WalkManagerChain(request.EmployeeId.Value, level);

            if (managerId.HasValue)
            {
                return new List<int> { managerId.Value };
            }
        }

        return new List<int>();
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
        WorkflowRequest request,
        List<int> candidateEmployeeIds,
        WorkflowRoleGroup roleGroup)
    {
        if (roleGroup == WorkflowRoleGroup.None
            || roleGroup == WorkflowRoleGroup.Company
            || candidateEmployeeIds.Count == 0
            || !request.EmployeeId.HasValue)
        {
            return candidateEmployeeIds;
        }

        var requesterScope = CurrentScopeOf(request.EmployeeId.Value, roleGroup);
        if (requesterScope == null)
        {
            return candidateEmployeeIds;
        }

        var narrowed = candidateEmployeeIds
            .Where(id => CurrentScopeOf(id, roleGroup) == requesterScope)
            .ToList();

        return narrowed.Count > 0 ? narrowed : candidateEmployeeIds;
    }

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
    /// </summary>
    private List<int> ExcludePreviousSignatories(
        WorkflowRequest request,
        RequestMaster requestMaster,
        int currentStep,
        List<int> candidates)
    {
        if (requestMaster == null || candidates.Count == 0)
        {
            return candidates;
        }

        var earlierSignatorySteps = _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id && d.Step < currentStep && d.SignatoryStep)
            .Select(d => d.Step)
            .ToList();

        if (earlierSignatorySteps.Count == 0)
        {
            return candidates;
        }

        var alreadySigned = _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == request.Id
                        && l.ActionById.HasValue
                        && earlierSignatorySteps.Contains(l.WorkflowStep)
                        && l.ActionType == RequestActionType.Approved)
            .Select(l => l.ActionById.Value)
            .Distinct()
            .ToList();

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
