using System;
using System.Collections.Generic;
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
/// <item><description><see cref="TotalAmount"/> (legacy rollup of child <see cref="TravelDetailCost.Amount"/>
/// rows) is stored, NOT client-editable, and recalculated by <see cref="ITravelCostRollupService"/> when a
/// cost line is saved or deleted.</description></item>
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

    /// <summary>
    /// Legacy "Per Diem Pay (%)": the share of this leg's per-diem amount that is paid. 0-100, default 100
    /// (legacy <c>AfterConstruction</c>). Legacy applied it only where the per diem is PAID -- the payroll
    /// engine's <c>GetTravelPerDiemPay</c> summed <c>PerDiemAmount * PerDiemPayPercentage / 100</c> per leg;
    /// the per-diem amount itself (nights x rate) ignores it. <c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c>
    /// returns both figures.
    /// </summary>
    public virtual double PerDiemPayPercentage { get; set; } = 100;

    /// <summary>
    /// Does this leg need a visa arranged? Carried on the legacy <c>TravelDeskDetailRequest</c> and never
    /// copied onto <c>TravelDetail</c>, so the one instruction the travel desk actually had to act on
    /// disappeared the moment the request was approved. Now that the leg IS the request, it lives here.
    /// </summary>
    public virtual bool IsVisaRequired { get; set; }

    [StringLength(500)]
    public virtual string VisaComments { get; set; }

    /// <summary>Does this leg need a hotel booked? Same provenance as <see cref="IsVisaRequired"/>.</summary>
    public virtual bool IsHotelRequired { get; set; }

    [StringLength(500)]
    public virtual string HotelComments { get; set; }

    /// <summary>Anything else to be arranged or reimbursed. Same provenance as <see cref="IsVisaRequired"/>.</summary>
    public virtual bool AnyOtherExpense { get; set; }

    [StringLength(500)]
    public virtual string OtherComments { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>The cost lines for this leg, so a whole trip inserts as one graph.</summary>
    public virtual ICollection<TravelDetailCost> TravelDetailCosts { get; set; }
}
