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
using HCMv3.Dto;
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
    private readonly IRepository<Abp.Organizations.OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<HCMv3.OrganizationManagement.WorkLocation> _workLocationRepository;
    private readonly IRepository<WFReadLog> _readLogRepository;
    private readonly IRepository<EmployeeDelegation> _delegationRepository;
    private readonly IRepository<WorkflowRole> _workflowRoleRepository;
    private readonly IWorkflowDelegationResolver _delegationResolver;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IWorkflowNotificationDispatcher _notificationDispatcher;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly HCMv3.Workflow.Exporting.IWorkflowRequestLogExcelExporter _requestLogExporter;

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
        IRepository<Abp.Organizations.OrganizationUnit, long> organizationUnitRepository,
        IRepository<HCMv3.OrganizationManagement.WorkLocation> workLocationRepository,
        IRepository<WFReadLog> readLogRepository,
        IRepository<EmployeeDelegation> delegationRepository,
        IRepository<WorkflowRole> workflowRoleRepository,
        IWorkflowDelegationResolver delegationResolver,
        IEmployeeSessionService employeeSessionService,
        IWorkflowApprovalService workflowApprovalService,
        IWorkflowNotificationDispatcher notificationDispatcher,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IRequestMasterResolver requestMasterResolver,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        HCMv3.Workflow.Exporting.IWorkflowRequestLogExcelExporter requestLogExporter)
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
        _organizationUnitRepository = organizationUnitRepository;
        _workLocationRepository = workLocationRepository;
        _readLogRepository = readLogRepository;
        _delegationRepository = delegationRepository;
        _workflowRoleRepository = workflowRoleRepository;
        _delegationResolver = delegationResolver;
        _employeeSessionService = employeeSessionService;
        _workflowApprovalService = workflowApprovalService;
        _handlerResolver = handlerResolver;
        _requestMasterResolver = requestMasterResolver;
        _sysSettingsAccessor = sysSettingsAccessor;
        _requestLogExporter = requestLogExporter;
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
    /// The decision-rights side of the request: who may act, who has acted, who stands in for whom, and who
    /// has opened it.
    ///
    /// <para>
    /// <c>[HttpPost]</c> for the same NSwag reason as the two reads above.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<WorkflowParticipantsDto> GetParticipants(GetWorkflowParticipantsInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);
        var access = await GetAccessAsync(request);

        // Identical rule to GetRequestDetail, and deliberately the SAME code path. This endpoint names
        // people -- their photo, their job title, who covers for them -- so walking the id range here would
        // leak more about the organisation than the request itself does.
        EnsureCanView(access);

        var result = new WorkflowParticipantsDto
        {
            WorkflowRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            WorkflowStep = request.WorkflowStep,
            RequestStatus = request.RequestStatus
        };

        await FillCurrentWorkflowRoleAsync(result, request);

        result.Approvers = await BuildApproversAsync(request);
        result.Delegates = await BuildDelegatesAsync(result.Approvers);
        result.ReadLog = await BuildReadLogAsync(request.Id);

        await FillPeopleAsync(result);

        return result;
    }

    /// <summary>
    /// One participant's directory card.
    ///
    /// <para>
    /// <b>Two gates, not one.</b> The caller must be able to view the request -- the same
    /// <see cref="EnsureCanView"/> rule as everywhere else on this service -- AND the employee they are
    /// asking about must already be ON that request. Without the second gate this would be an employee
    /// lookup by id wearing a workflow id as a hat: anyone with one request of their own could walk the
    /// employee table and print the staff directory. With it, the endpoint can only ever describe somebody
    /// the request has already named to this caller.
    /// </para>
    ///
    /// <para>
    /// The card itself is work contact information only. See <see cref="WorkflowParticipantProfileDto"/>
    /// for what is excluded and why.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<WorkflowParticipantProfileDto> GetParticipantProfile(GetWorkflowParticipantProfileInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);
        var access = await GetAccessAsync(request);

        EnsureCanView(access);

        if (!await IsParticipantAsync(request, input.EmployeeId))
        {
            throw new AbpAuthorizationException(L("YouCannotViewThisWorkflowRequest"));
        }

        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == input.EmployeeId)
            .Select(e => new
            {
                e.Id,
                e.Code,
                e.FullNameEn,
                e.FullNameAr,
                e.Email,
                e.BusinessLandline,
                e.BusinessExtension,
                HasPhoto = e.PhotoPath != null
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            throw new UserFriendlyException(L("EmployeeNotFound"));
        }

        var result = new WorkflowParticipantProfileDto
        {
            EmployeeId = employee.Id,
            Code = employee.Code,
            NameEn = employee.FullNameEn,
            NameAr = employee.FullNameAr,
            WorkEmail = employee.Email,
            BusinessLandline = employee.BusinessLandline,
            BusinessExtension = employee.BusinessExtension,
            PhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null
        };

        // The assignment they hold TODAY -- same "most recent wins" rule the rest of this service uses,
        // because an older assignment describes a job they have already left.
        var assignment = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employee.Id)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new { a.JobTitleId, a.OrganizationUnitId, a.WorkLocationId })
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            return result;
        }

        if (assignment.JobTitleId.HasValue)
        {
            var jobTitle = await _jobTitleRepository.GetAll()
                .Where(t => t.Id == assignment.JobTitleId.Value)
                .Select(t => new { t.NameEn, t.NameAr })
                .FirstOrDefaultAsync();

            result.JobTitleEn = jobTitle?.NameEn;
            result.JobTitleAr = jobTitle?.NameAr;
        }

        if (assignment.OrganizationUnitId.HasValue)
        {
            result.Department = await _organizationUnitRepository.GetAll()
                .Where(o => o.Id == assignment.OrganizationUnitId.Value)
                .Select(o => o.DisplayName)
                .FirstOrDefaultAsync();
        }

        if (assignment.WorkLocationId.HasValue)
        {
            var location = await _workLocationRepository.GetAll()
                .Where(w => w.Id == assignment.WorkLocationId.Value)
                .Select(w => new { w.NameEn, w.NameAr })
                .FirstOrDefaultAsync();

            result.WorkLocationEn = location?.NameEn;
            result.WorkLocationAr = location?.NameAr;
        }

        return result;
    }

    /// <summary>
    /// Whether this employee appears anywhere on the request -- as its requester, an approver at any step,
    /// either side of a delegation touching an approver, or somebody who has opened it.
    ///
    /// <para>
    /// Built from the same three methods the panel itself uses, so the set of people whose card can be
    /// opened is exactly the set of names the panel can display. A person who is not on the panel has no
    /// card here, which is what keeps this from being a general employee lookup.
    /// </para>
    /// </summary>
    private async Task<bool> IsParticipantAsync(WorkflowRequest request, int employeeId)
    {
        if (request.EmployeeId == employeeId)
        {
            return true;
        }

        var approvers = await BuildApproversAsync(request);
        if (approvers.Any(a => a.Person.EmployeeId == employeeId))
        {
            return true;
        }

        var delegates = await BuildDelegatesAsync(approvers);
        if (delegates.Any(d => d.Person.EmployeeId == employeeId || d.DelegatedBy.EmployeeId == employeeId))
        {
            return true;
        }

        var readLog = await BuildReadLogAsync(request.Id);

        return readLog.Any(r => r.Person != null && r.Person.EmployeeId == employeeId);
    }

    /// <summary>
    /// The role the current step names, when the request can be routed at all. A request whose
    /// <c>RequestMaster</c> cannot be resolved still returns everything else: that request is precisely the
    /// one somebody opens this panel to diagnose, and failing the read would hide the evidence.
    /// </summary>
    private async Task FillCurrentWorkflowRoleAsync(WorkflowParticipantsDto result, WorkflowRequest request)
    {
        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);

            // One read for both facts about the current step. They are answered together because they are
            // read together: "who is this waiting on, and how many of them does it need".
            var step = await _requestDetailRepository.GetAll()
                .Where(d => d.RequestMasterId == master.Id && d.Step == request.WorkflowStep)
                .Select(d => new { d.WorkflowRoleId, d.ApprovalRequire })
                .FirstOrDefaultAsync();

            if (step == null)
            {
                return;
            }

            // Set before the role lookup can return early: a step with no workflow role still has an
            // approval rule, and the panel should say so.
            result.CurrentApprovalRequire = step.ApprovalRequire;

            var roleId = step.WorkflowRoleId;

            if (!roleId.HasValue)
            {
                return;
            }

            var role = await _workflowRoleRepository.GetAll()
                .Where(r => r.Id == roleId.Value)
                .Select(r => new { r.NameEn, r.NameAr })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return;
            }

            result.CurrentWorkflowRoleNameEn = role.NameEn;
            result.CurrentWorkflowRoleNameAr = role.NameAr;
        }
        catch (UserFriendlyException)
        {
            // Unroutable. Leave the role blank rather than refusing the whole panel.
        }
    }

    /// <summary>
    /// The action types that mean somebody <em>exercised approval authority</em>, and therefore that they
    /// were an approver of the step they did it at.
    ///
    /// <para>
    /// <c>Created</c> is excluded because its actor is the requester. <c>Read</c> and <c>Remarked</c> are
    /// excluded because looking at a request, or saying something about it, is not acting on it -- counting
    /// them would list every commenter as an approver.
    /// </para>
    /// </summary>
    private static readonly RequestActionType[] DecisionActionTypes =
    {
        RequestActionType.Approved,
        RequestActionType.Rejected,
        RequestActionType.SentBack,
        RequestActionType.SentBackToCreator
    };

    /// <summary>
    /// Everyone entitled to act, across every step the request has passed through.
    ///
    /// <para>
    /// <b>Still two queries, for a narrower reason than before.</b> <c>WorkFlowApprover</c> now records the
    /// step each row was assigned for and the engine no longer deletes the request's rows on every step
    /// change, so the table answers for every step of a request routed since that change -- including the
    /// people who were ENTITLED to approve a past step and did not, which is the part no reconstruction
    /// could ever recover.
    /// </para>
    ///
    /// <para>
    /// <c>RequestLog</c> is still read, for requests that were already in flight when the column arrived.
    /// Their earlier steps were deleted before there was anywhere to record them and only the action trail
    /// remembers who acted. Those rows keep saying so -- <c>ReconstructedFromLog</c> -- so the panel can go
    /// on warning that a past step lists whoever acted rather than whoever could have. New requests simply
    /// stop producing them.
    /// </para>
    /// </summary>
    private async Task<List<WorkflowApproverEntryDto>> BuildApproversAsync(WorkflowRequest request)
    {
        var entries = new List<WorkflowApproverEntryDto>();

        var live = await _approverRepository.GetAll()
            .Where(a => a.WorkflowRequestId == request.Id && a.ApproverId.HasValue)
            .Select(a => new { a.ApproverId, a.IsApproved, a.WorkflowStep })
            .ToListAsync();

        foreach (var row in live)
        {
            entries.Add(new WorkflowApproverEntryDto
            {
                Person = new WorkflowPersonDto { EmployeeId = row.ApproverId.Value },

                // The row's own step, not the request's. They are the same for the current step and differ
                // for every earlier one, which is the whole point of keeping the rows.
                WorkflowStep = row.WorkflowStep,
                IsApproved = row.IsApproved,
                Origin = row.WorkflowStep == request.WorkflowStep
                    ? WorkflowApproverOrigin.CurrentStep
                    : WorkflowApproverOrigin.PastStep
            });
        }

        var decisions = await _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == request.Id
                        && l.ActionById.HasValue
                        && DecisionActionTypes.Contains(l.ActionType))
            .Select(l => new { l.ActionById, l.ActionType, l.ActionDateTime, l.WorkflowStep })
            .ToListAsync();

        foreach (var group in decisions.GroupBy(d => new { d.ActionById, d.WorkflowStep }))
        {
            var employeeId = group.Key.ActionById.Value;
            var step = group.Key.WorkflowStep;

            // A stored row for the same person at the same step already says whether they approved, from
            // the engine's own state rather than from an inference. Skip rather than duplicate.
            //
            // Any stored origin counts, not just CurrentStep: past steps now have real rows too, and
            // testing only for CurrentStep would list every past approver twice -- once from the table and
            // once from the log.
            var alreadyLive = entries.Any(e => e.Origin != WorkflowApproverOrigin.ReconstructedFromLog
                                               && e.WorkflowStep == step
                                               && e.Person.EmployeeId == employeeId);

            if (alreadyLive)
            {
                continue;
            }

            var last = group.OrderByDescending(d => d.ActionDateTime).First();

            entries.Add(new WorkflowApproverEntryDto
            {
                Person = new WorkflowPersonDto { EmployeeId = employeeId },
                WorkflowStep = step,

                // Only an approval counts as approved. A step that was rejected or sent back must not read
                // as "has not got round to it yet" -- that is a different thing entirely.
                IsApproved = last.ActionType == RequestActionType.Approved,
                Origin = WorkflowApproverOrigin.ReconstructedFromLog,
                ActionType = last.ActionType,
                ActedAt = last.ActionDateTime
            });
        }

        await AddCreatorStepAsync(request, entries);
        await ApplyStepRulesAsync(request, entries);

        return entries
            .OrderBy(e => e.WorkflowStep)
            .ThenBy(e => e.Person.EmployeeId)
            .ToList();
    }

    /// <summary>
    /// Puts the person who RAISED the request at the head of the chain.
    ///
    /// <para>
    /// A creator step names who may submit, not who approves, so it has no approver row and the list used
    /// to start at step 2 -- which reads as a chain with its first link missing. The submitter is taken
    /// from the Created log entry rather than from the request's employee, because on a request raised by
    /// HR on somebody else's behalf those are two different people and the chain is about who acted.
    /// </para>
    /// </summary>
    private async Task AddCreatorStepAsync(WorkflowRequest request, List<WorkflowApproverEntryDto> entries)
    {
        var creatorSteps = await CreatorStepsAsync(request);

        if (creatorSteps.Count == 0)
        {
            return;
        }

        var created = await _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == request.Id
                        && l.ActionType == RequestActionType.Created
                        && l.ActionById.HasValue)
            .OrderBy(l => l.ActionDateTime)
            .Select(l => new { l.ActionById, l.ActionDateTime })
            .FirstOrDefaultAsync();

        if (created == null)
        {
            return;
        }

        entries.Add(new WorkflowApproverEntryDto
        {
            Person = new WorkflowPersonDto { EmployeeId = created.ActionById.Value },
            WorkflowStep = creatorSteps.Min(),

            // Not "approved". They submitted it; approving your own request is a different claim and one
            // the engine does not make.
            IsApproved = false,
            Origin = WorkflowApproverOrigin.CreatorStep,
            ActionType = RequestActionType.Created,
            ActedAt = created.ActionDateTime
        });
    }

    /// <summary>The steps of this request's chain that nominate nobody to approve.</summary>
    private async Task<List<int>> CreatorStepsAsync(WorkflowRequest request)
    {
        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);

            return await _requestDetailRepository.GetAll()
                .Where(d => d.RequestMasterId == master.Id
                            && d.ApproverPerson == WorkflowApproverPersonType.None)
                .Select(d => d.Step)
                .ToListAsync();
        }
        catch (UserFriendlyException)
        {
            // Unroutable. The panel still shows what it has rather than refusing the whole read.
            return new List<int>();
        }
    }

    /// <summary>
    /// Tags each row with its own step's approval rule, and marks the rows that rule has made moot.
    ///
    /// <para>
    /// The "not required" part is the one worth being careful about. On a step where any one approval is
    /// enough, the moment somebody approves, the others are not late -- they are simply not being asked.
    /// Showing them as Pending is a request for an action that will never come, which is how an approver
    /// ends up chasing a colleague for nothing.
    /// </para>
    /// </summary>
    private async Task ApplyStepRulesAsync(WorkflowRequest request, List<WorkflowApproverEntryDto> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        Dictionary<int, ApprovalRequire> rules;

        try
        {
            var master = await _requestMasterResolver.ResolveAsync(request);

            rules = await _requestDetailRepository.GetAll()
                .Where(d => d.RequestMasterId == master.Id)
                .Select(d => new { d.Step, d.ApprovalRequire })
                .ToDictionaryAsync(d => d.Step, d => d.ApprovalRequire);
        }
        catch (UserFriendlyException)
        {
            // Unroutable: no rule to state, so the panel states none rather than guessing one.
            return;
        }

        foreach (var group in entries.GroupBy(e => e.WorkflowStep))
        {
            if (!rules.TryGetValue(group.Key, out var rule))
            {
                continue;
            }

            var rows = group.ToList();

            foreach (var entry in rows)
            {
                entry.ApprovalRequire = rule;
            }

            // Only Single can make anybody redundant. Under All and Parallel every approver is still wanted
            // however many have already acted.
            if (rule != ApprovalRequire.Single || !rows.Any(r => r.IsApproved))
            {
                continue;
            }

            foreach (var entry in rows.Where(r => !r.IsApproved && r.Origin != WorkflowApproverOrigin.CreatorStep))
            {
                entry.IsNotRequired = true;
            }
        }
    }

    /// <summary>
    /// Active delegations touching any of this request's approvers, in both directions.
    ///
    /// <para>
    /// <b>Both directions, because HCMv3 SUBSTITUTES where legacy ADDED.</b>
    /// <c>WorkflowDelegationResolver.ApplySubstitution</c> replaces a delegator with their stand-in when the
    /// step is routed, so by the time a <c>WorkFlowApprover</c> row exists the delegator is already gone
    /// from it. Listing only "delegations FROM the approvers", as legacy's tab did, would therefore be
    /// empty on exactly the requests where a delegation is doing something -- and would leave the reader
    /// with an approver list containing a name the org chart does not explain. The reverse direction is
    /// what answers "why is Sara on this?".
    /// </para>
    /// </summary>
    private async Task<List<WorkflowDelegateEntryDto>> BuildDelegatesAsync(List<WorkflowApproverEntryDto> approvers)
    {
        var result = new List<WorkflowDelegateEntryDto>();

        var approverIds = approvers.Select(a => a.Person.EmployeeId).Distinct().ToList();
        if (approverIds.Count == 0)
        {
            return result;
        }

        // Same day boundary as WorkflowDelegationResolver, which is the authority on "active today".
        var today = DateTime.Now.Date;

        var rows = await _delegationRepository.GetAll()
            .Where(d => d.DelegateToId != d.EmployeeId
                        && d.StartDate.Date <= today
                        && d.EndDate.Date >= today
                        && (approverIds.Contains(d.EmployeeId) || approverIds.Contains(d.DelegateToId)))
            .Select(d => new { d.EmployeeId, d.DelegateToId, d.StartDate, d.EndDate })
            .ToListAsync();

        foreach (var row in rows)
        {
            var relation = approverIds.Contains(row.EmployeeId)
                ? WorkflowDelegationRelation.ApproverDelegatedOut
                : WorkflowDelegationRelation.ApproverIsStandingIn;

            // For a delegation OUT, show only the cover the router would actually honour. Overlapping rows
            // are possible, and ResolveDelegateFor is the single place that decides which one wins (and
            // that drops a stand-in who has since left the company). Naming a colleague here whom the
            // engine would ignore is worse than naming nobody -- it tells the approver they are covered
            // when they are not.
            if (relation == WorkflowDelegationRelation.ApproverDelegatedOut
                && _delegationResolver.ResolveDelegateFor(row.EmployeeId) != row.DelegateToId)
            {
                continue;
            }

            result.Add(new WorkflowDelegateEntryDto
            {
                Person = new WorkflowPersonDto { EmployeeId = row.DelegateToId },
                DelegatedBy = new WorkflowPersonDto { EmployeeId = row.EmployeeId },
                Relation = relation,
                StartDate = row.StartDate,
                EndDate = row.EndDate
            });
        }

        return result
            .OrderBy(d => d.Relation)
            .ThenBy(d => d.Person.EmployeeId)
            .ToList();
    }

    /// <summary>
    /// Who has opened this request, newest first.
    ///
    /// <para>
    /// <b><c>WFReadLog</c> is the source, not the <c>RequestLog</c> Read rows.</b>
    /// <c>WorkflowApprovalService.MarkAsReadAsync</c> writes both in one transaction, so they carry the
    /// same events -- but <c>WFReadLog</c> is the table the feature is named for, the one
    /// <c>RequestDetail.EnableReadLog</c> switches on, and the one legacy's Read Log tab read. Its
    /// <c>User</c> column holds the acting employee's id as a string, which is what the resolution below
    /// turns back into a person; an id we can no longer match leaves the raw value to be shown instead of
    /// a blank row.
    /// </para>
    /// </summary>
    private async Task<List<WorkflowReadLogEntryDto>> BuildReadLogAsync(int requestId)
    {
        var rows = await _readLogRepository.GetAll()
            .Where(r => r.WorkflowRequestId == requestId)
            .Select(r => new { r.ReadDateTime, r.User })
            .ToListAsync();

        return rows
            .OrderByDescending(r => r.ReadDateTime)
            .Select(r => new WorkflowReadLogEntryDto
            {
                ReadAt = r.ReadDateTime,
                User = r.User,
                Person = int.TryParse(r.User, NumberStyles.Integer, CultureInfo.InvariantCulture, out var employeeId)
                    ? new WorkflowPersonDto { EmployeeId = employeeId }
                    : null
            })
            .ToList();
    }

    /// <summary>
    /// Fills every person stub on the panel in one pass, so a request with six approvers and a dozen reads
    /// costs two queries rather than forty.
    /// </summary>
    private async Task FillPeopleAsync(WorkflowParticipantsDto result)
    {
        var stubs = result.Approvers.Select(a => a.Person)
            .Concat(result.Delegates.Select(d => d.Person))
            .Concat(result.Delegates.Select(d => d.DelegatedBy))
            .Concat(result.ReadLog.Select(r => r.Person))
            .Where(p => p != null)
            .ToList();

        var employeeIds = stubs.Select(p => p.EmployeeId).Distinct().ToList();
        var people = await LoadPeopleAsync(employeeIds);

        foreach (var stub in stubs)
        {
            if (!people.TryGetValue(stub.EmployeeId, out var loaded))
            {
                continue;
            }

            stub.Code = loaded.Code;
            stub.NameEn = loaded.NameEn;
            stub.NameAr = loaded.NameAr;
            stub.JobTitleEn = loaded.JobTitleEn;
            stub.JobTitleAr = loaded.JobTitleAr;
            stub.PhotoUrl = loaded.PhotoUrl;
        }

        // A read-log id that matched no employee is not a person, and leaving a nameless stub would render
        // as an empty row. Dropping it lets the client fall back to the raw stored value.
        foreach (var entry in result.ReadLog)
        {
            if (entry.Person != null && !people.ContainsKey(entry.Person.EmployeeId))
            {
                entry.Person = null;
            }
        }
    }

    private async Task<Dictionary<int, WorkflowPersonDto>> LoadPeopleAsync(List<int> employeeIds)
    {
        var map = new Dictionary<int, WorkflowPersonDto>();

        if (employeeIds.Count == 0)
        {
            return map;
        }

        var employees = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, HasPhoto = e.PhotoPath != null })
            .ToListAsync();

        foreach (var employee in employees)
        {
            map[employee.Id] = new WorkflowPersonDto
            {
                EmployeeId = employee.Id,
                Code = employee.Code,
                NameEn = employee.FullNameEn,
                NameAr = employee.FullNameAr,
                PhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null
            };
        }

        // The title each of them holds TODAY, same rule FillRequesterAsync uses -- most recent assignment
        // wins, because an older one describes a job they have already left. Grouped in memory after one
        // query rather than a top-1 query per person.
        var assignments = await (from assignment in _orgAssignmentRepository.GetAll()
                                 join title in _jobTitleRepository.GetAll()
                                     on assignment.JobTitleId equals (int?)title.Id
                                 where employeeIds.Contains(assignment.EmployeeId)
                                 select new { assignment.EmployeeId, assignment.StartDate, title.NameEn, title.NameAr })
            .ToListAsync();

        foreach (var group in assignments.GroupBy(a => a.EmployeeId))
        {
            if (!map.TryGetValue(group.Key, out var person))
            {
                continue;
            }

            var current = group.OrderByDescending(a => a.StartDate).First();

            person.JobTitleEn = current.NameEn;
            person.JobTitleAr = current.NameAr;
        }

        return map;
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
                new WorkflowDetailFieldDto { Name = WorkflowDetailFieldNames.RequestNumber, Label = L("RequestNumber"), Value = request.RequestNumber, Kind = WorkflowDetailFieldKind.Text },
                new WorkflowDetailFieldDto { Name = WorkflowDetailFieldNames.SubmittedOn, Label = L("SubmittedOn"), Value = request.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), Kind = WorkflowDetailFieldKind.DateTime },
                new WorkflowDetailFieldDto { Name = WorkflowDetailFieldNames.Summary, Label = L("Summary"), Value = CurrentSummary(request), Kind = WorkflowDetailFieldKind.LongText }
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
        // type's own work. Detected on the stable NAMES rather than on the labels, which are localized and
        // would stop matching in Arabic.
        result.IsDefaultProjection = detail.Fields.Count <= 3
                                     && detail.Fields.Any(f => f.Name == WorkflowDetailFieldNames.Summary);

        result.SubmissionComment = detail.SubmissionComment;
        result.Fields = detail.Fields
            .Select(f => new WorkflowDetailFieldDto { Name = f.Name, Label = f.Label, Value = f.Value, Kind = f.Kind })
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
                                                      && a.WorkflowStep == request.WorkflowStep
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
                .Select(d => new { d.CanReject, d.CanSendBack, d.IsCommentRequire })
                .FirstOrDefaultAsync();

            var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
            var sendBackToCreator = workflowSettings != null && workflowSettings.SendBackToCreator;

            result.CanReject = detail?.CanReject ?? false;
            result.CanSendBack = (detail?.CanSendBack ?? false) && sendBackToCreator;
            result.IsCommentRequired = detail?.IsCommentRequire ?? false;
        }
        catch (UserFriendlyException)
        {
            result.CanReject = false;
            result.CanSendBack = false;

            // Not required, because we could not read the step that would have required it. The engine is
            // still the authority and will refuse the decision if it disagrees -- this flag only decides
            // whether the screen warns first.
            result.IsCommentRequired = false;
        }
    }

    private string CurrentSummary(WorkflowRequest request)
    {
        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        return isArabic
            ? (request.SummaryAr.IsNullOrWhiteSpace() ? request.SummaryEn : request.SummaryAr)
            : request.SummaryEn;
    }

    /// <summary>
    /// <c>[HttpPost]</c> for the reason given on <see cref="GetActivityFeed"/>: a <c>Get*</c> method with a
    /// custom input DTO is emitted by NSwag as loose query parameters and the DTO class never generated.
    /// </summary>
    [HttpPost]
    public async Task<List<WorkflowRequestLogEntryDto>> GetRequestLog(GetWorkflowRequestLogInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);

        EnsureCanView(await GetAccessAsync(request));

        return await ReadRequestLogAsync(request.Id);
    }

    [HttpPost]
    public async Task<FileDto> ExportRequestLogToExcel(GetWorkflowRequestLogInput input)
    {
        var request = await _requestRepository.GetAsync(input.WorkflowRequestId);

        // The same check the grid makes, not a weaker one. An export that skipped it would be a way to read
        // any request in the tenant by guessing an id.
        EnsureCanView(await GetAccessAsync(request));

        var entries = await ReadRequestLogAsync(request.Id);

        return await _requestLogExporter.ExportToFile(
            request.RequestNumber,
            entries,
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar");
    }

    /// <summary>
    /// The rows themselves, oldest first -- a log is read forwards, unlike the conversation, which shows
    /// the newest post at the top because that is the one you came back for.
    /// </summary>
    private async Task<List<WorkflowRequestLogEntryDto>> ReadRequestLogAsync(int requestId)
    {
        var logs = await _requestLogRepository.GetAll()
            .Where(l => l.WorkflowRequestId == requestId)
            .OrderBy(l => l.ActionDateTime)
            .ThenBy(l => l.Id)
            .Select(l => new WorkflowRequestLogEntryDto
            {
                Id = l.Id,
                ActionByEmployeeId = l.ActionById,
                ActionDateTime = l.ActionDateTime,
                ActionType = l.ActionType,
                WorkflowStep = l.WorkflowStep,
                ActionDays = l.ActionDays,
                Platform = l.Platform,
                Comment = l.RequestActionComment
            })
            .ToListAsync();

        await FillLogActorsAsync(logs);

        return logs;
    }

    /// <summary>
    /// Names and photos for the grid, resolved in one query rather than per row -- the same approach
    /// <see cref="FillActorsAsync"/> takes for the feed, against a different DTO.
    /// </summary>
    private async Task FillLogActorsAsync(List<WorkflowRequestLogEntryDto> entries)
    {
        var employeeIds = entries
            .Where(e => e.ActionByEmployeeId.HasValue)
            .Select(e => e.ActionByEmployeeId.Value)
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
                e.Code,
                e.FullNameEn,
                e.FullNameAr,
                HasPhoto = e.PhotoPath != null
            })
            .ToListAsync();

        var byId = employees.ToDictionary(e => e.Id);

        foreach (var entry in entries.Where(e => e.ActionByEmployeeId.HasValue))
        {
            if (!byId.TryGetValue(entry.ActionByEmployeeId.Value, out var employee))
            {
                continue;
            }

            entry.ActionByCode = employee.Code;
            entry.ActionByNameEn = employee.FullNameEn;
            entry.ActionByNameAr = employee.FullNameAr;
            entry.ActionByPhotoUrl = employee.HasPhoto ? "/api/employee-photo/" + employee.Id : null;
        }
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
                e.Code,
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
            activity.ActorCode = employee.Code;
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
