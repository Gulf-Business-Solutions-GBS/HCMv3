using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IEssInboxAppService"/>
[AbpAuthorize(AppPermissions.Ess_Inbox)]
public class EssInboxAppService : HCMv3AppServiceBase, IEssInboxAppService
{
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IRepository<WorkflowRemark> _feedbackRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IWorkflowPlatformProvider _platformProvider;
    private readonly IWorkflowActivityAppService _activityAppService;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public EssInboxAppService(
        IRepository<WorkFlowApprover> approverRepository,
        IRepository<WorkflowRemark> feedbackRepository,
        IRepository<RequestLog> requestLogRepository,
        IWorkflowPlatformProvider platformProvider,
        IWorkflowActivityAppService activityAppService,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _approverRepository = approverRepository;
        _feedbackRepository = feedbackRepository;
        _requestLogRepository = requestLogRepository;
        _platformProvider = platformProvider;
        _activityAppService = activityAppService;
        _requestRepository = requestRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _requestDetailRepository = requestDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeScopeProvider = employeeScopeProvider;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<EssInboxItemDto>> GetMyInbox(GetMyInboxInput input)
    {
        input.Normalize();

        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // Two sources, paged as one list.
        //
        // Approvals are the requests this person must decide. Feedback requests are the ones a colleague
        // has asked them to look at. Both are "waiting on you" in the sense that matters -- something is
        // expected -- and putting them in separate screens would mean an ask nobody opened because it was
        // filed somewhere they do not check. They are paged together and each row says which it is.
        var approvals = input.Kind == EssInboxItemKind.FeedbackRequest
            ? new List<WorkflowRequest>()
            : await ApprovalRequestsAsync(employeeId, input.RequestType);

        var feedback = input.Kind == EssInboxItemKind.Approval
            ? new List<WorkflowRemark>()
            : await FeedbackRequestsAsync(employeeId, input.RequestType);

        var items = await BuildItemsAsync(approvals);

        items.AddRange(await BuildFeedbackItemsAsync(feedback));

        // Newest first, across both kinds. Sorting each source separately and concatenating would put every
        // feedback request below every approval regardless of age.
        var ordered = items
            .OrderByDescending(i => i.SubmittedOn ?? DateTime.MinValue)
            .ToList();

        var totalCount = ordered.Count;

        // Paged in memory rather than in SQL, deliberately. Two different tables cannot be paged as one
        // without a union the ORM would have to materialise anyway, and an inbox is small -- what is
        // waiting on one person right now, not an archive. If that ever stops being true this is the line
        // to revisit, and it will be obvious which one it was.
        var page = ordered
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<EssInboxItemDto>(totalCount, page);
    }

    /// <summary>
    /// The requests waiting on this person to decide them.
    ///
    /// <para>
    /// The step filter is what keeps this an inbox rather than an archive. Approver rows survive a step
    /// change now, so "not yet approved" alone would also match a row left unapproved at an earlier step --
    /// one of three approvers where any one sufficed -- and that request would sit in their inbox for the
    /// rest of its life, unactionable.
    /// </para>
    /// </summary>
    private Task<List<WorkflowRequest>> ApprovalRequestsAsync(int employeeId, string requestType)
    {
        return _approverRepository.GetAll()
            .Where(a => a.ApproverId == employeeId
                        && !a.IsApproved
                        && a.WorkflowStep == a.WorkflowRequest.WorkflowStep
                        && a.WorkflowRequest.RequestStatus == RequestStatus.Open)
            .Select(a => a.WorkflowRequest)
            .WhereIf(!requestType.IsNullOrWhiteSpace(), r => r.RequestType == requestType)
            .ToListAsync();
    }

    /// <summary>
    /// The requests somebody has asked this person to comment on and which they have not answered.
    ///
    /// <para>
    /// Answered asks drop out: the point of the row is that something is expected, and once they have said
    /// their piece nothing is. The request stays readable to them either way -- being asked is permanent,
    /// being asked and not having replied is not.
    /// </para>
    /// </summary>
    private Task<List<WorkflowRemark>> FeedbackRequestsAsync(int employeeId, string requestType)
    {
        return _feedbackRepository.GetAll()
            .Where(r => r.CCReceiverId == employeeId
                        && r.RespondedAt == null
                        && r.WorkflowRequest.RequestStatus == RequestStatus.Open)
            .WhereIf(!requestType.IsNullOrWhiteSpace(), r => r.WorkflowRequest.RequestType == requestType)
            .ToListAsync();
    }

    /// <summary>
    /// Feedback asks as inbox rows: the same request summary, but saying who asked and why, and offering
    /// none of the decision buttons -- this person cannot approve anything.
    /// </summary>
    private async Task<List<EssInboxItemDto>> BuildFeedbackItemsAsync(List<WorkflowRemark> asks)
    {
        if (asks.Count == 0)
        {
            return new List<EssInboxItemDto>();
        }

        var requestIds = asks.Select(a => a.WorkflowRequestId).Distinct().ToList();

        var requests = await _requestRepository.GetAll()
            .Where(r => requestIds.Contains(r.Id))
            .ToListAsync();

        var items = await BuildItemsAsync(requests);
        var byRequestId = items.ToDictionary(i => i.Id);

        var askerIds = asks.Where(a => a.RequestedById.HasValue).Select(a => a.RequestedById.Value).Distinct().ToList();

        var askers = (await _employeeRepository.GetAll()
            .Where(e => askerIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FullNameEn, e.FullNameAr })
            .ToListAsync()).ToDictionary(e => e.Id);

        var result = new List<EssInboxItemDto>();

        foreach (var ask in asks)
        {
            if (!byRequestId.TryGetValue(ask.WorkflowRequestId, out var item))
            {
                continue;
            }

            item.Kind = EssInboxItemKind.FeedbackRequest;
            item.FeedbackNote = ask.RequestNote;
            item.FeedbackRequestedByEmployeeId = ask.RequestedById;

            // No decision is theirs to take. Said here as well as enforced on the server, so the screen
            // never offers a button that would be refused.
            item.CanReject = false;
            item.CanSendBack = false;

            if (ask.RequestedById.HasValue && askers.TryGetValue(ask.RequestedById.Value, out var asker))
            {
                item.FeedbackRequestedByNameEn = asker.FullNameEn;
                item.FeedbackRequestedByNameAr = asker.FullNameAr;
            }

            result.Add(item);
        }

        return result;
    }

    public async Task<int> GetMyPendingCount()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();

        // Zero rather than an error: the badge is decoration, and an unlinked user seeing "0" is correct.
        // The screens themselves explain the missing link.
        if (!employeeId.HasValue)
        {
            return 0;
        }

        // Must match GetMyInbox exactly, or the badge says a number the list cannot produce.
        return await _approverRepository.GetAll()
            .CountAsync(a => a.ApproverId == employeeId.Value
                             && !a.IsApproved
                             && a.WorkflowStep == a.WorkflowRequest.WorkflowStep
                             && a.WorkflowRequest.RequestStatus == RequestStatus.Open);
    }

    public async Task Approve(ApproveRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // The engine re-checks that this employee is actually the current approver, so authorization does
        // not rest on the inbox query having been honest about what it listed.
        await _workflowApprovalService.ApproveAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    public async Task Reject(RejectRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        await _workflowApprovalService.RejectAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    public async Task SendBack(SendBackRequestInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        await _workflowApprovalService.SendBackAsync(input.WorkflowRequestId, employeeId, input.Comment);
    }

    /// <summary>
    /// Fills in the display fields for a page of requests, resolving requesters and type names in one query
    /// each rather than per row.
    /// </summary>
    // ------------------------------------------------------------------ get feedback

    public async Task RequestFeedback(RequestFeedbackInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);

        // Only somebody the request is actually with may ask. Otherwise anyone who could see a request
        // could pull colleagues into it, and "who asked you?" would have no good answer.
        await EnsureCurrentApproverAsync(request, employeeId);

        var wanted = (input.EmployeeIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (wanted.Count == 0)
        {
            throw new UserFriendlyException(L("SelectAtLeastOneColleagueToAsk"));
        }

        // Neither the requester nor the asker. Asking the person whose request it is what they think of it
        // is not something anyone means to do, and asking yourself is a no-op that would still send a
        // notification.
        if (request.EmployeeId.HasValue && wanted.Contains(request.EmployeeId.Value))
        {
            throw new UserFriendlyException(L("CannotAskTheRequesterForFeedbackOnTheirOwnRequest"));
        }

        wanted.Remove(employeeId);

        var alreadyAsked = await _feedbackRepository.GetAll()
            .Where(r => r.WorkflowRequestId == request.Id && r.CCReceiverId.HasValue)
            .Select(r => r.CCReceiverId.Value)
            .ToListAsync();

        var note = input.Note.IsNullOrWhiteSpace() ? null : input.Note.Trim();

        foreach (var candidate in wanted.Where(id => !alreadyAsked.Contains(id)))
        {
            await _feedbackRepository.InsertAsync(new WorkflowRemark
            {
                TenantId = AbpSession.GetTenantId(),
                WorkflowRequestId = request.Id,
                CCReceiverId = candidate,
                RequestedById = employeeId,
                WorkflowStep = request.WorkflowStep,
                RequestNote = note
            });
        }
    }

    public async Task SubmitFeedback(SubmitFeedbackInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var ask = await _feedbackRepository.GetAll()
            .Where(r => r.WorkflowRequestId == input.WorkflowRequestId
                        && r.CCReceiverId == employeeId
                        && r.RespondedAt == null)
            .FirstOrDefaultAsync();

        if (ask == null)
        {
            throw new UserFriendlyException(L("YouHaveNotBeenAskedForFeedbackOnThisRequest"));
        }

        // Posted through the ordinary comment path, so it lands in the conversation everyone reads rather
        // than in a private channel only the asker would see.
        await _activityAppService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = input.WorkflowRequestId,
            Body = input.Comment
        });

        ask.RespondedAt = Clock.Now;
        await _feedbackRepository.UpdateAsync(ask);

        // And into the request log, which is the durable account of what was done to this request. The
        // conversation carries the words; the log carries the fact that somebody was asked and answered,
        // with a step and a timestamp, in the one place a report reads.
        await _requestLogRepository.InsertAsync(new RequestLog
        {
            TenantId = AbpSession.GetTenantId(),
            WorkflowRequestId = input.WorkflowRequestId,
            ActionById = employeeId,
            RequestActionComment = input.Comment,
            ActionDateTime = Clock.Now,

            // Remarked, not Approved: this person decided nothing. The type is what stops a feedback entry
            // being counted as an approval by anything reading the log.
            ActionType = RequestActionType.Remarked,
            WorkflowStep = ask.WorkflowStep,
            ActionDays = 0,
            Platform = _platformProvider.Current
        });
    }

    [HttpPost]
    public async Task<List<FeedbackCandidateDto>> GetFeedbackCandidates(GetFeedbackCandidatesInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);

        await EnsureCurrentApproverAsync(request, employeeId);

        var asks = await _feedbackRepository.GetAll()
            .Where(r => r.WorkflowRequestId == request.Id && r.CCReceiverId.HasValue)
            .Select(r => new { EmployeeId = r.CCReceiverId.Value, Answered = r.RespondedAt != null })
            .ToListAsync();

        var asked = asks.Select(a => a.EmployeeId).ToList();
        var answered = asks.Where(a => a.Answered).Select(a => a.EmployeeId).ToList();

        var excluded = new List<int> { employeeId };

        if (request.EmployeeId.HasValue)
        {
            excluded.Add(request.EmployeeId.Value);
        }

        var candidates = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active && !excluded.Contains(e.Id))
            .Select(e => new FeedbackCandidateDto
            {
                EmployeeId = e.Id,
                Code = e.Code,
                NameEn = e.FullNameEn,
                NameAr = e.FullNameAr,
                PhotoUrl = e.PhotoPath != null ? "/api/employee-photo/" + e.Id : null
            })
            .OrderBy(e => e.NameEn)
            .ToListAsync();

        // Anyone with an approver row on this request, at any step. Not just the current one: an earlier
        // approver has already had their say and a later one has it coming, and both are worth flagging.
        var approvers = await _approverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id && a.ApproverId.HasValue)
            .Select(a => a.ApproverId.Value)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            candidate.AlreadyAsked = asked.Contains(candidate.EmployeeId);
            candidate.HasResponded = answered.Contains(candidate.EmployeeId);
            candidate.IsAlreadyApprover = approvers.Contains(candidate.EmployeeId);
        }

        return candidates;
    }

    public async Task CancelFeedbackRequest(CancelFeedbackInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);

        // The same entitlement as asking. Somebody who cannot pull a colleague into a request should not be
        // able to pull them back out of it either.
        await EnsureCurrentApproverAsync(request, employeeId);

        var ask = await _feedbackRepository.GetAll()
            .Where(r => r.WorkflowRequestId == input.WorkflowRequestId && r.CCReceiverId == input.EmployeeId)
            .FirstOrDefaultAsync();

        if (ask == null)
        {
            throw new UserFriendlyException(L("YouHaveNotBeenAskedForFeedbackOnThisRequest"));
        }

        // Answered asks stay. What they said is in the conversation and withdrawing the request would not
        // unsay it -- it would leave a remark on the request with nothing to explain why it is there.
        if (ask.RespondedAt.HasValue)
        {
            throw new UserFriendlyException(L("CannotWithdrawAFeedbackRequestAlreadyAnswered"));
        }

        // Soft-deleted, like everything else here: FullAuditedEntity records who withdrew it and when, so
        // the fact that somebody was asked and un-asked is not lost.
        await _feedbackRepository.DeleteAsync(ask);
    }

    [HttpPost]
    public async Task<List<FeedbackRequestDto>> GetFeedbackRequests(GetFeedbackCandidatesInput input)
    {
        var rows = await _feedbackRepository.GetAll()
            .Where(r => r.WorkflowRequestId == input.WorkflowRequestId)
            .Select(r => new FeedbackRequestDto
            {
                Id = r.Id,
                EmployeeId = r.CCReceiverId ?? 0,
                RequestedByEmployeeId = r.RequestedById,
                Note = r.RequestNote,
                WorkflowStep = r.WorkflowStep,
                RequestedAt = r.CreationTime,
                RespondedAt = r.RespondedAt
            })
            .ToListAsync();

        await FillFeedbackPeopleAsync(rows);

        return rows;
    }

    /// <summary>Names and photos for the feedback rows, in one query rather than per row.</summary>
    private async Task FillFeedbackPeopleAsync(List<FeedbackRequestDto> rows)
    {
        var ids = rows.Select(r => r.EmployeeId)
            .Concat(rows.Where(r => r.RequestedByEmployeeId.HasValue).Select(r => r.RequestedByEmployeeId.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var people = (await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, HasPhoto = e.PhotoPath != null })
            .ToListAsync()).ToDictionary(e => e.Id);

        foreach (var row in rows)
        {
            if (people.TryGetValue(row.EmployeeId, out var person))
            {
                row.Code = person.Code;
                row.NameEn = person.FullNameEn;
                row.NameAr = person.FullNameAr;
                row.PhotoUrl = person.HasPhoto ? "/api/employee-photo/" + person.Id : null;
            }

            if (row.RequestedByEmployeeId.HasValue && people.TryGetValue(row.RequestedByEmployeeId.Value, out var asker))
            {
                row.RequestedByNameEn = asker.FullNameEn;
                row.RequestedByNameAr = asker.FullNameAr;
            }
        }
    }

    /// <summary>
    /// Throws unless the caller is the approver this request is presently waiting on.
    ///
    /// <para>
    /// Deliberately narrower than "can see it". A past approver, the requester and an administrator can all
    /// read a request; none of them is entitled to pull colleagues into somebody else's decision.
    /// </para>
    /// </summary>
    private async Task EnsureCurrentApproverAsync(WorkflowRequest request, int employeeId)
    {
        var isCurrentApprover = await _approverRepository.GetAll()
            .AnyAsync(a => a.WorkflowRequestId == request.Id
                           && a.WorkflowStep == request.WorkflowStep
                           && a.ApproverId == employeeId
                           && !a.IsApproved);

        if (!isCurrentApprover)
        {
            throw new UserFriendlyException(L("YouAreNotTheCurrentApproverForThisWorkflowRequest"));
        }
    }

    private async Task<List<EssInboxItemDto>> BuildItemsAsync(List<WorkflowRequest> requests)
    {
        if (requests.Count == 0)
        {
            return new List<EssInboxItemDto>();
        }

        var employeeIds = requests.Where(r => r.EmployeeId.HasValue).Select(r => r.EmployeeId.Value).Distinct().ToList();
        var requestTypes = requests.Select(r => r.RequestType).Distinct().ToList();

        var employees = (await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FullNameEn, e.FullNameAr, HasPhoto = e.PhotoPath != null })
            .ToListAsync()).ToDictionary(e => e.Id);

        var types = (await _transactionTypeRepository.GetAll()
            .Where(t => requestTypes.Contains(t.Code))
            .Select(t => new { t.Code, t.NameEn, t.NameAr })
            .ToListAsync()).ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        var items = new List<EssInboxItemDto>();

        foreach (var request in requests)
        {
            var item = new EssInboxItemDto
            {
                Id = request.Id,
                RequestType = request.RequestType,
                RequestNumber = request.RequestNumber,
                SummaryEn = request.SummaryEn,
                SummaryAr = request.SummaryAr,
                RequesterEmployeeId = request.EmployeeId,
                SubmittedOn = request.CreationTime,
                WorkflowStep = request.WorkflowStep
            };

            if (request.EmployeeId.HasValue && employees.TryGetValue(request.EmployeeId.Value, out var employee))
            {
                item.RequesterNameEn = employee.FullNameEn;
                item.RequesterNameAr = employee.FullNameAr;
                item.RequesterPhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null;
            }

            if (!request.RequestType.IsNullOrWhiteSpace() && types.TryGetValue(request.RequestType, out var type))
            {
                item.RequestTypeNameEn = type.NameEn;
                item.RequestTypeNameAr = type.NameAr;
            }

            await FillStepCapabilitiesAsync(item, request);

            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Reads what this step actually permits, so the screen does not offer a Reject button the engine would
    /// refuse. A configuration that cannot be resolved leaves approve-only rather than failing the whole
    /// inbox -- one broken chain should not stop an approver seeing everything else waiting on them.
    /// </summary>
    private async Task FillStepCapabilitiesAsync(EssInboxItemDto item, WorkflowRequest request)
    {
        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);

            var details = await _requestDetailRepository.GetAll()
                .Where(d => d.RequestMasterId == master.Id)
                .Select(d => new { d.Step, d.ApproverPerson, d.CanReject, d.CanSendBack })
                .ToListAsync();

            // The denominator of "Step 2 / 3", so it has to be counted in the same numbering as the
            // numerator -- WorkflowStep, which is RequestDetail.Step, the engine's own step number.
            //
            // It used to be RequestMaster.WorkflowSteps, and that was wrong twice over. WorkflowSteps is a
            // stored count refreshed only when steps are edited through RequestDetailsAppService, so a chain
            // configured any other way leaves it stale -- EXIT-STD has three rows and says 2. And even when
            // it is current it counts the creator row: the step whose ApproverPerson is None, which says who
            // may RAISE the request and nominates nobody to approve it (see IWorkflowStepSequence). A
            // request sitting at step 2 of that chain was therefore shown as "Step 2 / 2", reading as final
            // when a whole approval still followed it.
            //
            // The last step that actually names an approver answers both: it ignores the creator row, it
            // ignores the stored count, and it is the step the engine will in fact stop at, because
            // GetNextApproverStep only ever moves to a row with an approver on it.
            var lastApprovalStep = details
                .Where(d => d.ApproverPerson != WorkflowApproverPersonType.None)
                .Select(d => (int?)d.Step)
                .Max();

            item.TotalSteps = lastApprovalStep ?? 0;

            var detail = details.FirstOrDefault(d => d.Step == request.WorkflowStep);

            // Legacy ESS (UnifiedLeaveRequest / RosterPlanner controllers): the send-back button is offered
            // only when ManageWorkflowSettings.SendBackToCreator is on, over and above the step's own flag.
            var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
            var sendBackToCreator = workflowSettings != null && workflowSettings.SendBackToCreator;

            item.CanReject = detail?.CanReject ?? false;
            item.CanSendBack = (detail?.CanSendBack ?? false) && sendBackToCreator;

            // Legacy TravelController: at the step named by ManageWorkflowSettings.EnableTravelCostOnWorkflowstep
            // the approver of a travel request is offered the travel cost entry.
            item.EnableTravelCost = workflowSettings != null
                                    && workflowSettings.EnableTravelCostOnWorkflowstep > 0
                                    && workflowSettings.EnableTravelCostOnWorkflowstep == request.WorkflowStep
                                    && string.Equals(request.RequestType,
                                        HCMv3.TravelManagement.Workflow.TravelWorkflowTransactionHandler.TravelRequestType,
                                        StringComparison.OrdinalIgnoreCase);
        }
        catch (Abp.UI.UserFriendlyException)
        {
            item.TotalSteps = 0;
            item.CanReject = false;
            item.CanSendBack = false;
        }
    }
}
