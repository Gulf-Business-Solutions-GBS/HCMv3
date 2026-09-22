using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
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
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IWorkflowNotificationDispatcher _notificationDispatcher;

    public WorkflowActivityAppService(
        IRepository<WorkflowRequest> requestRepository,
        IRepository<RequestLog> requestLogRepository,
        IRepository<WorkflowComment> commentRepository,
        IRepository<WorkflowRemark> remarkRepository,
        IRepository<WorkFlowApprover> approverRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeSessionService employeeSessionService,
        IWorkflowApprovalService workflowApprovalService,
        IWorkflowNotificationDispatcher notificationDispatcher)
    {
        _notificationDispatcher = notificationDispatcher;
        _requestRepository = requestRepository;
        _requestLogRepository = requestLogRepository;
        _commentRepository = commentRepository;
        _remarkRepository = remarkRepository;
        _approverRepository = approverRepository;
        _employeeRepository = employeeRepository;
        _employeeSessionService = employeeSessionService;
        _workflowApprovalService = workflowApprovalService;
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

        var isFeedbackRecipient = employeeId.HasValue &&
                                  await _remarkRepository.GetAll()
                                      .AnyAsync(r => r.WorkflowRequestId == request.Id && r.CCReceiverId == employeeId.Value);

        return new RequestAccess(employeeId, isRequester, isApprover, isFeedbackRecipient, isAdmin);
    }

    /// <summary>Who the current user is relative to one request, and what that lets them see.</summary>
    private sealed class RequestAccess
    {
        public RequestAccess(int? employeeId, bool isRequester, bool isApprover, bool isFeedbackRecipient, bool isAdmin)
        {
            EmployeeId = employeeId;
            IsRequester = isRequester;
            IsApprover = isApprover;
            IsFeedbackRecipient = isFeedbackRecipient;
            IsAdmin = isAdmin;
        }

        public int? EmployeeId { get; }

        public bool IsRequester { get; }

        public bool IsApprover { get; }

        public bool IsFeedbackRecipient { get; }

        public bool IsAdmin { get; }

        public bool CanView => IsRequester || IsApprover || IsFeedbackRecipient || IsAdmin;

        /// <summary>
        /// The requester NEVER sees internal comments -- even when they are also an approver on their own
        /// request, which some configurations allow. "Approvers only" has to mean the same thing on every
        /// request or the feature is not trustworthy enough to use.
        /// </summary>
        public bool CanSeeInternal => !IsRequester && (IsApprover || IsFeedbackRecipient || IsAdmin);
    }
}
