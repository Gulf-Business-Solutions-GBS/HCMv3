using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Date-bound assignment of an <see cref="Employee"/> to a <see cref="Position"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.PositionRelationship</c>). FTE-limit overlap
/// validation and the <c>Position.Vacant</c> flag recompute are ported into
/// <c>PositionRelationshipsAppService</c>.
/// </summary>
public class PositionRelationship : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PositionId { get; set; }

    public virtual Position Position { get; set; }

    public virtual PositionRelationType RelationType { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
