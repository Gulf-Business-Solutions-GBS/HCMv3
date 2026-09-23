using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.GovtRelationsManagement.Workflow;

/// <summary>
/// Workflow handler for exit permits (<see cref="EmployeeExitPermit"/>).
///
/// <para>
/// <b>No <c>OnApprovedAsync</c> override, on purpose.</b> Approving an exit permit changes nothing beyond the
/// row's own status: the permit was granted by the government before this record existed, so there is no
/// quota to deduct, no payment to raise and no other table to touch. The legacy object appeared to have an
/// approval side effect, but the block was commented out and had been copy-pasted from a different
/// transaction type; implementing it here would invent behaviour the business never had. The base's status
/// stamp is the whole of what approval means for this type.
/// </para>
///
/// <para>
/// Lives in GovtRelationsManagement rather than Workflow, the same rule the other handlers follow: permit
/// rules belong to the module that owns government relations, and the engine stays type-agnostic.
/// </para>
/// </summary>
public class ExitPermitWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeExitPermit>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for exit permits.</summary>
    public const string ExitPermitRequestType = "EmployeeExitPermit";

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ExitPermitWorkflowTransactionHandler(
        IRepository<EmployeeExitPermit> repository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => ExitPermitRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var permit = await GetTargetAsync(context);
        if (permit == null)
        {
            return;
        }

        // The permit number is the only thing that ties this record to the document the government actually
        // issued. Without it an approver is being asked to sign off something unverifiable.
        if (permit.ExternalRequestNumber.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("ExitPermitNumberIsRequired"));
        }

        if (permit.ExitDate == default)
        {
            throw new UserFriendlyException(L("ExitDateIsRequired"));
        }

        if (permit.StartDate == default)
        {
            throw new UserFriendlyException(L("StartDateIsRequired"));
        }

        // Nobody leaves the country twice on the same day. A second record for that date is a resubmission
        // the employee thought had failed -- and pending ones have to count, or the duplicate is invisible
        // to this check precisely while it is most likely to be filed.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var exitDate = permit.ExitDate.Date;

            var alreadyRecorded = Repository.GetAll()
                .Any(p => p.Id != permit.Id
                          && p.EmployeeId == permit.EmployeeId
                          && p.ExitDate == exitDate
                          && (p.WorkflowStatus == null
                              || p.WorkflowStatus == RequestStatus.Approved
                              || p.WorkflowStatus == RequestStatus.Closed
                              || p.WorkflowStatus == RequestStatus.Open));

            if (alreadyRecorded)
            {
                throw new UserFriendlyException(L("AnExitPermitAlreadyExistsForThisExitDate"));
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var permit = await GetTargetAsync(context);
        if (permit == null)
        {
            return null;
        }

        var date = permit.ExitDate.ToString("dd MMM yyyy");
        var number = permit.ExternalRequestNumber ?? string.Empty;

        return new WorkflowSummary(
            $"{ReasonEn(permit.TravelReason)} exit permit {number}, {date}",
            $"إذن خروج {ReasonAr(permit.TravelReason)} {number}، {date}");
    }

    /// <summary>
    /// Every field the exit permit form captures, in the order it captures them.
    ///
    /// <para>
    /// This is the type that exposed the gap. The summary line happens to carry the permit number and the
    /// exit date, so an approver could half-decide from it -- but <see cref="EmployeeExitPermit.StartDate"/>
    /// appears nowhere in that sentence and was simply unreachable, and the reason and the requester's own
    /// note were only there by accident of wording. All six are here, which is the whole point: the approver
    /// reads what the requester filled in, not a paraphrase of it.
    /// </para>
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var permit = await GetTargetAsync(context);
        if (permit == null)
        {
            // The row is gone or not written yet. The engine's own facts still describe the request, and a
            // card showing those beats a card showing nothing.
            return await base.BuildDetailAsync(context);
        }

        var detail = new WorkflowRequestDetail
        {
            SubmissionComment = permit.Comments
        };

        return detail
            .Add(WorkflowDetailField.Text(L("RequestNumber"), context.Request?.RequestNumber))
            .Add(WorkflowDetailField.Text(L("ExitPermitNumber"), permit.ExternalRequestNumber))
            .Add(WorkflowDetailField.Text(L("TravelReason"), L("TravelReason_" + permit.TravelReason)))
            .Add(WorkflowDetailField.Date(L("StartDate"), permit.StartDate))
            .Add(WorkflowDetailField.Date(L("ExitDate"), permit.ExitDate))
            .Add(WorkflowDetailField.LongText(L("Comments"), permit.Comments));
    }

    private static string ReasonEn(TravelReason reason)
    {
        switch (reason)
        {
            case TravelReason.PlannedLeave: return "Planned leave";
            case TravelReason.EmergencyLeave: return "Emergency leave";
            case TravelReason.PersonalTravel: return "Personal travel";
            default: return reason.ToString();
        }
    }

    private static string ReasonAr(TravelReason reason)
    {
        switch (reason)
        {
            case TravelReason.PlannedLeave: return "إجازة مخططة";
            case TravelReason.EmergencyLeave: return "إجازة طارئة";
            case TravelReason.PersonalTravel: return "سفر شخصي";
            default: return reason.ToString();
        }
    }
}
