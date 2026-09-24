using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowEscalationService"/>
///
/// <remarks>
/// <para>
/// Ported from legacy <c>WorkFlowServices</c> (escalation) and <c>NotificationService</c> (reminders). Both
/// measure from the SAME clock: the most recent action on the request. That matters more than it looks --
/// measuring from submission instead would keep escalating a request that is moving briskly through a long
/// chain, and measuring from the step's start would need a column nothing writes.
/// </para>
///
/// <para>
/// The two modes of escalation are genuinely different and legacy had both:
/// </para>
/// <list type="bullet">
/// <item><b>Next level</b> (<c>EscalateNextLevel</c>): advance the request a step, as though the current
/// approver had said yes. Used where waiting is itself the decision -- the request must keep moving.</item>
/// <item><b>Sideways</b>: keep the step where it is and hand it to the step's nominated escalation role,
/// excluding whoever has been sitting on it. Used where the decision genuinely needs making, just by
/// somebody else.</item>
/// </list>
///
/// <para>
/// NEITHER mode approves anything. A stalled request escalates to a different person, never to a decision --
/// the whole point is that a human still has to decide, sooner. An escalation that auto-approved would turn
/// an unstaffed inbox into silent blanket approval, which is the worst possible reading of "nobody objected".
/// </para>
/// </remarks>
public class WorkflowEscalationService : HCMv3DomainServiceBase, IWorkflowEscalationService
{
    private readonly IRepository<WorkflowRequest> _workflowRequestRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApproverResolver _approverResolver;
    private readonly IWorkflowStepSequence _stepSequence;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowEscalationService(
        IRepository<WorkflowRequest> workflowRequestRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApproverResolver approverResolver,
        IWorkflowStepSequence stepSequence,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _stepSequence = stepSequence;
        _workflowRequestRepository = workflowRequestRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
        _requestLogRepository = requestLogRepository;
        _requestDetailRepository = requestDetailRepository;
        _requestMasterResolver = requestMasterResolver;
        _approverResolver = approverResolver;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<List<WorkflowEscalationResult>> EscalateStalledRequestsAsync()
    {
        var results = new List<WorkflowEscalationResult>();

        var openRequests = _workflowRequestRepository.GetAll()
            .Where(r => r.RequestStatus == RequestStatus.Open && r.WorkflowStep > 0)
            .ToList();

        foreach (var request in openRequests)
        {
            try
            {
                var result = await TryEscalateAsync(request);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                // One request's bad configuration must not stop every other request escalating. Logged,
                // never swallowed silently -- a request that cannot escalate is exactly what somebody needs
                // to be told about.
                Logger.Error(
                    $"Workflow escalation failed for request {request.Id} ({request.RequestNumber}): {ex.Message}",
                    ex);
            }
        }

        return results;
    }

    private async Task<WorkflowEscalationResult> TryEscalateAsync(WorkflowRequest request)
    {
        var requestDetail = await GetCurrentStepDetailAsync(request);

        if (requestDetail == null || !requestDetail.ShouldEscalate || requestDetail.EscalateAfterDays <= 0)
        {
            return null;
        }

        var stalledDays = DaysSinceLastAction(request);
        if (stalledDays == null || stalledDays.Value <= requestDetail.EscalateAfterDays)
        {
            return null;
        }

        var currentStep = request.WorkflowStep;

        return requestDetail.EscalateNextLevel
            ? await EscalateToNextLevelAsync(request, currentStep)
            : await EscalateSidewaysAsync(request, requestDetail, currentStep);
    }

    /// <summary>
    /// Advances the request one step, as though the stalled approver had approved -- but WITHOUT running the
    /// transaction handler.
    ///
    /// <para>
    /// That omission is deliberate and is the reason this does not simply call <c>ApproveAsync</c>. The
    /// handler is where a leave deducts quota and a loan starts recovering; firing it because a deadline
    /// passed would apply real consequences that no person agreed to. Escalation moves the request along the
    /// chain; only a human ever completes it.
    /// </para>
    /// <para>
    /// A request already on its final step cannot advance, so it is left alone rather than being approved by
    /// timeout.
    /// </para>
    /// </summary>
    private async Task<WorkflowEscalationResult> EscalateToNextLevelAsync(WorkflowRequest request, int currentStep)
    {
        var requestMaster = await _requestMasterResolver.ResolveAsync(request);

        // Same rule the approval engine uses (IWorkflowStepSequence), and it has to be: a chain whose step 1
        // is a creator row would otherwise escalate by a different reckoning than it approves by, and the
        // two would disagree about which step is last.
        var nextApproverStep = _stepSequence.GetNextApproverStep(requestMaster, currentStep);

        if (!nextApproverStep.HasValue)
        {
            // Nothing above this step to escalate to. Approving here would be approval by timeout.
            return null;
        }

        var nextStep = nextApproverStep.Value;
        var nextDetail = GetStepDetail(requestMaster.Id, nextStep);

        var newApprovers = _approverResolver.ResolveForStep(request, requestMaster, nextDetail);
        if (newApprovers.Count == 0)
        {
            throw new UserFriendlyException(L("NoApproverCouldBeResolvedForThisWorkflowStep"));
        }

        await ReplaceApproversAsync(request, newApprovers);

        request.WorkflowStep = nextStep;
        await _workflowRequestRepository.UpdateAsync(request);

        await AddEscalationLogAsync(request, newApprovers.First(), currentStep);

        return new WorkflowEscalationResult
        {
            WorkflowRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            FromStep = currentStep,
            ToStep = nextStep,
            MovedToNextLevel = true,
            NewApproverEmployeeIds = newApprovers
        };
    }

    /// <summary>
    /// Keeps the step where it is and hands it to the step's nominated escalation approvers.
    ///
    /// <para>
    /// Does nothing when nobody can be resolved -- a step configured to escalate to a role no one currently
    /// holds must leave the existing approvers in place. Clearing them would leave the request assigned to
    /// nobody at all, which is strictly worse than a slow approver.
    /// </para>
    /// </summary>
    private async Task<WorkflowEscalationResult> EscalateSidewaysAsync(
        WorkflowRequest request,
        RequestDetail requestDetail,
        int currentStep)
    {
        // The step being escalated from, not every approver the request has ever had. This list is handed
        // to the resolver to exclude, so without the filter an escalation would refuse to pick anyone who
        // approved at any earlier step -- steadily running out of candidates on a long chain.
        var currentApproverIds = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id
                        && a.WorkflowStep == currentStep
                        && a.ApproverId.HasValue)
            .Select(a => a.ApproverId.Value)
            .ToList();

        var newApprovers = _approverResolver.ResolveForEscalation(request, requestDetail, currentApproverIds);

        if (newApprovers.Count == 0)
        {
            return null;
        }

        await ReplaceApproversAsync(request, newApprovers);

        await _workflowRequestRepository.UpdateAsync(request);

        await AddEscalationLogAsync(request, newApprovers.First(), currentStep);

        return new WorkflowEscalationResult
        {
            WorkflowRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            FromStep = currentStep,
            ToStep = currentStep,
            MovedToNextLevel = false,
            NewApproverEmployeeIds = newApprovers
        };
    }

    public async Task<List<WorkflowReminderDue>> GetRemindersDueAsync()
    {
        var due = new List<WorkflowReminderDue>();

        var openRequests = _workflowRequestRepository.GetAll()
            .Where(r => r.RequestStatus == RequestStatus.Open && r.WorkflowStep > 0)
            .ToList();

        foreach (var request in openRequests)
        {
            RequestDetail requestDetail;
            try
            {
                requestDetail = await GetCurrentStepDetailAsync(request);
            }
            catch (UserFriendlyException)
            {
                // Unroutable configuration is an escalation-side problem to report; it must not stop the
                // rest of the reminders going out.
                continue;
            }

            if (requestDetail == null || requestDetail.ReminderDays <= 0)
            {
                continue;
            }

            var pendingDays = DaysSinceLastAction(request);
            if (pendingDays == null || pendingDays.Value < requestDetail.ReminderDays)
            {
                continue;
            }

            // Every ReminderDays, not every day after the first. A daily nag past day one is how people
            // learn to filter these to a folder they never open.
            if (pendingDays.Value % requestDetail.ReminderDays != 0)
            {
                continue;
            }

            // Pending ON THE CURRENT STEP. An unapproved row from an earlier step -- one of three where
            // any one sufficed -- is not somebody to chase; the request is not with them.
            var approverIds = _workFlowApproverRepository.GetAll()
                .Where(a => a.WorkflowRequestId == request.Id
                            && a.WorkflowStep == request.WorkflowStep
                            && !a.IsApproved
                            && a.ApproverId.HasValue)
                .Select(a => a.ApproverId.Value)
                .Distinct()
                .ToList();

            if (approverIds.Count == 0)
            {
                continue;
            }

            due.Add(new WorkflowReminderDue
            {
                WorkflowRequestId = request.Id,
                RequestNumber = request.RequestNumber,
                SummaryEn = request.SummaryEn,
                SummaryAr = request.SummaryAr,
                WorkflowStep = request.WorkflowStep,
                PendingDays = pendingDays.Value,
                ApproverEmployeeIds = approverIds
            });
        }

        return due;
    }

    /// <summary>
    /// Whole days since anything last happened on this request.
    ///
    /// <para>
    /// The last action, not the submission date: a request moving briskly through a seven-step chain has
    /// nobody to chase, however long ago it started. Null when there is no log at all, which means the
    /// request has not really begun and there is nothing to measure.
    /// </para>
    /// </summary>
    private int? DaysSinceLastAction(WorkflowRequest request)
    {
        var lastActionAt = _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == request.Id)
            .OrderByDescending(l => l.ActionDateTime)
            .Select(l => (DateTime?)l.ActionDateTime)
            .FirstOrDefault();

        if (!lastActionAt.HasValue)
        {
            return null;
        }

        return (DateTime.Today - lastActionAt.Value.Date).Days;
    }

    private async Task<RequestDetail> GetCurrentStepDetailAsync(WorkflowRequest request)
    {
        var requestMaster = await _requestMasterResolver.ResolveAsync(request);

        return GetStepDetail(requestMaster.Id, request.WorkflowStep);
    }

    private RequestDetail GetStepDetail(int requestMasterId, int step)
    {
        return _requestDetailRepository.GetAll()
            .FirstOrDefault(d => d.RequestMasterId == requestMasterId && d.Step == step);
    }

    /// <summary>
    /// Swaps the CURRENT step's approvers for the escalation's. Earlier steps keep theirs -- an escalation
    /// changes who the request is waiting on now, not who approved it before.
    /// </summary>
    private async Task ReplaceApproversAsync(WorkflowRequest request, List<int> approverEmployeeIds)
    {
        foreach (var superseded in _workFlowApproverRepository.GetAll()
                     .Where(a => a.WorkflowRequestId == request.Id && a.WorkflowStep == request.WorkflowStep)
                     .ToList())
        {
            await _workFlowApproverRepository.DeleteAsync(superseded);
        }

        foreach (var employeeId in approverEmployeeIds)
        {
            await _workFlowApproverRepository.InsertAsync(new WorkFlowApprover
            {
                TenantId = request.TenantId,
                WorkflowRequestId = request.Id,
                ApproverId = employeeId,
                IsApproved = false,
                WorkflowStep = request.WorkflowStep
            });
        }

        request.ApproverId = approverEmployeeIds.First();
    }

    /// <summary>
    /// Logged as <see cref="RequestActionType.Escalated"/> against the step it LEFT, so the history reads as
    /// "step 2 escalated" rather than making the new approver look like they did something.
    /// </summary>
    private async Task AddEscalationLogAsync(WorkflowRequest request, int newApproverId, int fromStep)
    {
        // Read BEFORE the row goes in, or it measures against itself and every escalation records zero.
        // This is the same count the escalation threshold was tested against a moment ago, which is the
        // point: the log should say the number that caused the escalation, not a second opinion of it.
        var stalledDays = DaysSinceLastAction(request) ?? 0;

        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = request.TenantId,
            WorkflowRequestId = request.Id,
            ActionById = newApproverId,
            RequestActionComment = L("EscalatedAutomaticallyAfterNoAction"),
            ActionDateTime = DateTime.Now,
            ActionType = RequestActionType.Escalated,
            WorkflowStep = fromStep,
            ActionDays = stalledDays,

            // Core, and correct: nobody clicked anything. A timer did this.
            Platform = WorkflowPlatform.Core
        });
    }
}
