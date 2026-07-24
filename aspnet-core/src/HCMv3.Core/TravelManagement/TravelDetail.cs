using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TravelManagement;

/// <summary>
/// Aggregated child of <see cref="Travel"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetail</c>), linked back via <see cref="TravelId"/>.
///
/// DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description>The legacy non-persistent <c>PerDiemRate</c>/<c>PerDiemAmount</c> properties (a live
/// cross-entity lookup: Employee -&gt; OrgAssignment -&gt; EmployeeGroup/SubGroup as of DepartureDate, then a
/// <see cref="TravelPerDiem"/> query) are dropped as stored/computed entity properties. Instead,
/// <c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c> exposes the same lookup as an on-demand
/// app-service method (mirroring <c>ProjectManagement.EmployeeProjectDetailsAppService.CalculateWorkDaysAsync</c>).</description></item>
/// <item><description>The legacy <c>SavePerDiemAmount()</c> side effect (auto-creating a "PerDiem Expense"
/// <see cref="ExpenseType"/> and a child <see cref="TravelDetailCost"/> row on first save) is NOT ported --
/// that implicit side effect is out of scope; costs must be added explicitly via
/// <c>TravelDetailCostsAppService</c>.</description></item>
/// <item><description><see cref="TotalAmount"/> (legacy <c>UpdateTotalAmount()</c> rollup of child
/// <see cref="TravelDetailCost.Amount"/> rows) is kept as a stored, but NOT client-editable, column --
/// same precedent as <see cref="Travel.TotalTravelCost"/>; see that class's remarks.</description></item>
/// </list>
/// </summary>
public class TravelDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TravelId { get; set; }

    public virtual Travel Travel { get; set; }

    public virtual int DepartureId { get; set; }

    public virtual TravelDestination Departure { get; set; }

    public virtual int ArrivalId { get; set; }

    public virtual TravelDestination Arrival { get; set; }

    public virtual DateTime DepartureDate { get; set; }

    public virtual DateTime DepartureTime { get; set; }

    public virtual DateTime ArrivalDate { get; set; }

    public virtual DateTime ArrivalTime { get; set; }

    public virtual int? HotelId { get; set; }

    public virtual Hotel Hotel { get; set; }

    public virtual int? TravelAirlineId { get; set; }

    public virtual TravelAirline TravelAirline { get; set; }

    public virtual bool NightStay { get; set; }

    public virtual int TotalNights { get; set; }

    /// <summary>Legacy display name "Total Amount". See class remarks -- not client-editable.</summary>
    public virtual double TotalAmount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
