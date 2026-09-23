using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowApprovalService"/>
[UnitOfWork]
public class WorkflowApprovalService : HCMv3DomainServiceBase, IWorkflowApprovalService
{
    private readonly IRepository<WorkflowRequest> _workflowRequestRepository;
    private readonly IRepository<WorkFlowApprover> _workFlowApproverRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<WFReadLog> _readLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IWorkflowApproverResolver _approverResolver;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IWorkflowNotificationDispatcher _notificationDispatcher;

    public WorkflowApprovalService(
        IRepository<WorkflowRequest> workflowRequestRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<WFReadLog> readLogRepository,
        IRepository<Employee> employeeRepository,
        IWorkflowApproverResolver approverResolver,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IWorkflowNotificationDispatcher notificationDispatcher)
    {
        _notificationDispatcher = notificationDispatcher;
        _workflowRequestRepository = workflowRequestRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
        _requestLogRepository = requestLogRepository;
        _requestDetailRepository = requestDetailRepository;
        _readLogRepository = readLogRepository;
        _employeeRepository = employeeRepository;
        _approverResolver = approverResolver;
        _requestMasterResolver = requestMasterResolver;
        _handlerResolver = handlerResolver;
    }

    public async Task SubmitAsync(int requestId, int actingEmployeeId)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);

        if (request.RequestStatus != RequestStatus.Draft)
        {
            throw new UserFriendlyException(L("WorkflowRequestCanOnlyBeSubmittedFromDraft"));
        }

        // Throws when the type is unregistered, retired, or has no reachable configuration -- a request
        // that cannot be routed has no valid next state, so it fails here rather than sitting in limbo.
        var requestMaster = await _requestMasterResolver.ResolveAsync(request);

        var context = new WorkflowTransactionContext(request, actingEmployeeId);
        var handler = _handlerResolver.Resolve(request.RequestType);

        if (handler != null)
        {
            // Type-specific refusals -- quota, overlap, eligibility -- happen before an approver is
            // assigned, so a rejection here costs nobody an approval round-trip.
            await handler.ValidateBeforeSubmitAsync(context);

            var summary = await handler.BuildSummaryAsync(context);
            if (summary != null)
            {
                request.SummaryEn = summary.SummaryEn;
                request.SummaryAr = summary.SummaryAr;
            }
        }

        request.WorkflowStep = 1;
        request.RequestStatus = RequestStatus.Open;

        await AssignApproversForStepAsync(request, requestMaster, step: 1);

        await AddLogAsync(request, actingEmployeeId, RequestActionType.Created, remark: null, workflowStep: 1);

        await _workflowRequestRepository.UpdateAsync(request);

        // Stamps the transaction row as Open, which is what takes it out of view for payroll, quota and
        // reports while it is in flight. Runs after the request is saved so the row can link to its id.
        if (handler != null)
        {
            await handler.OnSubmittedAsync(context);
        }

        // Last, once the request is real and routed: the people it has landed on are told they have something
        // to look at.
        await _notificationDispatcher.DispatchAsync(
            request,
            step: 1,
            WorkflowNotificationAction.Submit,
            actingEmployeeId);
    }

    public async Task ApproveAsync(int requestId, int actingEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);
        EnsureOpenAndCurrentApprover(request, actingEmployeeId);

        var currentStep = request.WorkflowStep;

        var requestMaster = await _requestMasterResolver.ResolveAsync(request);
        var requestDetail = GetRequestDetail(requestMaster.Id, currentStep);

        // A step can demand that its approver say something. Checked BEFORE any approver row is marked --
        // refusing afterwards would leave the step half-approved, with this employee recorded as having
        // acted on a request that did not move.
        //
        // Enforced here rather than in the screen so it holds for every caller: the inbox, a mobile client,
        // an integration. The screen can still ask first; it just is not what makes the rule true.
        if (requestDetail != null && requestDetail.IsCommentRequire && remark.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("ACommentIsRequiredToApproveAtThisWorkflowStep"));
        }

        // Mark this employee's pending approver row(s) at the current step as approved.
        var approvers = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id)
            .ToList();

        foreach (var approverRow in approvers.Where(a => !a.IsApproved && a.ApproverId == actingEmployeeId))
        {
            approverRow.IsApproved = true;
            await _workFlowApproverRepository.UpdateAsync(approverRow);
        }

        var stepApproved = IsStepApproved(approvers, requestDetail);

        await AddLogAsync(
            request,
            actingEmployeeId,
            RequestActionType.Approved,
            remark,
            currentStep,
            ResolveDelegatedFrom(request, requestMaster, requestDetail, actingEmployeeId));

        if (!stepApproved)
        {
            // Parallel/All step still has other pending approvers -- request stays Open at the same step.
            await _workflowRequestRepository.UpdateAsync(request);
            return;
        }

        var totalSteps = requestMaster.WorkflowSteps;
        var isFinalApproval = currentStep >= totalSteps;

        if (isFinalApproval)
        {
            request.RequestStatus = RequestStatus.Approved;
            request.ApprovalProcessTime = (DateTime.Now - request.LastModificationTime.GetValueOrDefault(DateTime.Now)).Days;
        }
        else
        {
            var nextStep = currentStep + 1;
            request.WorkflowStep = nextStep;

            await AssignApproversForStepAsync(request, requestMaster, nextStep);
        }

        await _workflowRequestRepository.UpdateAsync(request);

        if (isFinalApproval)
        {
            // Only on the LAST step. This is the once-per-request moment the transaction becomes real --
            // the row is marked approved and the handler applies its side effects (quota deduction, payroll
            // posting). Firing it on an intermediate step would apply them once per approver.
            //
            // Inside the same unit of work deliberately: if the handler throws, the approval rolls back with
            // it. A leave that could not deduct quota must not end up recorded as approved.
            var handler = _handlerResolver.Resolve(request.RequestType);
            if (handler != null)
            {
                await handler.OnApprovedAsync(new WorkflowTransactionContext(request, actingEmployeeId, remark));
            }
        }

        await _notificationDispatcher.DispatchAsync(
            request,
            currentStep,
            WorkflowNotificationAction.Approve,
            actingEmployeeId,
            remark);

        if (!isFinalApproval)
        {
            // The step it has just moved TO announces its own arrival, exactly as if the request had been
            // submitted to it -- otherwise the one person who now has to act is the one person not told.
            await _notificationDispatcher.DispatchAsync(
                request,
                request.WorkflowStep,
                WorkflowNotificationAction.Submit,
                actingEmployeeId);
        }
    }

    public async Task RejectAsync(int requestId, int actingEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);
        EnsureOpenAndCurrentApprover(request, actingEmployeeId);

        var requestMaster = await _requestMasterResolver.ResolveAsync(request);
        var requestDetail = GetRequestDetail(requestMaster.Id, request.WorkflowStep);
        if (requestDetail != null && !requestDetail.CanReject)
        {
            throw new UserFriendlyException(L("RejectIsNotAllowedAtThisWorkflowStep"));
        }

        request.RequestStatus = RequestStatus.Rejected;

        // A rejection carries at least as much consequence as an approval, so it records the delegation the
        // same way. "Who killed this request, and on whose authority" is asked more often, not less.
        await AddLogAsync(
            request,
            actingEmployeeId,
            RequestActionType.Rejected,
            remark,
            request.WorkflowStep,
            ResolveDelegatedFrom(request, requestMaster, requestDetail, actingEmployeeId));

        await _workflowRequestRepository.UpdateAsync(request);

        // Marks the row rejected, which keeps it hidden from the rest of the system, and lets the handler
        // release anything it reserved at submit.
        var rejectHandler = _handlerResolver.Resolve(request.RequestType);
        if (rejectHandler != null)
        {
            await rejectHandler.OnRejectedAsync(new WorkflowTransactionContext(request, actingEmployeeId, remark));
        }

        await _notificationDispatcher.DispatchAsync(
            request,
            request.WorkflowStep,
            WorkflowNotificationAction.Reject,
            actingEmployeeId,
            remark);
    }

    public async Task SendBackAsync(int requestId, int actingEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);
        EnsureOpenAndCurrentApprover(request, actingEmployeeId);

        var requestMaster = await _requestMasterResolver.ResolveAsync(request);
        var requestDetail = GetRequestDetail(requestMaster.Id, request.WorkflowStep);
        if (requestDetail != null && !requestDetail.CanSendBack)
        {
            throw new UserFriendlyException(L("SendBackIsNotAllowedAtThisWorkflowStep"));
        }

        var stepBeingSentBackFrom = request.WorkflowStep;

        foreach (var approverRow in _workFlowApproverRepository.GetAll()
                     .Where(a => a.WorkflowRequestId == request.Id).ToList())
        {
            await _workFlowApproverRepository.DeleteAsync(approverRow);
        }

        request.WorkflowStep = 1;
        request.RequestStatus = RequestStatus.Draft;

        await AddLogAsync(
            request,
            actingEmployeeId,
            RequestActionType.SentBack,
            remark,
            stepBeingSentBackFrom,
            ResolveDelegatedFrom(request, requestMaster, requestDetail, actingEmployeeId));

        await _workflowRequestRepository.UpdateAsync(request);

        // Back to draft: still alive and resubmittable, unlike a rejection.
        var sendBackHandler = _handlerResolver.Resolve(request.RequestType);
        if (sendBackHandler != null)
        {
            await sendBackHandler.OnSentBackAsync(new WorkflowTransactionContext(request, actingEmployeeId, remark));
        }

        // Announced from the step it was sent back FROM, since that is the step whose configuration decided
        // to send it back and therefore the one that says who should hear about it.
        await _notificationDispatcher.DispatchAsync(
            request,
            stepBeingSentBackFrom,
            WorkflowNotificationAction.SendBack,
            actingEmployeeId,
            remark);
    }

    public async Task ReassignAsync(int requestId, int actingEmployeeId, int newApproverEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);
        EnsureOpenAndCurrentApprover(request, actingEmployeeId);

        var pendingApproverRows = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id && !a.IsApproved && a.ApproverId == actingEmployeeId)
            .ToList();

        foreach (var approverRow in pendingApproverRows)
        {
            approverRow.ApproverId = newApproverEmployeeId;
            await _workFlowApproverRepository.UpdateAsync(approverRow);
        }

        if (request.ApproverId == actingEmployeeId)
        {
            request.ApproverId = newApproverEmployeeId;
        }

        // Transferred, not Assigned. The two are different events and legacy had both: Assigned is an ad-hoc
        // feedback request that leaves the approver in place, Transferred is this -- handing the decision
        // itself to somebody else. Logging them alike would make the history unable to answer who was
        // actually responsible for the step.
        await AddLogAsync(request, actingEmployeeId, RequestActionType.Transferred, remark, request.WorkflowStep);

        await _workflowRequestRepository.UpdateAsync(request);

        // Resolved AFTER the approver rows were repointed, so the person now holding the step is among the
        // people told about it.
        await _notificationDispatcher.DispatchAsync(
            request,
            request.WorkflowStep,
            WorkflowNotificationAction.Reassign,
            actingEmployeeId,
            remark);
    }

    /// <summary>
    /// Records that an approver opened the request, when the step is configured to track it.
    ///
    /// <para>
    /// Deliberately not a general audit trail: it writes only where <c>RequestDetail.EnableReadLog</c> is on,
    /// because the point is to answer "did they even look at it" for the steps where somebody cared enough to
    /// ask. Logging every view of every request would bury that answer in noise and grow a table nobody reads.
    /// </para>
    /// <para>
    /// Silent by design. This is a side effect of viewing, so a failure here must never stop the request
    /// being displayed -- and one read row per approver per step is enough to answer the question, so repeat
    /// views are not recorded again.
    /// </para>
    /// </summary>
    public async Task MarkAsReadAsync(int requestId, int actingEmployeeId)
    {
        var request = await _workflowRequestRepository.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
        {
            return;
        }

        RequestDetail requestDetail;
        try
        {
            var requestMaster = await _requestMasterResolver.ResolveAsync(request);
            requestDetail = GetRequestDetail(requestMaster.Id, request.WorkflowStep);
        }
        catch (UserFriendlyException)
        {
            // An unroutable request still has to be readable -- refusing to show it would hide the very
            // thing an administrator needs to see to fix the configuration.
            return;
        }

        if (requestDetail == null || !requestDetail.EnableReadLog)
        {
            return;
        }

        var alreadyLogged = _requestLogRepository.GetAll()
            .Any(l => l.WorkflowRequestId == request.Id
                      && l.ActionById == actingEmployeeId
                      && l.WorkflowStep == request.WorkflowStep
                      && l.ActionType == RequestActionType.Read);

        if (alreadyLogged)
        {
            return;
        }

        await _readLogRepository.InsertAsync(new WFReadLog
        {
            TenantId = request.TenantId,
            WorkflowRequestId = request.Id,
            ReadDateTime = DateTime.Now,
            User = actingEmployeeId.ToString()
        });

        // Also on the action log, so the activity feed shows it in sequence with everything else rather than
        // in a separate place nobody thinks to look.
        await AddLogAsync(request, actingEmployeeId, RequestActionType.Read, remark: null, workflowStep: request.WorkflowStep);
    }

    /// <summary>
    /// Authorization guard shared by Approve/Reject/SendBack/Reassign: ported from legacy
    /// <c>RequestProcessApproval.HideShowController</c>'s <c>IsApprover</c> computation -- the acting
    /// employee must hold an unresolved (<c>IsApproved == false</c>) <see cref="WorkFlowApprover"/> row for
    /// this request, or (fallback, for requests whose approver rows could not be resolved) be the request's
    /// <see cref="WorkflowRequest.ApproverId"/>.
    ///
    /// <para>
    /// <b>Delegation needs no branch here, and that is the design.</b> Legacy bolted an extra "...or is an
    /// <c>EmployeeDelegation</c> delegate of the assigned approver" clause onto this check, which is what
    /// made the delegator and the delegate BOTH able to act. HCMv3 substitutes upstream instead: the
    /// stand-in's employee id is what <see cref="IWorkflowApproverResolver"/> wrote into
    /// <see cref="WorkFlowApprover"/> when the step was assigned, so they pass this check as themselves and
    /// the delegator fails it as a natural consequence of no longer being on the request. Adding a
    /// delegate-of clause back here would silently restore the legacy "both people can act" behaviour AND
    /// open a second hop (a stand-in's own delegate could act on the first person's requests), so it must
    /// not be added.
    /// </para>
    /// <para>
    /// One consequence worth knowing: a request assigned BEFORE the delegation existed keeps its original
    /// approver, because approver rows are written at step entry and nothing rewrites them when a delegation
    /// is later approved. Cover therefore takes effect for everything routed from that point on, not
    /// retroactively for what is already sitting in the queue.
    /// </para>
    /// </summary>
    private void EnsureOpenAndCurrentApprover(WorkflowRequest request, int actingEmployeeId)
    {
        if (request.RequestStatus != RequestStatus.Open)
        {
            throw new UserFriendlyException(L("WorkflowRequestIsNotOpenForApprovalAction"));
        }

        if (request.WorkflowStep <= 0)
        {
            throw new UserFriendlyException(L("WorkflowRequestHasNotBeenSubmittedYet"));
        }

        var isCurrentApprover = _workFlowApproverRepository.GetAll()
            .Any(a => a.WorkflowRequestId == request.Id && !a.IsApproved && a.ApproverId == actingEmployeeId);

        if (!isCurrentApprover && request.ApproverId != actingEmployeeId)
        {
            throw new UserFriendlyException(L("YouAreNotTheCurrentApproverForThisWorkflowRequest"));
        }
    }

    private static bool IsStepApproved(List<WorkFlowApprover> approversAtRequest, RequestDetail requestDetail)
    {
        if (approversAtRequest.Count == 0)
        {
            return true;
        }

        var approvalRequire = requestDetail?.ApprovalRequire ?? ApprovalRequire.Single;

        return approvalRequire switch
        {
            ApprovalRequire.Single => approversAtRequest.Any(a => a.IsApproved),
            ApprovalRequire.All => approversAtRequest.All(a => a.IsApproved),
            ApprovalRequire.Parallel => approversAtRequest.All(a => a.IsApproved),
            _ => approversAtRequest.All(a => a.IsApproved)
        };
    }

    /// <summary>
    /// Whether this action is being taken by somebody standing in for the person the step nominated, and if
    /// so for whom.
    ///
    /// <para>
    /// Never allowed to break the action. A delegation is a fact ABOUT the approval, not a precondition of
    /// it -- if working out the "on behalf of" line fails for any reason, the approval still happens and the
    /// log simply reads as it did before this feature. Losing an approval to protect an audit annotation
    /// would be the wrong way round.
    /// </para>
    /// </summary>
    private int? ResolveDelegatedFrom(
        WorkflowRequest request,
        RequestMaster requestMaster,
        RequestDetail requestDetail,
        int actingEmployeeId)
    {
        try
        {
            return _approverResolver.ResolveDelegatedFrom(request, requestMaster, requestDetail, actingEmployeeId);
        }
        catch (Exception ex)
        {
            Logger.Warn(
                $"Could not determine whether employee {actingEmployeeId} acted under a delegation on "
                + $"workflow request {request.Id}: {ex.Message}",
                ex);

            return null;
        }
    }

    private RequestDetail GetRequestDetail(int requestMasterId, int step)
    {
        return _requestDetailRepository.GetAll()
            .FirstOrDefault(d => d.RequestMasterId == requestMasterId && d.Step == step);
    }

    /// <summary>
    /// Resolves and (re)assigns the approver(s) for <paramref name="step"/>, deleting any previous
    /// <see cref="WorkFlowApprover"/> rows for the request first. Ported (simplified per this interface's
    /// remarks) from legacy <c>WorkFlowServices.AssignWF</c> + <c>GetApprover</c> +
    /// <c>OperateApproverInfo(operationType: 1)</c>.
    /// </summary>
    private async Task AssignApproversForStepAsync(WorkflowRequest request, RequestMaster requestMaster, int step)
    {
        var requestDetail = GetRequestDetail(requestMaster.Id, step);

        var approverEmployeeIds = ResolveApproversForStep(request, requestMaster, requestDetail);

        if (approverEmployeeIds.Count == 0)
        {
            throw new UserFriendlyException(L("NoApproverCouldBeResolvedForThisWorkflowStep"));
        }

        foreach (var existing in _workFlowApproverRepository.GetAll()
                     .Where(a => a.WorkflowRequestId == request.Id).ToList())
        {
            await _workFlowApproverRepository.DeleteAsync(existing);
        }

        foreach (var employeeId in approverEmployeeIds)
        {
            await _workFlowApproverRepository.InsertAsync(new WorkFlowApprover
            {
                TenantId = request.TenantId,
                WorkflowRequestId = request.Id,
                ApproverId = employeeId,
                IsApproved = false
            });
        }

        request.ApproverId = approverEmployeeIds[0];
    }

    private List<int> ResolveApproversForStep(WorkflowRequest request, RequestMaster requestMaster, RequestDetail requestDetail)
    {
        // Delegated: role holders narrowed by org scope, manager chains walked to the configured level, and
        // signatories excluded from later signatory steps all live in one place, so escalation resolves
        // approvers exactly the way a normal step does.
        return _approverResolver.ResolveForStep(request, requestMaster, requestDetail);
    }

    /// <summary>
    /// Writes the audit row for an action, naming BOTH people when the actor is standing in for somebody.
    ///
    /// <para>
    /// <c>ActionById</c> is always whoever actually clicked, because that is the fact the history exists to
    /// record and overwriting it with the delegator would be a lie about who took the decision. What that
    /// alone loses is the other half of the story -- that the chain nominated a different person and a
    /// delegation is why it moved -- so the delegator is written into the comment, by name and code, as a
    /// localized phrase. For a payment batch or a disciplinary action, "who signed this off, and under whose
    /// authority" is the whole question the log is asked six months later.
    /// </para>
    /// <para>
    /// Recorded as text rather than a column deliberately: the substitution is fully reconstructable from
    /// the <c>EmployeeDelegation</c> rows, which are never hard-deleted, so a schema change would be storing
    /// a second copy of a fact the database already holds.
    /// </para>
    /// </summary>
    private async Task AddLogAsync(
        WorkflowRequest request,
        int actingEmployeeId,
        RequestActionType actionType,
        string remark,
        int workflowStep,
        int? onBehalfOfEmployeeId = null)
    {
        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = request.TenantId,
            WorkflowRequestId = request.Id,
            ActionById = actingEmployeeId,
            RequestActionComment = ComposeComment(remark, onBehalfOfEmployeeId),
            ActionDateTime = DateTime.Now,
            ActionType = actionType,
            WorkflowStep = workflowStep,
            Platform = WorkflowPlatform.Core
        });
    }

    private string ComposeComment(string remark, int? onBehalfOfEmployeeId)
    {
        if (!onBehalfOfEmployeeId.HasValue)
        {
            return remark;
        }

        var note = L("WorkflowActionTakenOnBehalfOf", DescribeEmployee(onBehalfOfEmployeeId.Value));

        return remark.IsNullOrWhiteSpace() ? note : remark + " -- " + note;
    }

    /// <summary>
    /// Name plus code. The code is what makes the line searchable and unambiguous when two people share a
    /// name, which in a workforce of any size they do.
    /// </summary>
    private string DescribeEmployee(int employeeId)
    {
        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Code, e.FullNameEn })
            .FirstOrDefault();

        if (employee == null)
        {
            return employeeId.ToString();
        }

        if (employee.FullNameEn.IsNullOrWhiteSpace())
        {
            return employee.Code ?? employeeId.ToString();
        }

        return employee.Code.IsNullOrWhiteSpace()
            ? employee.FullNameEn
            : employee.FullNameEn + " (" + employee.Code + ")";
    }
}
