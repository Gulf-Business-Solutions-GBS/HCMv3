using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.Dashboards.Dto;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyLeavePlanAppService"/>
/// <remarks>
/// Built on <see cref="MyTransactionAppServiceBase{TEntity}"/> like every other self-service request, with the
/// "draft or sent back" helpers, so a plan an approver sends back reopens in the same form and is resubmitted
/// under the same request number.
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyLeavePlans)]
public class MyLeavePlanAppService : MyTransactionAppServiceBase<LeavePlan>, IMyLeavePlanAppService
{
    private readonly ILeavePlanCalculator _calculator;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly LeavePlannerReader _plannerReader;

    public MyLeavePlanAppService(
        IRepository<LeavePlan> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        ILeavePlanCalculator calculator,
        LeavePlannerReader plannerReader)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _calculator = calculator;
        _employeeScopeProvider = employeeScopeProvider;
        _plannerReader = plannerReader;
    }

    protected override string RequestType => LeavePlanWorkflowTransactionHandler.LeavePlanRequestType;

    /// <summary>Display tag on the request number, e.g. LPL-000042.</summary>
    protected override string RequestNumberPrefix => "LPL";

    [HttpPost]
    public async Task<PagedResultDto<MyLeavePlanDto>> GetMyLeavePlans(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = Clock.Now.Date;

            // A plan is about leave still to come: by default the ones not over yet, plus anything still with an
            // approver or still the employee's to finish -- a draft may have no dates at all.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    p => p.EndDate >= today
                         || p.WorkflowStatus == RequestStatus.Open
                         || p.WorkflowStatus == RequestStatus.Draft);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var requests = await GetOwnRequestsAsync(items);

            return new PagedResultDto<MyLeavePlanDto>(totalCount, items.Select(p => ToDto(p, requests)).ToList());
        }
    }

    public async Task<LeavePlanPreviewDto> PreviewMyLeavePlan(PreviewLeavePlanInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        var start = input?.StartDate?.Date ?? default;
        var end = input?.EndDate?.Date ?? default;

        // The projection counts approved leave only -- pending leave does not reserve quota. The calculator
        // makes sure of that itself, whatever filter state it is called in.
        return new LeavePlanPreviewDto
        {
            PlannedDays = _calculator.GetPlannedDays(start, end),
            ProjectedLeaveBalance = await _calculator.GetProjectedLeaveBalanceAsync(employeeId, start),
            LeaveDays = await _calculator.GetAnnualLeaveDaysAsync(employeeId, start, end)
        };
    }

    public async Task<MyRequestSubmissionDto> RequestLeavePlan(RequestLeavePlanInput input)
    {
        // Refused before a row exists, rather than created and rolled back. The handler applies the same rule
        // at submit for anything that reaches the engine another way.
        _calculator.Validate(input.StartDate, input.EndDate);

        var employeeId = await GetMyEmployeeIdAsync();

        var plan = new LeavePlan
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Comments = input.Comments
        };

        // Taken now as well as at submit: with workflow switched off for plans no handler runs, and the row is
        // effective as saved.
        await _calculator.RefreshFiguresAsync(plan);

        // No sub-type: legacy routed every leave plan through request code "14" alike.
        return await CreateAndSubmitAsync(_ => plan, null);
    }

    public async Task<MyRequestSubmissionDto> SaveMyLeavePlanDraft(SaveLeavePlanDraftInput input)
    {
        // Nothing validated: a draft is what is saved because it cannot be finished yet.
        if (input.Id.HasValue)
        {
            var figures = await FiguresForAsync(input);

            await UpdateEditableAsync(input.Id.Value, plan => Apply(input, plan, figures));

            return new MyRequestSubmissionDto
            {
                TransactionId = input.Id.Value,
                SubmittedForApproval = false,
                SavedAsDraft = true
            };
        }

        var newFigures = await FiguresForAsync(input);

        return await SaveDraftAsync(employeeId =>
        {
            var plan = new LeavePlan
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId
            };

            Apply(input, plan, newFigures);

            return plan;
        });
    }

    public async Task<MyLeavePlanDto> GetMyLeavePlanDraftForEdit(EntityDto input)
    {
        var plan = await GetMyEditableAsync(input.Id);

        return ToDto(plan, await GetOwnRequestsAsync(new[] { plan }));
    }

    public Task<MyRequestSubmissionDto> SubmitMyLeavePlanDraft(EntityDto input)
    {
        return SubmitEditableAsync(input.Id, async plan =>
        {
            // The same rule the one-step request applies, against what the row now holds. The handler applies
            // it too, but with workflow off for plans no handler runs.
            _calculator.Validate(plan.StartDate, plan.EndDate);

            await _calculator.RefreshFiguresAsync(plan);

            return (int?)null;
        });
    }

    public Task CancelMyLeavePlan(EntityDto input) => CancelAsync(input.Id);

    [HttpPost]
    public async Task<LeavePlannerDto> GetMyTeamLeavePlanner(GetLeavePlannerInput input)
    {
        // Legacy GetSchedulerData: the employee's own plans and leave, and those of everybody whose manager
        // they are (EmployeeManager, one level). EmployeeScope.Team is exactly that pair.
        var scope = await _employeeScopeProvider.GetScopeAsync(EmployeeScope.Team);

        // Team is never unrestricted; an unlinked account resolves to empty, which shows nobody.
        IReadOnlyCollection<int> ids = scope.IsUnrestricted ? null : scope.EmployeeIds;

        var planner = _plannerReader.Read(input, ids, Clock.Now.Date, L("LeaveDashboard_Unassigned"));

        // The team is the team: no unit filter to offer, and nothing to say about scope.
        planner.OrgUnitOptions = new List<DashboardOrgUnitOptionDto>();
        planner.Scope = null;

        return planner;
    }

    /// <summary>Planned days and projected balance for the dates a draft is being saved with.</summary>
    private async Task<(double PlannedDays, double? Projected)> FiguresForAsync(SaveLeavePlanDraftInput input)
    {
        var start = input.StartDate?.Date ?? default;
        var end = input.EndDate?.Date ?? default;

        var employeeId = await GetMyEmployeeIdAsync();

        return (_calculator.GetPlannedDays(start, end), await _calculator.GetProjectedLeaveBalanceAsync(employeeId, start));
    }

    /// <summary>
    /// Copies the form onto the plan. Missing dates stay unset rather than becoming today's: a draft with no
    /// date must stay one, and a guessed date could be submitted for a day nobody chose.
    /// </summary>
    private static void Apply(SaveLeavePlanDraftInput input, LeavePlan plan, (double PlannedDays, double? Projected) figures)
    {
        plan.StartDate = input.StartDate?.Date ?? default;
        plan.EndDate = input.EndDate?.Date ?? default;
        plan.Comments = input.Comments;
        plan.PlannedDays = figures.PlannedDays;
        plan.ProjectedLeaveBalance = figures.Projected;
    }

    private static MyLeavePlanDto ToDto(LeavePlan entity, IReadOnlyDictionary<int, WorkflowRequestInfo> requests)
    {
        var isDraft = IsDraft(entity);
        var isSentBack = IsSentBack(entity, requests);

        return new MyLeavePlanDto
        {
            Id = entity.Id,
            RequestNumber = RequestNumberOf(entity, requests),
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            PlannedDays = entity.PlannedDays,
            ProjectedLeaveBalance = entity.ProjectedLeaveBalance,
            Comments = entity.Comments,
            CreationTime = entity.CreationTime,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            IsDraft = isDraft,
            IsSentBack = isSentBack,
            CanEdit = isDraft || isSentBack
        };
    }
}
