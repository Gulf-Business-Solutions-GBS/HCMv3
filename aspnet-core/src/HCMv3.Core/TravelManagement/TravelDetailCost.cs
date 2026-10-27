using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

/// <summary>
/// Aggregated child of <see cref="TravelDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetailCost</c>), linked back via
/// <see cref="TravelDetailId"/>.
///
/// Saving or deleting a cost line recalculates <c>TravelDetail.TotalAmount</c> and
/// <c>Travel.TotalTravelCost</c> through <see cref="ITravelCostRollupService"/>, as legacy did.
/// </summary>
public class TravelDetailCost : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TravelDetailId { get; set; }

    public virtual TravelDetail TravelDetail { get; set; }

    public virtual int ExpenseTypeId { get; set; }

    public virtual ExpenseType ExpenseType { get; set; }

    /// <summary>
    /// What currency <see cref="Amount"/> is in. Present on the legacy <c>TravelDeskDetailCostRequest</c>
    /// and simply never copied across when the request was turned into a <c>TravelDetailCost</c> -- a plain
    /// bug rather than a design decision, and one that silently reinterpreted every foreign-currency line as
    /// company currency. Nullable, because every row that exists today predates the column and there is no
    /// honest value to backfill.
    /// </summary>
    public virtual int? CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
