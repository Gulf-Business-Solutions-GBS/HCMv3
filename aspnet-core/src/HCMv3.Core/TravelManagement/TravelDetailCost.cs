using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TravelManagement;

/// <summary>
/// Aggregated child of <see cref="TravelDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetailCost</c>), linked back via
/// <see cref="TravelDetailId"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy cascade that recomputed <c>TravelDetail.TotalAmount</c> and
/// <c>Travel.TotalTravelCost</c> on every save/delete of a cost row is NOT ported -- see
/// <see cref="TravelDetail"/> remarks. <c>TravelDetailCostsAppService</c> performs straightforward CRUD on
/// this row only.
/// </summary>
public class TravelDetailCost : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TravelDetailId { get; set; }

    public virtual TravelDetail TravelDetail { get; set; }

    public virtual int ExpenseTypeId { get; set; }

    public virtual ExpenseType ExpenseType { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
