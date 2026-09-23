using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow.Services;
using HCMv3.Workflow.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <inheritdoc cref="IWorkflowActivityAppService"/>
[AbpAuthorize]
public class WorkflowActivityAppService : HCMv3AppServiceBase, IWorkflowActivityAppService
{
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IRepository<WorkflowComment> _commentRepository;
    private readonly IRepository<WorkflowRemark> _remarkRepository;
    private readonly IRepository<WorkFlowApprover> _approverRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<HCMv3.OrganizationManagement.JobTitle> _jobTitleRepository;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IWorkflowNotificationDispatcher _notificationDispatcher;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public WorkflowActivityAppService(
        IRepository<WorkflowRequest> requestRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<WorkflowComment> commentRepository,
        IRepository<WorkflowRemark> remarkRepository,
        IRepository<WorkFlowApprover> approverRepository,
        IRepository<Employee> employeeRepository,
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<HCMv3.OrganizationManagement.JobTitle> jobTitleRepository,
        IEmployeeSessionService employeeSessionService,
        IWorkflowApprovalService workflowApprovalService,
        IWorkflowNotificationDispatcher notificationDispatcher,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IRequestMasterResolver requestMasterResolver,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _notificationDispatcher = notificationDispatcher;
        _requestRepository = requestRepository;
        _requestLogRepository = requestLogRepository;
        _commentRepository = commentRepository;
        _remarkRepository = remarkRepository;
        _approverRepository = approverRepository;
        _employeeRepository = employeeRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _requestDetailRepository = requestDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _jobTitleRepository = jobTitleRepository;
        _employeeSessionService = employeeSessionService;
        _workflowApprovalService = workflowApprovalService;
        _handlerResolver = handlerResolver;
        _requestMasterResolver = requestMasterResolver;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    /// <summary>
    /// <c>[HttpPost]</c> despite being a read: ABP maps <c>Get*</c> to HTTP GET, and NSwag then emits a
    /// custom input DTO as loose query-string parameters and never generates the DTO class at all -- the
    /// Angular client fails to compile against a type that does not exist. Same rule the <c>GetAll</c>
    /// methods across this codebase follow.
    /// </summary>
    [HttpPost]
    public async Task<WorkflowActivityFeedDto> GetActivityFeed(GetWorkflowActivityFeedInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);
        var access = await GetAccessAsync(request);

        EnsureCanView(access);

        var result = new WorkflowActivityFeedDto
        {
            WorkflowRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            SummaryEn = request.SummaryEn,
            SummaryAr = request.SummaryAr,
            RequestStatus = request.RequestStatus,
            CanComment = true,
            CanCommentInternally = access.CanSeeInternal
        };

        // Records that this approver looked at the request, for steps configured to track it. Placed here
        // because opening the activity feed IS how an approver reads a request -- there is no separate
        // "view" action to hang it off. A no-op for everyone else and for steps that do not ask for it.
        if (access.IsApprover && access.EmployeeId.HasValue)
        {
            await _workflowApprovalService.MarkAsReadAsync(request.Id, access.EmployeeId.Value);
        }

        var activities = new List<WorkflowActivityDto>();

        activities.AddRange(await GetActionActivitiesAsync(request.Id));
        activities.AddRange(await GetCommentActivitiesAsync(request.Id, access));
        activities.AddRange(await GetFeedbackRequestActivitiesAsync(request.Id));

        await FillActorsAsync(activities);

        // One chronological stream. Which table an entry came from is not something the reader should have
        // to think about -- that was the problem with legacy's separate grid and text blob.
        result.Activities = activities
            .OrderBy(a => a.OccurredAt)
            .ThenBy(a => a.Kind)
            .ThenBy(a => a.Id)
            .ToList();

        return result;
    }

    /// <summary>
    /// <c>[HttpPost]</c> for the same reason <see cref="GetActivityFeed"/> is: a <c>Get*</c> method taking a
    /// custom input DTO becomes loose query-string parameters under NSwag and the DTO class is never
    /// generated.
    /// </summary>
    [HttpPost]
    public async Task<WorkflowRequestDetailDto> GetRequestDetail(GetWorkflowRequestDetailInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);
        var access = await GetAccessAsync(request);

        // The check that matters. A workflow request id is a small integer, so without this anyone
        // authenticated could walk the range and read every salary advance, medical claim and resignation in
        // the tenant. Only the people this request is actually about may read it.
        EnsureCanView(access);

        var result = new WorkflowRequestDetailDto
        {
            WorkflowRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            RequestType = request.RequestType,
            TargetEntityId = request.TargetEntityId,
            RequestStatus = request.RequestStatus,
            SummaryEn = request.SummaryEn,
            SummaryAr = request.SummaryAr,
            SubmittedOn = request.CreationTime,
            WorkflowStep = request.WorkflowStep,
            RequesterEmployeeId = request.EmployeeId
        };

        await FillTypeNameAsync(result, request);
        await FillRequesterAsync(result, request);
        await FillFieldsAsync(result, request, access);
        await FillActionCapabilitiesAsync(result, request, access);

        return result;
    }

    /// <summary>
    /// Asks the type's own handler to project its row. A type with no handler at all still gets the engine's
    /// facts, because an approver staring at an empty card is the bug this endpoint was written to fix.
    /// </summary>
    private async Task FillFieldsAsync(WorkflowRequestDetailDto result, WorkflowRequest request, RequestAccess access)
    {
        var handler = _handlerResolver.Resolve(request.RequestType);

        if (handler == null)
        {
            result.IsDefaultProjection = true;
            result.Fields = new List<WorkflowDetailFieldDto>
            {
                new WorkflowDetailFieldDto { Label = L("RequestNumber"), Value = request.RequestNumber, Kind = WorkflowDetailFieldKind.Text },
                new WorkflowDetailFieldDto { Label = L("SubmittedOn"), Value = request.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), Kind = WorkflowDetailFieldKind.DateTime },
                new WorkflowDetailFieldDto { Label = L("Summary"), Value = CurrentSummary(request), Kind = WorkflowDetailFieldKind.LongText }
            };

            return;
        }

        var context = new WorkflowTransactionContext(request, access.EmployeeId ?? 0);
        var detail = await handler.BuildDetailAsync(context);

        if (detail == null)
        {
            result.IsDefaultProjection = true;
            return;
        }

        // A projection that is only the three engine fields is the base class's fallback rather than the
        // type's own work. Saying so lets the screen be honest about it instead of implying a leave request
        // really does have three fields.
        result.IsDefaultProjection = detail.Fields.Count <= 3
                                     && detail.Fields.Any(f => f.Label == L("Summary"));

        result.SubmissionComment = detail.SubmissionComment;
        result.Fields = detail.Fields
            .Select(f => new WorkflowDetailFieldDto { Label = f.Label, Value = f.Value, Kind = f.Kind })
            .ToList();
    }

    private async Task FillTypeNameAsync(WorkflowRequestDetailDto result, WorkflowRequest request)
    {
        if (request.RequestType.IsNullOrWhiteSpace())
        {
            return;
        }

        var type = await _transactionTypeRepository.GetAll()
            .Where(t => t.Code == request.RequestType)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefaultAsync();

        if (type == null)
        {
            return;
        }

        result.RequestTypeNameEn = type.NameEn;
        result.RequestTypeNameAr = type.NameAr;
    }

    /// <summary>
    /// The subject of the request, as a person: name, employee number, job title, photo. This is the
    /// right-hand column of the reference screen, and it is what turns "request EXP-2026-00003" into
    /// "Ahmed, Driver, staff 2362".
    /// </summary>
    private async Task FillRequesterAsync(WorkflowRequestDetailDto result, WorkflowRequest request)
    {
        if (!request.EmployeeId.HasValue)
        {
            return;
        }

        var employeeId = request.EmployeeId.Value;

        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, HasPhoto = e.PhotoPath != null })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return;
        }

        result.RequesterCode = employee.Code;
        result.RequesterNameEn = employee.FullNameEn;
        result.RequesterNameAr = employee.FullNameAr;
        result.RequesterPhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null;

        // The most recent org assignment carries the title they hold today, which is the one an approver
        // needs -- an old assignment would describe a job they have already left.
        var jobTitle = await (from assignment in _orgAssignmentRepository.GetAll()
                              join title in _jobTitleRepository.GetAll()
                                  on assignment.JobTitleId equals (int?)title.Id
                              where assignment.EmployeeId == employeeId
                              orderby assignment.StartDate descending
                              select new { title.NameEn, title.NameAr })
            .FirstOrDefaultAsync();

        if (jobTitle == null)
        {
            return;
        }

        result.RequesterJobTitleEn = jobTitle.NameEn;
        result.RequesterJobTitleAr = jobTitle.NameAr;
    }

    /// <summary>
    /// What this viewer may actually do, so the dialog offers a Reject button only where the engine would
    /// accept one. A chain that cannot be resolved leaves approve-only rather than failing the read: being
    /// unable to configure send-back should not stop an approver seeing the request.
    /// </summary>
    private async Task FillActionCapabilitiesAsync(WorkflowRequestDetailDto result, WorkflowRequest request, RequestAccess access)
    {
        // "Current approver" is narrower than "approver": someone who approved at step 1 keeps their view of
        // the request forever, but must not be offered the buttons again at step 2.
        result.IsCurrentApprover = access.EmployeeId.HasValue
                                   && request.RequestStatus == RequestStatus.Open
                                   && await _approverRepository.GetAll()
                                       .AnyAsync(a => a.WorkflowRequestId == request.Id
                                                      && a.ApproverId == access.EmployeeId.Value
                                                      && !a.IsApproved);

        if (!result.IsCurrentApprover)
        {
            return;
        }

        result.CanApprove = true;

        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);

            var detail = await _requestDetailRepository.GetAll()
                .Where(d => d.RequestMasterId == master.Id && d.Step == request.WorkflowStep)
                .Select(d => new { d.CanReject, d.CanSendBack })
                .FirstOrDefaultAsync();

            var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
            var sendBackToCreator = workflowSettings != null && workflowSettings.SendBackToCreator;

            result.CanReject = detail?.CanReject ?? false;
            result.CanSendBack = (detail?.CanSendBack ?? false) && sendBackToCreator;
        }
        catch (UserFriendlyException)
        {
            result.CanReject = false;
            result.CanSendBack = false;
        }
    }

    private string CurrentSummary(WorkflowRequest request)
    {
        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        return isArabic
            ? (request.SummaryAr.IsNullOrWhiteSpace() ? request.SummaryEn : request.SummaryAr)
            : request.SummaryEn;
    }

    public async Task<WorkflowActivityDto> AddComment(AddWorkflowCommentInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);
        var access = await GetAccessAsync(request);

        EnsureCanView(access);

        // A comment is authored BY an employee, so an unlinked user genuinely cannot post one -- but they
        // CAN reach here, because admin permission grants view access without any employee link. Without
        // this guard the next line dereferences a null employee id and the caller gets a 500 instead of
        // being told what is wrong. Found by running it, not by the tests: every test linked the signed-in
        // user to an employee, so this path was never exercised.
        if (!access.EmployeeId.HasValue)
        {
            throw new UserFriendlyException(L("CurrentUserIsNotLinkedToAnEmployee"));
        }

        if (input.IsInternal && !access.CanSeeInternal)
        {
            // Refused rather than silently downgraded to a public comment: the author believes they are
            // speaking privately, and quietly publishing it would be worse than rejecting it.
            throw new UserFriendlyException(L("InternalCommentsAreForApproversOnly"));
        }

        if (input.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.FirstOrDefaultAsync(
                c => c.Id == input.ParentCommentId.Value && c.WorkflowRequestId == request.Id);

            if (parent == null)
            {
                throw new UserFriendlyException(L("WorkflowCommentNotFound"));
            }

            // A public reply under an internal parent would leak the private thread's existence and context
            // to the requester.
            if (parent.IsInternal && !input.IsInternal)
            {
                throw new UserFriendlyException(L("ReplyToInternalCommentMustBeInternal"));
            }
        }

        var comment = new WorkflowComment
        {
            TenantId = AbpSession.TenantId ?? 0,
            WorkflowRequestId = request.Id,
            ParentCommentId = input.ParentCommentId,
            AuthorEmployeeId = access.EmployeeId.Value,
            Body = input.Body,
            IsInternal = input.IsInternal
        };

        await _commentRepository.InsertAndGetIdAsync(comment);

        // An INTERNAL comment is deliberately not announced: the step's notification configuration routes to
        // the requester as readily as to approvers, and the whole point of an internal note is that they do
        // not see it.
        if (!input.IsInternal)
        {
            await _notificationDispatcher.DispatchAsync(
                request,
                request.WorkflowStep,
                WorkflowNotificationAction.Remark,
                access.EmployeeId.Value,
                input.Body);
        }

        var dto = ToDto(comment, canEdit: true);
        await FillActorsAsync(new List<WorkflowActivityDto> { dto });

        return dto;
    }

    public async Task<WorkflowActivityDto> EditComment(EditWorkflowCommentInput input)
    {
        var comment = await _commentRepository.GetAsync(input.Id);
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        // Only the author. An approver editing someone else's comment would rewrite the record of what was
        // said, which is exactly what the audit trail exists to prevent.
        if (!employeeId.HasValue || comment.AuthorEmployeeId != employeeId.Value)
        {
            throw new AbpAuthorizationException(L("OnlyTheAuthorCanEditThisComment"));
        }

        comment.Body = input.Body;
        comment.IsEdited = true;

        await _commentRepository.UpdateAsync(comment);

        var dto = ToDto(comment, canEdit: true);
        await FillActorsAsync(new List<WorkflowActivityDto> { dto });

        return dto;
    }

    public async Task DeleteComment(EntityDto input)
    {
        var comment = await _commentRepository.GetAsync(input.Id);
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        if (!employeeId.HasValue || comment.AuthorEmployeeId != employeeId.Value)
        {
            throw new AbpAuthorizationException(L("OnlyTheAuthorCanDeleteThisComment"));
        }

        var hasReplies = await _commentRepository.GetAll().AnyAsync(c => c.ParentCommentId == comment.Id);
        if (hasReplies)
        {
            // Deleting a parent would orphan its replies and strip the context they were written against.
            throw new UserFriendlyException(L("CannotDeleteACommentThatHasReplies"));
        }

        await _commentRepository.DeleteAsync(comment);
    }

    private async Task<List<WorkflowActivityDto>> GetActionActivitiesAsync(int requestId)
    {
        var logs = await _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == requestId)
            .Select(l => new
            {
                l.Id,
                l.ActionById,
                l.ActionDateTime,
                l.ActionType,
                l.RequestActionComment,
                l.WorkflowStep
            })
            .ToListAsync();

        return logs.Select(l => new WorkflowActivityDto
        {
            Kind = WorkflowActivityKind.Action,
            Id = l.Id,
            ActorEmployeeId = l.ActionById,
            OccurredAt = l.ActionDateTime,
            ActionType = l.ActionType,
            Body = l.RequestActionComment,
            WorkflowStep = l.WorkflowStep,

            // Audit rows are immutable by definition.
            CanEdit = false
        }).ToList();
    }

    private async Task<List<WorkflowActivityDto>> GetCommentActivitiesAsync(int requestId, RequestAccess access)
    {
        var query = _commentRepository.GetAll().Where(c => c.WorkflowRequestId == requestId);

        // The filter is applied in the QUERY, not after materialising. An internal comment must never reach
        // the requester's browser at all, not merely be hidden by the UI.
        if (!access.CanSeeInternal)
        {
            query = query.Where(c => !c.IsInternal);
        }

        var comments = await query.ToListAsync();

        return comments.Select(c => ToDto(c, canEdit: access.EmployeeId.HasValue && c.AuthorEmployeeId == access.EmployeeId.Value)).ToList();
    }

    private async Task<List<WorkflowActivityDto>> GetFeedbackRequestActivitiesAsync(int requestId)
    {
        // WorkflowRemark is an ad-hoc feedback assignment, not a remark: someone outside the configured
        // approval chain was asked for their opinion. Shown as a system event so the thread explains why a
        // person who is not an approver suddenly appears in it.
        var remarks = await _remarkRepository.GetAll()
            .Where(r => r.WorkflowRequestId == requestId)
            .Select(r => new { r.Id, r.CCReceiverId, r.CreationTime })
            .ToListAsync();

        return remarks.Select(r => new WorkflowActivityDto
        {
            Kind = WorkflowActivityKind.FeedbackRequested,
            Id = r.Id,
            ActorEmployeeId = r.CCReceiverId,
            OccurredAt = r.CreationTime,
            CanEdit = false
        }).ToList();
    }

    /// <summary>
    /// Resolves every actor in one query rather than per entry, and points each at the photo endpoint rather
    /// than embedding the image.
    /// </summary>
    private async Task FillActorsAsync(List<WorkflowActivityDto> activities)
    {
        var employeeIds = activities
            .Where(a => a.ActorEmployeeId.HasValue)
            .Select(a => a.ActorEmployeeId.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return;
        }

        var employees = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new
            {
                e.Id,
                e.FullNameEn,
                e.FullNameAr,
                HasPhoto = e.PhotoPath != null
            })
            .ToListAsync();

        var byId = employees.ToDictionary(e => e.Id);

        foreach (var activity in activities.Where(a => a.ActorEmployeeId.HasValue))
        {
            if (!byId.TryGetValue(activity.ActorEmployeeId.Value, out var employee))
            {
                continue;
            }

            activity.ActorNameEn = employee.FullNameEn;
            activity.ActorNameAr = employee.FullNameAr;
            activity.ActorPhotoUrl = employee.HasPhoto
                ? "/api/employee-photo/" + employee.Id
                : null;
        }
    }

    private static WorkflowActivityDto ToDto(WorkflowComment comment, bool canEdit)
    {
        return new WorkflowActivityDto
        {
            Kind = WorkflowActivityKind.Comment,
            Id = comment.Id,
            ActorEmployeeId = comment.AuthorEmployeeId,
            OccurredAt = comment.CreationTime,
            Body = comment.Body,
            IsInternal = comment.IsInternal,
            IsEdited = comment.IsEdited,
            ParentCommentId = comment.ParentCommentId,
            CanEdit = canEdit
        };
    }

    private void EnsureCanView(RequestAccess access)
    {
        if (!access.CanView)
        {
            throw new AbpAuthorizationException(L("YouCannotViewThisWorkflowRequest"));
        }
    }

    private async Task<RequestAccess> GetAccessAsync(WorkflowRequest request)
    {
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();
        var isAdmin = await PermissionChecker.IsGrantedAsync(AppPermissions.Pages_Workflow_WorkflowRequests);

        var isRequester = employeeId.HasValue && request.EmployeeId == employeeId.Value;

        var isApprover = employeeId.HasValue &&
                         await _approverRepository.GetAll()
                             .AnyAsync(a => a.WorkflowRequestId == request.Id && a.ApproverId == employeeId.Value);

        // THE fix for "why can an approver not see the other approvers' comments?".
        //
        // WorkFlowApprover holds only the step the request is sitting on right now: AssignApproversForStep
        // deletes every row for the request before inserting the next step's. So the manager who approved at
        // step 1 has no row the moment it reaches step 2 -- and with it lost not just the internal thread but
        // the request itself, which answered AbpAuthorizationException if they opened it again. Someone who
        // has acted on a request is permanently part of it, so the durable record of who acted is the one to
        // ask: RequestLog, which is never deleted.
        //
        // Created is excluded deliberately. It is the submission, and its actor is the requester -- counting
        // it would make every requester an approver of their own request and hand them the internal thread,
        // which is precisely the rule the requester exclusion below exists to hold.
        var isPastApprover = employeeId.HasValue && !isApprover &&
                             await _requestLogRepository.GetAll()
                                 .AnyAsync(l => l.WorkflowRequestId == request.Id
                                                && l.ActionById == employeeId.Value
                                                && l.ActionType != RequestActionType.Created);

        var isFeedbackRecipient = employeeId.HasValue &&
                                  await _remarkRepository.GetAll()
                                      .AnyAsync(r => r.WorkflowRequestId == request.Id && r.CCReceiverId == employeeId.Value);

        return new RequestAccess(employeeId, isRequester, isApprover, isPastApprover, isFeedbackRecipient, isAdmin);
    }

    /// <summary>Who the current user is relative to one request, and what that lets them see.</summary>
    private sealed class RequestAccess
    {
        public RequestAccess(int? employeeId, bool isRequester, bool isApprover, bool isPastApprover, bool isFeedbackRecipient, bool isAdmin)
        {
            EmployeeId = employeeId;
            IsRequester = isRequester;
            IsApprover = isApprover;
            IsPastApprover = isPastApprover;
            IsFeedbackRecipient = isFeedbackRecipient;
            IsAdmin = isAdmin;
        }

        public int? EmployeeId { get; }

        public bool IsRequester { get; }

        /// <summary>Named on the request's current step.</summary>
        public bool IsApprover { get; }

        /// <summary>
        /// Acted on this request at an earlier step. They no longer hold a <c>WorkFlowApprover</c> row -- the
        /// engine deletes those on every step change -- but they are still one of the people the request is
        /// about, and the conversation they took part in must not vanish from under them.
        /// </summary>
        public bool IsPastApprover { get; }

        public bool IsFeedbackRecipient { get; }

        public bool IsAdmin { get; }

        public bool CanView => IsRequester || IsApprover || IsPastApprover || IsFeedbackRecipient || IsAdmin;

        /// <summary>
        /// Everyone on the approving side of a request sees the whole thread, including each other's
        /// internal notes; the requester sees only what was said publicly.
        ///
        /// <para>
        /// The requester exclusion comes FIRST and is absolute -- even when they are also an approver on
        /// their own request, which some configurations allow. "Approvers only" has to mean the same thing
        /// on every request or the feature is not trustworthy enough to use.
        /// </para>
        ///
        /// <para>
        /// Past approvers were added here for the user's complaint that approvers could not read one
        /// another's remarks. They could, in fact, while they held the step -- and then the request moved on,
        /// their approver row was deleted with the step, and the thread disappeared. The new composer no
        /// longer writes internal comments at all (the checkbox is gone), so going forward the thread is
        /// simply shared by everyone involved; this governs what is already stored.
        /// </para>
        /// </summary>
        public bool CanSeeInternal => !IsRequester && (IsApprover || IsPastApprover || IsFeedbackRecipient || IsAdmin);
    }
}
