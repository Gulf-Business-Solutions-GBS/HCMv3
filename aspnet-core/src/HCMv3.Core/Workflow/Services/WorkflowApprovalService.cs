using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
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
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;

    public WorkflowApprovalService(
        IRepository<WorkflowRequest> workflowRequestRepository,
        IRepository<WorkFlowApprover> workFlowApproverRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowTransactionHandlerResolver handlerResolver)
    {
        _workflowRequestRepository = workflowRequestRepository;
        _workFlowApproverRepository = workFlowApproverRepository;
        _requestLogRepository = requestLogRepository;
        _requestDetailRepository = requestDetailRepository;
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _employeeManagerRepository = employeeManagerRepository;
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
    }

    public async Task ApproveAsync(int requestId, int actingEmployeeId, string remark)
    {
        var request = await _workflowRequestRepository.GetAsync(requestId);
        EnsureOpenAndCurrentApprover(request, actingEmployeeId);

        var currentStep = request.WorkflowStep;

        // Mark this employee's pending approver row(s) at the current step as approved.
        var approvers = _workFlowApproverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id)
            .ToList();

        foreach (var approverRow in approvers.Where(a => !a.IsApproved && a.ApproverId == actingEmployeeId))
        {
            approverRow.IsApproved = true;
            await _workFlowApproverRepository.UpdateAsync(approverRow);
        }

        var requestMaster = await _requestMasterResolver.ResolveAsync(request);
        var requestDetail = GetRequestDetail(requestMaster.Id, currentStep);

        var stepApproved = IsStepApproved(approvers, requestDetail);

        await AddLogAsync(request, actingEmployeeId, RequestActionType.Approved, remark, currentStep);

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

        await AddLogAsync(request, actingEmployeeId, RequestActionType.Rejected, remark, request.WorkflowStep);

        await _workflowRequestRepository.UpdateAsync(request);

        // Marks the row rejected, which keeps it hidden from the rest of the system, and lets the handler
        // release anything it reserved at submit.
        var rejectHandler = _handlerResolver.Resolve(request.RequestType);
        if (rejectHandler != null)
        {
            await rejectHandler.OnRejectedAsync(new WorkflowTransactionContext(request, actingEmployeeId, remark));
        }
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

        await AddLogAsync(request, actingEmployeeId, RequestActionType.SentBack, remark, stepBeingSentBackFrom);

        await _workflowRequestRepository.UpdateAsync(request);

        // Back to draft: still alive and resubmittable, unlike a rejection.
        var sendBackHandler = _handlerResolver.Resolve(request.RequestType);
        if (sendBackHandler != null)
        {
            await sendBackHandler.OnSentBackAsync(new WorkflowTransactionContext(request, actingEmployeeId, remark));
        }
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

        await AddLogAsync(request, actingEmployeeId, RequestActionType.Assigned, remark, request.WorkflowStep);

        await _workflowRequestRepository.UpdateAsync(request);
    }

    /// <summary>
    /// Authorization guard shared by Approve/Reject/SendBack/Reassign: ported from legacy
    /// <c>RequestProcessApproval.HideShowController</c>'s <c>IsApprover</c> computation -- the acting
    /// employee must hold an unresolved (<c>IsApproved == false</c>) <see cref="WorkFlowApprover"/> row for
    /// this request, or (fallback, for requests whose approver rows could not be resolved) be the request's
    /// <see cref="WorkflowRequest.ApproverId"/>. Legacy's additional "or is an <c>EmployeeDelegation</c>
    /// delegate of the assigned approver" branch is not ported (delegation-of-approval is a separate,
    /// unported feature in this port -- see <see cref="EmployeeWorkflowRole"/> remarks for the related, but
    /// distinct, "who holds this WorkflowRole" delegation concept that IS ported).
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
        if (requestDetail != null && requestDetail.ApproverPerson == WorkflowApproverPersonType.Person
            && requestDetail.WorkflowRoleId.HasValue)
        {
            var today = DateTime.Now.Date;
            var ids = _employeeWorkflowRoleRepository.GetAll()
                .Where(r => r.WorkflowRoleId == requestDetail.WorkflowRoleId.Value
                            && r.StartDate.Date <= today && r.EndDate.Date >= today)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToList();

            if (ids.Count > 0)
            {
                return ids;
            }
        }

        if (requestDetail != null && requestDetail.ApproverPerson == WorkflowApproverPersonType.PersonManager
            && request.EmployeeId.HasValue)
        {
            var today = DateTime.Now.Date;
            var managerId = _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == request.EmployeeId.Value
                            && m.StartDate.Date <= today && m.EndDate.Date >= today)
                .Select(m => (int?)m.ManagerId)
                .FirstOrDefault();

            if (managerId.HasValue)
            {
                return new List<int> { managerId.Value };
            }
        }

        // Fallback: RequestMaster.DefaultApprover (mirrors legacy's own last-resort behaviour).
        if (requestMaster.DefaultApproverId.HasValue)
        {
            return new List<int> { requestMaster.DefaultApproverId.Value };
        }

        return new List<int>();
    }

    private async Task AddLogAsync(WorkflowRequest request, int actingEmployeeId, RequestActionType actionType, string remark, int workflowStep)
    {
        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = request.TenantId,
            WorkflowRequestId = request.Id,
            ActionById = actingEmployeeId,
            RequestActionComment = remark,
            ActionDateTime = DateTime.Now,
            ActionType = actionType,
            WorkflowStep = workflowStep,
            Platform = WorkflowPlatform.Core
        });
    }
}
