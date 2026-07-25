using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Aggregated child of <see cref="GatePass"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassVehicle</c>), linked back via
/// <see cref="GatePassId"/>. Following the established Travel/TravelDetail precedent in this codebase, this
/// child is managed by its own app service rather than nested inside the <c>GatePass</c> create/edit DTO.
/// Legacy <c>PerformValidation</c> parity (no duplicate VehiclePlateNumber per gate pass) is ported in
/// <c>GatePassVehiclesAppService</c>.
/// </summary>
public class GatePassVehicle : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int GatePassId { get; set; }

    public virtual GatePass GatePass { get; set; }

    public virtual int VehicleTypeId { get; set; }

    public virtual VehicleType VehicleType { get; set; }

    [Required]
    [StringLength(64)]
    public virtual string VehiclePlateNumber { get; set; }

    public virtual DateTime VehicleDocExpiryDate { get; set; }

    public virtual int VehicleBrandId { get; set; }

    public virtual VehicleBrand VehicleBrand { get; set; }

    public virtual int VehicleModelId { get; set; }

    public virtual VehicleModel VehicleModel { get; set; }
}
