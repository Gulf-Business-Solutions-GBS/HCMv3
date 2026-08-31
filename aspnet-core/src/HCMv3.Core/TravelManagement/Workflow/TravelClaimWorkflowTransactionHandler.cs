using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TravelManagement.Workflow;

/// <summary>
/// Workflow handler for travel expense claims (<see cref="TravelFeedback"/>).
///
/// <para>
/// Named for what it is to the employee rather than what the table is called. <c>TravelFeedback</c> is the
/// legacy name and carries the claim amount, so the entity keeps it; the request type an approver sees says
/// "Travel Expense Claim", because that is the decision being asked for.
/// </para>
/// <para>
/// Lives in TravelManagement, not Workflow -- the same rule the other eight follow. Travel rules belong to
/// the module that owns travel, and the engine stays type-agnostic.
/// </para>
/// </summary>
public class TravelClaimWorkflowTransactionHandler : WorkflowTransactionHandlerBase<TravelFeedback>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for travel claims.</summary>
    public const string TravelClaimRequestType = "TravelFeedback";

    private readonly IRepository<Travel> _travelRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TravelClaimWorkflowTransactionHandler(
        IRepository<TravelFeedback> repository,
        IRepository<Travel> travelRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _travelRepository = travelRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => TravelClaimRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var claim = await GetTargetAsync(context);
        if (claim == null)
        {
            return;
        }

        if (claim.ClaimAmount <= 0)
        {
            throw new UserFriendlyException(L("ClaimAmountMustBeGreaterThanZero"));
        }

        // A second claim against the same trip is almost always a resubmission the employee thought had
        // failed. Pending ones count, or the duplicate would be invisible to this check.
        if (claim.TravelId.HasValue)
        {
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                var alreadyClaimed = Repository.GetAll()
                    .Any(c => c.Id != claim.Id
                              && c.EmployeeId == claim.EmployeeId
                              && c.TravelId == claim.TravelId
                              && (c.WorkflowStatus == null
                                  || c.WorkflowStatus == RequestStatus.Approved
                                  || c.WorkflowStatus == RequestStatus.Closed
                                  || c.WorkflowStatus == RequestStatus.Open));

                if (alreadyClaimed)
                {
                    throw new UserFriendlyException(L("ATravelClaimAlreadyExistsForThisTrip"));
                }
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var claim = await GetTargetAsync(context);
        if (claim == null)
        {
            return null;
        }

        var date = claim.ClaimDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"Travel claim {claim.ClaimAmount:0.##}, {date}",
            $"مطالبة سفر {claim.ClaimAmount:0.##}، {date}");
    }
}
