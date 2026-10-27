using System;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for leave plans (<see cref="LeavePlan"/>, legacy <c>LeavePlanner</c>, request code "14").
///
/// <para>
/// <b>No <c>OnApprovedAsync</c> override, on purpose.</b> An approved plan books no leave and charges no
/// quota. Legacy's approval block in <c>LeavePlanner.OnSaving</c> is commented out and was copied from the
/// asset request; reviving it would invent a side effect the business never had. The base's status stamp is
/// the whole of what approval means: the plan now shows in the Leave Planner as approved.
/// </para>
///
/// <para>
/// <b>No overlap rule</b>, also as legacy: neither <c>LeavePlanner.PerformValidations</c> nor the legacy
/// workflow service looked at other plans or at booked leave. Two plans over the same weeks, or a plan over
/// leave already approved, are both accepted -- the planner shows them side by side.
/// </para>
///
/// <para>
/// Named <c>...WorkflowTransactionHandler</c> because Castle binds by that suffix; any other name registers as
/// itself only and the type would route with no validation at all.
/// </para>
/// </summary>
public class LeavePlanWorkflowTransactionHandler : WorkflowTransactionHandlerBase<LeavePlan>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for leave plans.</summary>
    public const string LeavePlanRequestType = "LeavePlan";

    private readonly ILeavePlanCalculator _calculator;

    public LeavePlanWorkflowTransactionHandler(
        IRepository<LeavePlan> repository,
        IUnitOfWorkManager unitOfWorkManager,
        ILeavePlanCalculator calculator)
        : base(repository, unitOfWorkManager)
    {
        _calculator = calculator;
    }

    public override string RequestType => LeavePlanRequestType;

    /// <summary>Legacy <c>LeavePlanner.PerformValidations</c>: both dates, and the end not before the start.</summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return;
        }

        _calculator.Validate(plan.StartDate, plan.EndDate);
    }

    /// <summary>
    /// Stamps the status, then takes the figures again from the dates as submitted -- legacy assigned
    /// <c>_plannedDays</c> at exactly this point, so what the approver reads is what was sent rather than what
    /// a draft held when it was first saved.
    /// </summary>
    public override async Task OnSubmittedAsync(WorkflowTransactionContext context)
    {
        await base.OnSubmittedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return;
        }

        await _calculator.RefreshFiguresAsync(plan);
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return null;
        }

        var start = plan.StartDate.ToString("dd MMM yyyy");
        var end = plan.EndDate.ToString("dd MMM yyyy");
        var days = _calculator.GetPlannedDays(plan.StartDate, plan.EndDate);

        return new WorkflowSummary(
            $"Leave plan {start} - {end} ({days} days)",
            $"خطة إجازة {start} - {end} ({days} يوم)");
    }

    /// <summary>
    /// Every field the leave plan form shows, under the plan's own property names so the Angular form can put
    /// each value back where the requester saw it.
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return await base.BuildDetailAsync(context);
        }

        var detail = new WorkflowRequestDetail
        {
            SubmissionComment = plan.Comments
        };

        return detail
            .Add(WorkflowDetailField.Text(WorkflowDetailFieldNames.RequestNumber, L("RequestNumber"), context.Request?.RequestNumber))
            .Add(WorkflowDetailField.Date(nameof(LeavePlan.StartDate), L("StartDate"), plan.StartDate))
            .Add(WorkflowDetailField.Date(nameof(LeavePlan.EndDate), L("EndDate"), plan.EndDate))
            .Add(WorkflowDetailField.Number(nameof(LeavePlan.PlannedDays), L("PlannedDays"), (decimal)plan.PlannedDays))
            .Add(WorkflowDetailField.Number(
                nameof(LeavePlan.ProjectedLeaveBalance),
                L("ProjectedLeaveBalance"),
                plan.ProjectedLeaveBalance.HasValue ? (decimal?)Math.Round((decimal)plan.ProjectedLeaveBalance.Value, 3) : null))
            .Add(WorkflowDetailField.LongText(nameof(LeavePlan.Comments), L("Comments"), plan.Comments));
    }
}
