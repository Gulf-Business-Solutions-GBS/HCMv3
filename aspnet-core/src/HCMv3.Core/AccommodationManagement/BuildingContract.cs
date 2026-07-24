using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Contract period of a <see cref="Building"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.BuildingContract</c>), listed separately and
/// linked back via <see cref="BuildingId"/>.
/// </summary>
public class BuildingContract : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int BuildingId { get; set; }

    public virtual Building Building { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual string Comments { get; set; }
}
