using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyPunchAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyPunches)]
public class MyPunchAppService : MyTransactionAppServiceBase<TimeEvent>, IMyPunchAppService
{
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IPunchDutyResolver _punchDutyResolver;

    public MyPunchAppService(
        IRepository<TimeEvent> repository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<TransType> transTypeRepository,
        IPunchDutyResolver punchDutyResolver,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _transTypeRepository = transTypeRepository;
        _punchDutyResolver = punchDutyResolver;
    }

    protected override string RequestType => TimeEventWorkflowTransactionHandler.TimeEventRequestType;

    protected override string RequestNumberPrefix => "PCH";

    [HttpPost]
    public async Task<PagedResultDto<MyPunchRequestDto>> GetMyPunches(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // Only the punches this employee ASKED for, never the thousands their badge produced. A raw
            // punch log is a different screen answering a different question; this list answers "where has
            // my missing-punch request got to", and an imported or device punch has no answer to give.
            // TransSource.ESS is in the predicate because a row created while workflow is switched off for
            // this type has neither a request nor a status -- without it, the punch would vanish from the
            // very screen that created it.
            var query = MyRowsQuery(employeeId)
                .Where(e => e.WorkflowRequestId != null
                            || e.WorkflowStatus != null
                            || e.TransSource == TransSource.ESS)
                .WhereIf(!input.IncludeHistory,
                    e => e.WorkflowStatus == RequestStatus.Draft || e.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var transTypes = await LoadTransTypesAsync(items);

            return new PagedResultDto<MyPunchRequestDto>(
                totalCount,
                items.Select(e => ToDto(e, transTypes)).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestMissingPunch(RequestMissingPunchInput input)
    {
        if (input.TransDate.Date > DateTime.Today)
        {
            // A missing punch is a claim about something that already happened. Allowing a future date would
            // let an employee pre-book an arrival they have not made.
            throw new UserFriendlyException(L("APunchCannotBeRequestedForAFutureDate"));
        }

        var result = await CreateAndSubmitAsync(
            employeeId => new TimeEvent
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                TransTypeId = input.TransTypeId,

                // ActualTransType records what the device reported, and no device reported anything here.
                // Left null on purpose rather than mirroring TransTypeId, so a derived punch stays
                // distinguishable from a real one in the punch log.
                ActualTransTypeId = null,
                TransDate = input.TransDate.Date,

                // Left at default when the employee did not say -- the handler fills it from the duty they
                // were rostered on, but only once the request is granted, so an unapproved row never carries
                // a time that looks authoritative.
                TransTime = input.TransTime.HasValue
                    ? input.TransDate.Date.Add(input.TransTime.Value.TimeOfDay)
                    : default,
                TransSource = TransSource.ESS
            },
            // Routed on the punch type: the catalog row's SubTypeSource is TransType, so a tenant can send
            // missing arrivals down a different chain from missing departures.
            input.TransTypeId);

        if (!result.SubmittedForApproval)
        {
            // Workflow is off for this type, so no handler will ever run against this row -- and it is
            // already effective. The one thing the handler would have done on approval therefore has to
            // happen now, or a form submitted without a time becomes a real punch at midnight and the Time
            // Engine treats it as an arrival five hours early.
            await FillMissingTimeFromDutyAsync(result.TransactionId);
        }

        return result;
    }

    public Task CancelMyPunch(EntityDto input) => CancelAsync(input.Id);

    /// <summary>
    /// Stamps the duty's start (or end) on a punch whose time the employee left blank.
    ///
    /// <para>
    /// Only for the workflow-off path; when workflow is on this is
    /// <c>TimeEventWorkflowTransactionHandler.OnApprovedAsync</c>'s job and must not happen a moment sooner,
    /// because a time filled in at submit looks like something the employee stated. <c>TransSource</c> is
    /// left as <c>ESS</c> here rather than being moved to <c>System</c>: nothing approved this row, and ESS
    /// is still where it came from.
    /// </para>
    /// </summary>
    private async Task FillMissingTimeFromDutyAsync(int transactionId)
    {
        var entity = await Repository.GetAsync(transactionId);

        if (entity.TransTime != default)
        {
            return;
        }

        var derived = await _punchDutyResolver.ResolveDefaultPunchTimeAsync(
            entity.EmployeeId, entity.TransTypeId, entity.TransDate);

        if (derived == null)
        {
            // Refusing rolls the row back with the unit of work, which is the point: a punch on a day the
            // employee was not rostered has no time it could honestly be given.
            throw new UserFriendlyException(L("NoDutyIsScheduledForThatDay",
                entity.TransDate.ToString("dd MMM yyyy")));
        }

        entity.TransTime = entity.TransDate.Date.Add(derived.Value);

        await Repository.UpdateAsync(entity);
    }

    private async Task<Dictionary<int, TransType>> LoadTransTypesAsync(List<TimeEvent> items)
    {
        var ids = items.Select(e => e.TransTypeId).Distinct().ToList();

        var types = await _transTypeRepository.GetAll()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        return types.ToDictionary(t => t.Id);
    }

    private static MyPunchRequestDto ToDto(TimeEvent entity, IReadOnlyDictionary<int, TransType> transTypes)
    {
        transTypes.TryGetValue(entity.TransTypeId, out var transType);

        return new MyPunchRequestDto
        {
            Id = entity.Id,
            TransTypeId = entity.TransTypeId,
            TransTypeNameEn = transType?.NameEn,
            TransTypeNameAr = transType?.NameAr,
            IsClockIn = string.Equals(transType?.Code?.Trim(), TransTypeCodes.ClockIn, StringComparison.OrdinalIgnoreCase),
            TransDate = entity.TransDate,

            // Default means "not decided yet", and the screen has to be able to tell that apart from
            // midnight.
            TransTime = entity.TransTime == default ? (DateTime?)null : entity.TransTime,
            TransSource = entity.TransSource,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
