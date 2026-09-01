using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.TimeManagement.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TimeManagement.Workflow;

/// <summary>
/// Workflow handler for a missing-punch request -- the port of legacy
/// <c>HCM10.Module.BusinessObjects.Workflow.Leave.SkipPunchRequest</c>.
///
/// <para>
/// <b>What a skip punch actually is, and why it targets <see cref="TimeEvent"/>.</b> An employee worked the
/// day but the clock has no record of their arrival or departure -- the badge failed, the device was down,
/// they were at a client site. What is missing is a raw clock event, so what the request creates is a raw
/// clock event; the Time Engine then recomputes the day from the punches exactly as it would have if the
/// punch had been made at the time. This is deliberately NOT routed through <c>EmployeeAttendance</c>, which
/// is a paid attendance-type transaction feeding payroll directly: putting a missing punch there would
/// fabricate an attendance day the employee may not be entitled to, instead of letting the engine work out
/// what the day was worth.
/// </para>
///
/// <para>
/// <b>The legacy shape that is gone.</b> Legacy did this in <c>SkipPunchRequest.OnSaving</c>: a separate
/// request table, and a block that -- on every save, when the status happened to be Approved -- inserted a
/// <c>TimeEvent</c>. Here the <c>TimeEvent</c> row IS the request from the moment it is raised; it simply
/// carries a <see cref="IWorkflowEnabledEntity.WorkflowStatus"/> that keeps it behind the
/// <c>ApprovedOnly</c> filter, so the engine cannot see it until somebody grants it. Nothing is inserted on
/// approval -- approval only makes the existing row visible and, if the employee left the time blank, fills
/// it in.
/// </para>
/// </summary>
public class TimeEventWorkflowTransactionHandler : WorkflowTransactionHandlerBase<TimeEvent>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for missing punches.</summary>
    public const string TimeEventRequestType = "TimeEvent";

    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IPunchDutyResolver _punchDutyResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TimeEventWorkflowTransactionHandler(
        IRepository<TimeEvent> repository,
        IRepository<TransType> transTypeRepository,
        IPunchDutyResolver punchDutyResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _transTypeRepository = transTypeRepository;
        _punchDutyResolver = punchDutyResolver;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => TimeEventRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var timeEvent = await GetTargetAsync(context);
        if (timeEvent == null)
        {
            return;
        }

        if (timeEvent.TransDate == default)
        {
            throw new UserFriendlyException(L("PunchDateIsRequired"));
        }

        if (timeEvent.TransTypeId <= 0)
        {
            throw new UserFriendlyException(L("TransTypeCannotBeBlank"));
        }

        var dayStart = timeEvent.TransDate.Date;
        var dayEnd = dayStart.AddDays(1);

        // The filter has to come off for this check to mean anything. A punch that has been asked for but
        // not yet granted is Open, which is exactly what ApprovedOnly hides -- leave it on and an employee
        // can file the same missing punch every morning until somebody approves all of them, and the day
        // ends up with several arrivals.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = Repository.GetAll()
                .Any(e => e.Id != timeEvent.Id
                          && e.EmployeeId == timeEvent.EmployeeId
                          && e.TransTypeId == timeEvent.TransTypeId
                          && e.TransDate >= dayStart
                          && e.TransDate < dayEnd
                          && (e.WorkflowStatus == null
                              || e.WorkflowStatus == RequestStatus.Approved
                              || e.WorkflowStatus == RequestStatus.Closed
                              || e.WorkflowStatus == RequestStatus.Open));

            if (duplicate)
            {
                throw new UserFriendlyException(L("APunchOfThisTypeAlreadyExistsForThatDay",
                    dayStart.ToString("dd MMM yyyy")));
            }
        }

        // No duty means the employee was not due to work that day at all, and there is nothing for a punch
        // to belong to -- nor, if the time was left blank, anything to derive it from. Legacy failed at
        // approval time with "duty period is not specified"; refusing at submit tells the employee before an
        // approver spends time on it.
        var duty = await _punchDutyResolver.TryResolveDutyAsync(timeEvent.EmployeeId, dayStart);
        if (duty == null)
        {
            throw new UserFriendlyException(L("NoDutyIsScheduledForThatDay", dayStart.ToString("dd MMM yyyy")));
        }
    }

    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            // Amendments leave the row to ApplyAmendmentAsync -- see the base class remarks.
            return;
        }

        var timeEvent = await GetTargetAsync(context);
        if (timeEvent == null || timeEvent.TransTime != default)
        {
            return;
        }

        // The employee asked for "an in-punch on the 12th" without saying when. The honest answer is the
        // time they were rostered to arrive, so that is what is stamped -- and TransSource.System records
        // that this time was derived rather than observed, which is the distinction an auditor asking why
        // the punch is exactly on the shift boundary needs.
        var derived = await _punchDutyResolver.ResolveDefaultPunchTimeAsync(
            timeEvent.EmployeeId, timeEvent.TransTypeId, timeEvent.TransDate);

        if (derived == null)
        {
            throw new UserFriendlyException(L("NoDutyIsScheduledForThatDay",
                timeEvent.TransDate.ToString("dd MMM yyyy")));
        }

        timeEvent.TransTime = timeEvent.TransDate.Date.Add(derived.Value);
        timeEvent.TransSource = TransSource.System;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(timeEvent);
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var timeEvent = await GetTargetAsync(context);
        if (timeEvent == null)
        {
            return null;
        }

        var date = timeEvent.TransDate.ToString("dd MMM yyyy");

        var transType = _transTypeRepository.GetAll()
            .FirstOrDefault(t => t.Id == timeEvent.TransTypeId);

        // The two types the engine itself recognises get the words an approver thinks in; anything else a
        // tenant has added is named as they named it, rather than being forced into In or Out.
        var code = transType?.Code?.Trim();
        string en, ar;

        if (string.Equals(code, TransTypeCodes.ClockIn, StringComparison.OrdinalIgnoreCase))
        {
            en = "In-punch";
            ar = "بصمة حضور";
        }
        else if (string.Equals(code, TransTypeCodes.ClockOut, StringComparison.OrdinalIgnoreCase))
        {
            en = "Out-punch";
            ar = "بصمة انصراف";
        }
        else
        {
            en = transType?.NameEn ?? "punch";
            ar = transType?.NameAr ?? "بصمة";
        }

        return new WorkflowSummary(
            $"Missing {en}, {date}",
            $"{ar} مفقودة، {date}");
    }
}
