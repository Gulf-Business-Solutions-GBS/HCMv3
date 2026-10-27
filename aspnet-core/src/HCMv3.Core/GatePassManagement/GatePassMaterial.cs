using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Aggregated child of <see cref="GatePass"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassMaterial</c>), linked back via
/// <see cref="GatePassId"/>. Following the established Travel/TravelDetail precedent in this codebase, this
/// child is managed by its own app service rather than nested inside the <c>GatePass</c> create/edit DTO.
/// </summary>
public class GatePassMaterial : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int GatePassId { get; set; }

    public virtual GatePass GatePass { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string ItemCode { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string ItemName { get; set; }

    public virtual int Quantity { get; set; }

    public virtual DateTime ExpectedReturnDate { get; set; }

    public virtual int ReturnedQuantity { get; set; }
}
