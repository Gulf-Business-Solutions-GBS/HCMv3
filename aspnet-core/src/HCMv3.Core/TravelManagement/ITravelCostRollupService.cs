using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TravelManagement;

/// <summary>
/// Keeps <see cref="TravelDetail.TotalAmount"/> and <see cref="Travel.TotalTravelCost"/> in line with the
/// cost lines below them, the way legacy's three <c>UpdateTotalAmount</c> methods did
/// (<c>HCM10.Module/BusinessObjects/TravelManagement/Travel.cs</c> lines 194-211,
/// <c>TravelDetail.cs</c> lines 262-279, <c>TravelDetailCost.cs</c> lines 111-133).
///
/// <para>The recalculate methods read from the database, so the caller must save its own insert, update or
/// delete first (the app services call <c>CurrentUnitOfWork.SaveChangesAsync()</c> before calling).</para>
/// </summary>
public interface ITravelCostRollupService : IDomainService
{
    /// <summary>
    /// Legacy <c>Travel.UpdateTotalAmount</c> / <c>TravelDetail.UpdateTotalAmount</c>:
    /// <c>TotalTravelCost = Rounding(employee, sum of the trip's legs' TotalAmount)</c>.
    /// Does nothing when the trip does not exist.
    /// </summary>
    Task RecalculateTravelTotalAsync(int travelId);

    /// <summary>
    /// Legacy <c>TravelDetailCost.UpdateTotalAmount</c>: the leg's
    /// <c>TotalAmount = Rounding(employee, sum of its cost lines' Amount)</c>, then the trip total as in
    /// <see cref="RecalculateTravelTotalAsync"/>. Does nothing when the leg does not exist.
    /// </summary>
    Task RecalculateTravelDetailTotalAsync(int travelDetailId);

    /// <summary>
    /// The same two totals for a trip being created as one graph (legs and cost lines in memory, nothing
    /// saved yet): each leg from its cost lines, then the trip from its legs.
    /// </summary>
    Task ApplyToNewTravelAsync(Travel travel);
}
