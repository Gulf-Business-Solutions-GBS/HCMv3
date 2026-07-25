using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Company pool-car master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.PoolCar</c>). Standalone vehicle record (as opposed to
/// <see cref="GatePassVehicle"/>, which is scoped to a single gate pass) that <see cref="EmployeePoolCar"/>
/// assignments reference.
/// </summary>
public class PoolCar : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

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
