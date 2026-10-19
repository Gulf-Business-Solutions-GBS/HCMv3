using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo.Services;

namespace HCMv3.TravelManagement;

/// <inheritdoc cref="ITravelCostRollupService"/>
/// <remarks>
/// <para><b>Legacy formulas, kept exactly:</b></para>
/// <list type="bullet">
/// <item><description>Trip: <c>TotalTravelCost = CurrencyService.Rounding(Travel.Employee,
/// TravelDetail.Sum(TotalAmount))</c> -- legacy <c>Travel.cs</c> lines 200-201, <c>TravelDetail.cs</c> lines
/// 268-269, <c>TravelDetailCost.cs</c> lines 122-123. It sums the legs' STORED <c>TotalAmount</c>, it does
/// not go back to the cost lines.</description></item>
/// <item><description>Leg: <c>TotalAmount = CurrencyService.Rounding(Travel.Employee,
/// TravelDetailCost.Sum(Amount))</c> -- legacy <c>TravelDetailCost.cs</c> lines 117-118. Only a cost-line
/// change recalculates a leg; saving the leg itself only re-sums the trip (legacy <c>TravelDetail.cs</c>
/// line 227 calls <c>UpdateTotalAmount</c>, which touches only the trip).</description></item>
/// <item><description>The cost line's own currency is ignored: amounts in any currency are added as they
/// are. Legacy had no currency on the line at all.</description></item>
/// <item><description>Rounding is to the employee's payroll-area currency decimals, unrounded when none is
/// found, and unrounded for an applicant's trip (no employee) -- reused from <see cref="ICurrencyService.RoundAsync"/>, the existing port of legacy
/// <c>CurrencyService.Rounding</c>.</description></item>
/// </list>
///
/// <para>The trip is looked up with the <c>ApprovedOnly</c> filter off, because the travel desk edits legs and
/// costs of trips still waiting for approval, and those must total too.</para>
///
/// <para>Rows are read with <c>ToList()</c> before summing so that a leg whose total was just changed in
/// this unit of work is summed with its new value (EF hands back the tracked instance).</para>
/// </remarks>
public class TravelCostRollupService : HCMv3DomainServiceBase, ITravelCostRollupService
{
    private readonly IRepository<Travel> _travelRepository;
    private readonly IRepository<TravelDetail> _travelDetailRepository;
    private readonly IRepository<TravelDetailCost> _travelDetailCostRepository;
    private readonly ICurrencyService _currencyService;

    public TravelCostRollupService(
        IRepository<Travel> travelRepository,
        IRepository<TravelDetail> travelDetailRepository,
        IRepository<TravelDetailCost> travelDetailCostRepository,
        ICurrencyService currencyService)
    {
        _travelRepository = travelRepository;
        _travelDetailRepository = travelDetailRepository;
        _travelDetailCostRepository = travelDetailCostRepository;
        _currencyService = currencyService;
    }

    public async Task RecalculateTravelTotalAsync(int travelId)
    {
        var travel = FindTravel(travelId);
        if (travel == null)
        {
            // Legacy TravelDetail.cs line 266: `this.Travel != null`.
            return;
        }

        var totalAmount = _travelDetailRepository.GetAll()
            .Where(d => d.TravelId == travelId)
            .ToList()
            .Sum(d => d.TotalAmount);

        travel.TotalTravelCost = await RoundAsync(travel, totalAmount);
        await _travelRepository.UpdateAsync(travel);
    }

    public async Task RecalculateTravelDetailTotalAsync(int travelDetailId)
    {
        var detail = _travelDetailRepository.GetAll().FirstOrDefault(d => d.Id == travelDetailId);
        if (detail == null)
        {
            // Legacy TravelDetailCost.cs line 115: `this.TravelDetail != null`.
            return;
        }

        var travel = FindTravel(detail.TravelId);
        if (travel == null)
        {
            return;
        }

        var totalAmount = _travelDetailCostRepository.GetAll()
            .Where(c => c.TravelDetailId == travelDetailId)
            .ToList()
            .Sum(c => c.Amount);

        detail.TotalAmount = await RoundAsync(travel, totalAmount);
        await _travelDetailRepository.UpdateAsync(detail);

        await RecalculateTravelTotalAsync(travel.Id);
    }

    public async Task ApplyToNewTravelAsync(Travel travel)
    {
        if (travel == null)
        {
            return;
        }

        var legs = travel.TravelDetails?.ToList();
        if (legs == null || legs.Count == 0)
        {
            travel.TotalTravelCost = await RoundAsync(travel, 0);
            return;
        }

        foreach (var leg in legs)
        {
            var costTotal = leg.TravelDetailCosts?.Sum(c => c.Amount) ?? 0;
            leg.TotalAmount = await RoundAsync(travel, costTotal);
        }

        travel.TotalTravelCost = await RoundAsync(travel, legs.Sum(l => l.TotalAmount));
    }

    /// <summary>
    /// Legacy <c>CurrencyService.Rounding(Travel.Employee, value)</c>: <c>emp?.CurrentPayrollArea?...</c>, so an
    /// applicant's trip -- no employee -- is left unrounded rather than refused.
    /// </summary>
    private Task<double> RoundAsync(Travel travel, double value)
    {
        return travel.EmployeeId.HasValue
            ? _currencyService.RoundAsync(travel.EmployeeId.Value, value)
            : Task.FromResult(value);
    }

    private Travel FindTravel(int travelId)
    {
        using (UnitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return _travelRepository.GetAll().FirstOrDefault(t => t.Id == travelId);
        }
    }
}
