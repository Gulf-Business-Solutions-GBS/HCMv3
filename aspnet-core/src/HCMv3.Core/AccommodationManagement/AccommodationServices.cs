using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Date-range service step of an <see cref="EmployeeAccommodation"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.AccommodationServices</c>), listed separately and
/// linked back via <see cref="EmployeeAccommodationId"/>.
/// </summary>
public class AccommodationServices : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeAccommodationId { get; set; }

    public virtual EmployeeAccommodation EmployeeAccommodation { get; set; }

    public virtual int AccommodationServiceTypeId { get; set; }

    public virtual AccommodationServiceType AccommodationServiceType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual string Comments { get; set; }
}
