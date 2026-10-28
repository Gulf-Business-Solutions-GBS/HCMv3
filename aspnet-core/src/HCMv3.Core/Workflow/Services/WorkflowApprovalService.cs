using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.EntityFrameworkCore;

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
    private readonly IWorkflowStepSequence _stepSequence;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IWorkflowPlatformProvider _platformProvider;

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
        IWorkflowNotificationDispatcher notificationDispatcher,
        IWorkflowStepSequence stepSequence,
        IEmployeeScopeProvider employeeScopeProvider,
        IWorkflowPlatformProvider platformProvider)
    {
        _platformProvider = platformProvider;
        _stepSequence = stepSequence;
        _employeeScopeProvider = employeeScopeProvider;
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
        // The second of the two routing chokepoints. The admin submitter guards the path that CREATES a
        // request; this guards the path that SENDS one, so self-service and amendments are covered by the
        // same rule without each having to remember it.
        NonInteractiveScope.ThrowIfActive();

        var request = await _workflowRequestRepository.GetAsync(requestId);

        if (request.RequestStatus != RequestStatus.Draft)
        {
            throw new UserFriendlyException(L("WorkflowRequestCanOnlyBeSubmittedFromDraft"));
        }

        // Throws when the type is unregistered, retired, or has no reachable configuration -- a request
        // that cannot be routed has no valid next state, so it fails here rather than sitting in limbo.
        var requestMaster = await _requestMasterResolver.ResolveAsync(request);

        // "Step 1 says who may CREATE this" -- checked before anything is validated or routed, because a
        // person who is not allowed to raise the request at all should not have its type-specific rules run
        // against them, let alone reach an approver's inbox.
        await EnsureActingEmployeeMayRaiseAsync(request, requestMaster);

        var context = new WorkflowTransactionContext(request, actingEmployeeId);
        var handler = _handlerResolver.Resolve(request.RequestType);

        // The note the requester typed on the form. It lives on the transaction row, in a column only the
        // type's own handler can name, so the engine has to ask for it.
        string submissionComment = null;

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

            submissionComment = await GetSubmissionCommentAsync(handler, context);
        }

        // NOT a constant 1. A step whose ApproverPerson is None is a creator row -- it says who may raise the
        // request, not who approves it -- so approvals begin at the first step that actually names an
        // approver. See IWorkflowStepSequence for why "start at 2" would be just as wrong as "start at 1".
        var firstApproverStep = _stepSequence.GetFirstApproverStep(requestMaster);

        // Past any "Person on Request" step whose field this request leaves empty. If every step is skipped,
        // the first one stays and AssignApproversForStepAsync refuses the submission exactly as for any other
        // chain that resolves nobody -- an unapproved request must not slip through as approved.
        firstApproverStep = SkipStepsWithNoPersonOnRequest(request, requestMaster, firstApproverStep, actingEmployeeId)
                            ?? firstApproverStep;

        request.WorkflowStep = firstApproverStep;
        request.RequestStatus = RequestStatus.Open;

        await AssignApproversForStepAsync(request, requestMaster, firstApproverStep, actingEmployeeId);

        // The requester's own words, carried onto the Created entry so the activity feed opens with what
        // they said rather than with a bare "Created" line and their comment nowhere at all.
        //
        // Logged against the step the request was CREATED at, not the step it was routed to. Those are
        // different questions and the log answers the first: a reader looking at "Created ... step 2" sees
        // an act attributed to a step whose approver had nothing to do with it, while step 1 -- the step
        // that says who may raise this request -- shows no activity at all.
        await AddLogAsync(request, actingEmployeeId, RequestActionType.Created, remark: submissionComment,
            workflowStep: FirstStepOf(requestMaster, firstApproverStep));

        await _workflowRequestRepository.UpdateAsync(request);

        // Stamps the transaction row as Open, which is what takes it out of view for payroll, quota and
        // reports while it is in flight. Runs after the request is saved so the row can link to its id.
        if (handler != null)
        {
            await handler.OnSubmittedAsync(context);

            // The step the request was raised AT -- the creator row -- or 0 where the chain has none.
            await NotifyStepActionAsync(
                handler,
                context,
                WorkflowStepAction.Submitted,
                _stepSequence.GetCreatorStep(requestMaster)?.Step ?? 0);
        }

        // Flush before dispatching, and this is load-bearing rather than tidiness.
        //
        // AssignApproversForStepAsync inserts the WorkFlowApprover rows through the repository, but until
        // the unit of work is saved they exist only as tracked entities. The dispatcher resolves "who is
        // pending on this request?" with a database query, so without this it reads a table that does not
        // have them yet, finds nobody, and silently notifies only the requester -- who then receives
        // "this has been sent to you for approval" about their own request, while the approver's bell
        // stays empty. That is exactly what happened on EXP-2026-00002.
        await CurrentUnitOfWork.SaveChangesAsync();

        // Last, once the request is real and routed: the people it has landed on are told they have something
        // to look at.
        await _notificationDispatcher.DispatchAsync(
            request,
            firstApproverStep,
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

        // This step's rows only. Rows now survive a step change, so an unqualified read would drag in
        // step 1's approvals and let ApprovalRequire.All be satisfied by people who approved something
        // else -- the step would pass without its own approvers having acted.
        var approvers = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id && a.WorkflowStep == request.WorkflowStep)
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

        // Every approval at the step, whether or not it completes the step or the chain: the rules tied to a
        // step number in legacy fired on the approver's click, not on the step's outcome. Advisory -- see
        // NotifyStepActionAsync.
        await NotifyStepActionAsync(
            _handlerResolver.Resolve(request.RequestType),
            new WorkflowTransactionContext(request, actingEmployeeId, remark),
            WorkflowStepAction.Approved,
            currentStep);

        if (!stepApproved)
        {
            // Parallel/All step still has other pending approvers -- request stays Open at the same step.
            await _workflowRequestRepository.UpdateAsync(request);
            return;
        }

        // The next step that NAMES AN APPROVER, not simply currentStep + 1: a later row with
        // ApproverPerson = None is a creator row and must be stepped over, and "nothing above this" is the
        // definition of complete. Walking off the end instead would strand the request on a step that
        // resolves nobody.
        var nextStep = _stepSequence.GetNextApproverStep(requestMaster, currentStep);

        // A "Person on Request" step the request leaves empty is passed over; passing over the last one means
        // this approval completes the chain.
        if (nextStep.HasValue)
        {
            nextStep = SkipStepsWithNoPersonOnRequest(request, requestMaster, nextStep.Value, actingEmployeeId);
        }

        var isFinalApproval = !nextStep.HasValue;

        if (isFinalApproval)
        {
            request.RequestStatus = RequestStatus.Approved;
            request.ApprovalProcessTime = (DateTime.Now - request.LastModificationTime.GetValueOrDefault(DateTime.Now)).Days;
        }
        else
        {
            request.WorkflowStep = nextStep.Value;

            await AssignApproversForStepAsync(request, requestMaster, nextStep.Value, actingEmployeeId);
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

    public async Task WithdrawAsync(int requestId, int actingEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);

        if (request.EmployeeId != actingEmployeeId)
        {
            throw new UserFriendlyException(L("OnlyTheRequesterCanWithdrawThisRequest"));
        }

        if (request.RequestStatus != RequestStatus.Open && request.RequestStatus != RequestStatus.Draft)
        {
            throw new UserFriendlyException(L("ThisRequestHasAlreadyBeenDecided"));
        }

        var stepWithdrawnAt = request.WorkflowStep;

        // Closed, not Rejected: nobody refused it. The inbox lists only Open requests, so the approvers it
        // was sitting with stop seeing it without their rows being touched -- they stay as history.
        request.RequestStatus = RequestStatus.Closed;

        await AddLogAsync(request, actingEmployeeId, RequestActionType.Closed, remark, stepWithdrawnAt);

        await _workflowRequestRepository.UpdateAsync(request);
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

        // Nothing is deleted. A send-back is part of the request's history, and so are the people it was
        // sitting with when it happened. Re-submission re-assigns the step it lands on, which supersedes
        // that step's rows in place -- see AssignApproversForStepAsync.

        // Back to the first APPROVER step, not to step 1. Where step 1 is a creator row, parking the request
        // there would leave a resubmitted request looking for an approver on a row that nominates none.
        request.WorkflowStep = _stepSequence.GetFirstApproverStep(requestMaster);
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

        // Legacy RequestProcessApproval.AssignNewApprover only offered employees not already approving this
        // step. Handing the step to one of them would merge two approvers into one and silently lose a vote.
        var alreadyApprovingThisStep = newApproverEmployeeId == actingEmployeeId
            || _workFlowApproverRepository.GetAll()
                .Any(a => a.WorkflowRequestId == request.Id
                          && a.WorkflowStep == request.WorkflowStep
                          && !a.IsApproved
                          && a.ApproverId == newApproverEmployeeId);
        if (alreadyApprovingThisStep)
        {
            throw new UserFriendlyException(L("NewApproverIsAlreadyApprovingThisStep"));
        }

        var pendingApproverRows = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id
                        && a.WorkflowStep == request.WorkflowStep
                        && !a.IsApproved
                        && a.ApproverId == actingEmployeeId)
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

        // "Not yet approved" is no longer enough on its own: a row left unapproved at an earlier step --
        // one of three approvers where any one was sufficient -- stays on the request forever. Without the
        // step filter that person could act on every later step of a request they were never asked about.
        var isCurrentApprover = _workFlowApproverRepository.GetAll()
            .Any(a => a.WorkflowRequestId == request.Id
                      && a.WorkflowStep == request.WorkflowStep
                      && !a.IsApproved
                      && a.ApproverId == actingEmployeeId);

        if (!isCurrentApprover && request.ApproverId != actingEmployeeId)
        {
            throw new UserFriendlyException(L("YouAreNotTheCurrentApproverForThisWorkflowRequest"));
        }
    }

    /// <summary>
    /// Enforces the creator rule the chain's first step carries: WHO may raise this request, and FOR WHOM.
    ///
    /// <para>
    /// The rule lives on the step that nominates no approver (<c>ApproverPerson = None</c>) -- legacy's step
    /// 1 -- in its <see cref="RequestDetail.Person"/> column. It has been stored and editable in HCMv3 since
    /// the port and read by nothing, so until now an administrator could set "employees may only raise this
    /// for themselves" and the engine would accept a request raised for anybody.
    /// </para>
    /// <para>
    /// Checked at SUBMIT, which is the single funnel both submission paths go through --
    /// <c>MyTransactionAppServiceBase</c> for self-service and <c>AdminTransactionSubmitter</c> for
    /// HR-raised. Putting it in either caller would leave the other unguarded.
    /// </para>
    /// <para>
    /// <b>An actor with no employee record is unrestricted, deliberately.</b> Admin accounts in this product
    /// are never linked to an <c>Employee</c> -- that is a standing rule, not an oversight -- so on the
    /// HR-raised path there is frequently no employee to scope FROM. <see cref="IEmployeeScopeProvider"/>
    /// resolves an unlinked user to "sees nobody", which is the right default for an ESS list query and
    /// exactly the wrong one here: applied literally it would refuse every HR-raised request in the system
    /// the day this shipped. So the guard returns early instead, and the request is governed by the
    /// permission on the admin screen that raised it.
    /// </para>
    /// </summary>
    private async Task EnsureActingEmployeeMayRaiseAsync(WorkflowRequest request, RequestMaster requestMaster)
    {
        var creatorStep = _stepSequence.GetCreatorStep(requestMaster);

        // No creator row, or one that names no scope. Every configuration that predates this convention --
        // including LEAVE-STD and MANPOWER-STD, whose step 1 is an approver -- lands here, so their
        // behaviour is unchanged.
        if (creatorStep == null || creatorStep.Person == WorkflowPersonScope.None)
        {
            return;
        }

        if (creatorStep.Person == WorkflowPersonScope.All)
        {
            return;
        }

        if (!request.EmployeeId.HasValue)
        {
            // Nobody to check against: a header over many employees (a payment batch) is not "for" one
            // person, so there is no subject the scope could be applied to.
            return;
        }

        // From the SESSION, not from the actingEmployeeId argument. The admin path passes the subject's own
        // id as a stand-in when the signed-in user has no employee link, which would make a Themselves check
        // pass trivially and mean nothing. See the remarks above for why an unlinked actor is unrestricted.
        var currentEmployeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();
        if (!currentEmployeeId.HasValue)
        {
            return;
        }

        var scope = MapToEmployeeScope(creatorStep.Person);

        if (await _employeeScopeProvider.IsInScopeAsync(request.EmployeeId.Value, scope))
        {
            return;
        }

        throw new UserFriendlyException(L("YouAreNotAllowedToRaiseThisRequestForThisEmployee"));
    }

    /// <summary>
    /// The workflow setup screen's scope onto the one the rest of the product already uses. Mapped rather
    /// than reimplemented: <see cref="IEmployeeScopeProvider"/> is where "whose records may I touch" is
    /// answered for ESS, and a second evaluator here would be a second thing to keep correct.
    /// </summary>
    private static EmployeeScope MapToEmployeeScope(WorkflowPersonScope person)
    {
        switch (person)
        {
            case WorkflowPersonScope.Themselves:
                return EmployeeScope.Self;

            case WorkflowPersonScope.Department:
                return EmployeeScope.Department;

            // Self plus everyone below them in the reporting line, at any depth -- EmployeeScope.Subordinates
            // includes the root employee, which is what makes it "self AND subordinates".
            case WorkflowPersonScope.SelfSubordinates:
                return EmployeeScope.Subordinates;

            default:
                return EmployeeScope.All;
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

    /// <summary>
    /// Tells the type's handler that somebody acted at a step, and never lets the answer matter.
    ///
    /// <para>
    /// Swallows everything, on purpose. What hangs off this is a side effect of the action -- a passport
    /// withdrawal raised because a leave reached a step -- and losing a leave approval to protect a
    /// convenience would be the wrong way round. Handlers are told the same thing from their side: observe
    /// and defer, do not write here.
    /// </para>
    /// </summary>
    private async Task NotifyStepActionAsync(
        IWorkflowTransactionHandler handler,
        WorkflowTransactionContext context,
        WorkflowStepAction action,
        int step)
    {
        if (handler == null)
        {
            return;
        }

        try
        {
            await handler.OnStepActionAsync(context, action, step);
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"The step observer for workflow request {context.Request?.Id} of type {context.RequestType} failed at step {step} ({action}); the action itself was not affected.",
                exception);
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
    /// <summary>
    /// Asks the handler for the requester's submission note.
    ///
    /// <para>
    /// Swallows a failure on purpose, and this is the one place in submit where that is right. A defect in a
    /// type's detail projection must not stop an employee submitting a leave request: the note is decoration
    /// on an audit row, while the submission is the transaction. Everything else in this method fails loudly
    /// because everything else changes what the request IS.
    /// </para>
    /// </summary>
    private async Task<string> GetSubmissionCommentAsync(IWorkflowTransactionHandler handler, WorkflowTransactionContext context)
    {
        try
        {
            var detail = await handler.BuildDetailAsync(context);

            return string.IsNullOrWhiteSpace(detail?.SubmissionComment) ? null : detail.SubmissionComment.Trim();
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"Could not read the submission comment for workflow request {context.Request?.Id} of type {context.RequestType}; the request was submitted without it.",
                exception);

            return null;
        }
    }

    private async Task AssignApproversForStepAsync(
        WorkflowRequest request, RequestMaster requestMaster, int step, int actingEmployeeId)
    {
        var requestDetail = GetRequestDetail(requestMaster.Id, step);

        var approverEmployeeIds = ResolveApproversForStep(request, requestMaster, requestDetail, actingEmployeeId);

        if (approverEmployeeIds.Count == 0)
        {
            throw new UserFriendlyException(L("NoApproverCouldBeResolvedForThisWorkflowStep"));
        }

        // THIS step's rows are replaced; every other step's are left alone.
        //
        // The old code deleted every row on the request, which is what destroyed the history. Deleting
        // nothing is not the answer either: a step can be assigned twice -- a send-back and re-submission
        // land on the same step, and escalation re-resolves one -- and leaving the previous attempt behind
        // would give the step two sets of approvers, doubling ApprovalRequire.All's requirement.
        foreach (var superseded in _workFlowApproverRepository.GetAll()
                     .Where(a => a.WorkflowRequestId == request.Id && a.WorkflowStep == step).ToList())
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
                WorkflowStep = step
            });
        }

        request.ApproverId = approverEmployeeIds[0];

        // The moment some types act on: the appraisal adds its reviewers here, as legacy did when approvers
        // were assigned. Inside the action on purpose -- see IWorkflowTransactionHandler.OnApproversAssignedAsync.
        var handler = _handlerResolver.Resolve(request.RequestType);
        if (handler != null)
        {
            await handler.OnApproversAssignedAsync(
                new WorkflowTransactionContext(request, actingEmployeeId),
                step,
                approverEmployeeIds);
        }
    }

    /// <summary>
    /// The first step of the chain, whatever it is for.
    ///
    /// <para>
    /// Normally the creator step -- the one naming who may raise the request, which approves nothing. A
    /// chain configured without one starts at its first approver step instead, and the fallback says so
    /// rather than inventing a step 1 that does not exist.
    /// </para>
    /// </summary>
    private int FirstStepOf(RequestMaster requestMaster, int fallback)
    {
        var steps = _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == requestMaster.Id)
            .Select(d => d.Step)
            .ToList();

        return steps.Count == 0 ? fallback : steps.Min();
    }

    /// <summary>
    /// <paramref name="step"/>, or the first approver step after it, skipping every
    /// <see cref="WorkflowApproverPersonType.PersonOnRequest"/> step this request names nobody for. Null when
    /// that skips past the end of the chain.
    ///
    /// <para>
    /// Only that kind of step is ever skipped. Any other step that resolves nobody is a setup fault and still
    /// fails loudly in <see cref="AssignApproversForStepAsync"/>; a leave with no delegate is not a fault, it
    /// is simply a leave where nobody has to accept a handover.
    /// </para>
    /// </summary>
    private int? SkipStepsWithNoPersonOnRequest(
        WorkflowRequest request, RequestMaster requestMaster, int step, int actingEmployeeId)
    {
        int? candidate = step;

        while (candidate.HasValue)
        {
            var requestDetail = GetRequestDetail(requestMaster.Id, candidate.Value);

            if (requestDetail?.ApproverPerson != WorkflowApproverPersonType.PersonOnRequest
                || ResolveApproversForStep(request, requestMaster, requestDetail, actingEmployeeId).Count > 0)
            {
                return candidate;
            }

            candidate = _stepSequence.GetNextApproverStep(requestMaster, candidate.Value);
        }

        return null;
    }

    private List<int> ResolveApproversForStep(
        WorkflowRequest request, RequestMaster requestMaster, RequestDetail requestDetail, int actingEmployeeId)
    {
        // Delegated: role holders narrowed by org scope, manager chains walked to the configured level, and
        // signatories excluded from later signatory steps all live in one place, so escalation resolves
        // approvers exactly the way a normal step does.
        return _approverResolver.ResolveForStep(request, requestMaster, requestDetail, actingEmployeeId);
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
        var now = DateTime.Now;

        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = request.TenantId,
            WorkflowRequestId = request.Id,
            ActionById = actingEmployeeId,
            RequestActionComment = ComposeComment(remark, onBehalfOfEmployeeId),
            ActionDateTime = now,
            ActionType = actionType,
            WorkflowStep = workflowStep,
            ActionDays = await CalculateActionDaysAsync(request.Id, now),
            Platform = _platformProvider.Current
        });
    }

    /// <summary>
    /// How long this action sat waiting, in whole days.
    ///
    /// <para>
    /// Measured from the PREVIOUS action on the request, not from when the request was raised. That is the
    /// question the column is asked -- "how many days did this approver take" -- and measuring from the
    /// start instead would charge every approver for the delays of everyone before them, so the last person
    /// in a five-step chain would always look like the slowest.
    /// </para>
    ///
    /// <para>
    /// The first action has nothing before it and is zero.
    /// </para>
    ///
    /// <para>
    /// CALENDAR days, not elapsed hours divided by 24. An approval at 08:00 the morning after a 23:00
    /// submission is "next day" to everyone who will read this, and elapsed-time arithmetic calls it zero.
    /// More decisively: the escalation service writes this same column using calendar days, and
    /// EscalateAfterDays and ReminderDays are configured in calendar days too. A report that sets "days taken"
    /// beside "escalate after" has to be comparing the same unit, or the comparison means nothing.
    /// </para>
    /// </summary>
    private async Task<int> CalculateActionDaysAsync(int requestId, DateTime actionDateTime)
    {
        var previous = await _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == requestId)
            .OrderByDescending(l => l.ActionDateTime)
            .Select(l => (DateTime?)l.ActionDateTime)
            .FirstOrDefaultAsync();

        if (!previous.HasValue)
        {
            return 0;
        }

        var days = (actionDateTime.Date - previous.Value.Date).Days;

        // Never negative. Clock adjustments and rows written by a migration with a backdated timestamp are
        // both real, and a negative "days taken" in a report is worse than a zero.
        return days <= 0 ? 0 : days;
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
