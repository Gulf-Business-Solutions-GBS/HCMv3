using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TravelManagement.Workflow;

/// <summary>
/// Workflow handler for travel requests (<see cref="Travel"/>) -- the front half of travel, which until now
/// did not exist.
///
/// <para>
/// <b>Why this closes an orphan.</b> <see cref="TravelFeedback"/>, the travel EXPENSE CLAIM, has been
/// routable since the workflow engine shipped, and it points at a <see cref="Travel"/> row. But the only way
/// a Travel row could come into existence was for an administrator to key the trip in by hand, so an
/// employee could claim expenses for a trip that nobody in the system was ever able to request. This handler
/// is the missing half: the employee asks for the trip, it is approved, and the claim is then raised against
/// it.
/// </para>
///
/// <para>
/// <b>There is no TravelDeskRequest here, deliberately.</b> Legacy carried a whole parallel triple --
/// <c>TravelDeskRequest</c> / <c>TravelDeskDetailRequest</c> / <c>TravelDeskDetailCostRequest</c> -- and
/// copied it field by field onto Travel/TravelDetail/TravelDetailCost when the request was approved. The
/// trip is created up front instead, legs and costs and all, and hidden by the <c>ApprovedOnly</c> filter
/// until the chain completes. That is why <c>OnApprovedAsync</c> is not overridden: the base's status stamp
/// is what makes the row visible, and the row becoming visible IS the trip being approved.
/// </para>
///
/// <para>
/// <b>The legacy defect that is not reproduced.</b> <c>TravelDeskRequest.OnSaving</c> wrapped the entire
/// approval side effect in <c>catch (Exception ex) { ex.Message.ToString(); }</c>. If creating the Travel or
/// any of its children failed, the request was still marked approved, nothing was written, and nothing was
/// reported -- an employee holding an approved travel request for a trip that does not exist. There is no
/// copy step here to fail; and where this handler does throw, the approval rolls back with it, which is the
/// opposite behaviour and the correct one.
/// </para>
///
/// <para>
/// Lives in TravelManagement rather than Workflow, the rule every other handler follows: travel rules belong
/// to the module that owns travel, and the engine stays type-agnostic.
/// </para>
/// </summary>
public class TravelWorkflowTransactionHandler : WorkflowTransactionHandlerBase<Travel>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for travel requests.</summary>
    public const string TravelRequestType = "Travel";

    private readonly IRepository<TravelDetail> _travelDetailRepository;
    private readonly IRepository<TravelDestination> _destinationRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TravelWorkflowTransactionHandler(
        IRepository<Travel> repository,
        IRepository<TravelDetail> travelDetailRepository,
        IRepository<TravelDestination> destinationRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _travelDetailRepository = travelDetailRepository;
        _destinationRepository = destinationRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => TravelRequestType;

    /// <summary>
    /// The legacy rule set, plus the one rule legacy applied only at the copy step.
    ///
    /// <para>
    /// The overlap check is the interesting one. Legacy performed it inside <c>OnSaving</c> at the moment of
    /// approval, and used it merely to SKIP creating a duplicate Travel -- silently, because of the
    /// swallowing catch, so the employee got an approved request and no trip. Here it is a refusal at
    /// submit, before anything is created, which is where a "you are already travelling then" answer belongs.
    /// </para>
    /// <para>
    /// It runs with <c>ApprovedOnly</c> disabled on purpose: a trip the employee has already requested and
    /// which is still sitting in an approver's inbox must block a second one. With the filter on, the
    /// pending trip is invisible to this check and the same fortnight could be requested twice over --
    /// which, since both would then be approved, is exactly the state the check exists to prevent.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var travel = await GetTargetAsync(context);
        if (travel == null)
        {
            return;
        }

        if (travel.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (travel.EndDate.Date < travel.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeBeforeStartDate"));
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // A trip with no legs is not a request anybody can decide on -- there is nowhere to go, nothing
            // to book and no cost. Checked on the row rather than on the input so it holds whichever door
            // the request came in through, self service or the travel desk.
            var hasLegs = _travelDetailRepository.GetAll().Any(d => d.TravelId == travel.Id);
            if (!hasLegs)
            {
                throw new UserFriendlyException(L("ATravelRequestMustHaveAtLeastOneLeg"));
            }

            var start = travel.StartDate.Date;
            var end = travel.EndDate.Date;

            // Legacy's own overlap predicate, kept in its legacy shape:
            //   (other.Start <= mine.Start && other.End >= mine.Start) || (mine.Start <= other.Start && mine.End >= other.Start)
            // Rejected trips are excluded -- a refused request is not a trip, and leaving it in would lock
            // the employee out of ever re-requesting the dates they were turned down for.
            var overlapping = Repository.GetAll().Any(t =>
                t.Id != travel.Id
                && t.EmployeeId == travel.EmployeeId
                && t.WorkflowStatus != RequestStatus.Rejected
                && ((t.StartDate.Date <= start && t.EndDate.Date >= start)
                    || (start <= t.StartDate.Date && end >= t.StartDate.Date)));

            if (overlapping)
            {
                throw new UserFriendlyException(L("YouAlreadyHaveATripThatOverlapsTheseDates"));
            }
        }
    }

    /// <summary>
    /// Where they are going, when, and what it costs. An approver deciding a travel request is deciding
    /// those three things, so all three are in the one line they see in their inbox; a summary that said
    /// only "Travel request" would force them to open every one.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var travel = await GetTargetAsync(context);
        if (travel == null)
        {
            return null;
        }

        string destinationEn = null;
        string destinationAr = null;
        double totalCost;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var legs = _travelDetailRepository.GetAll()
                .Where(d => d.TravelId == travel.Id)
                .OrderBy(d => d.DepartureDate)
                .ToList();

            // The cost is summed from the legs rather than read off Travel.TotalTravelCost, because that
            // column is a stored rollup nothing recomputes (see the Travel entity remarks) and an approver
            // must not be shown a stale figure for the thing they are approving.
            totalCost = legs.Sum(l => l.TotalAmount);

            var arrivalId = legs.Select(l => (int?)l.ArrivalId).FirstOrDefault();
            if (arrivalId.HasValue)
            {
                var destination = _destinationRepository.GetAll().FirstOrDefault(d => d.Id == arrivalId.Value);
                destinationEn = destination?.NameEn;
                destinationAr = destination?.NameAr;
            }
        }

        var period = $"{travel.StartDate:dd MMM yyyy} - {travel.EndDate:dd MMM yyyy}";

        return new WorkflowSummary(
            $"Travel to {destinationEn ?? "-"}, {period}, {totalCost:0.##}",
            $"سفر إلى {destinationAr ?? "-"}، {period}، {totalCost:0.##}");
    }
}
