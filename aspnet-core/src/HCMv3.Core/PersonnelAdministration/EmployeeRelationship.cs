using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound relationship between two employees (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeRelationship</c>).
/// </summary>
public class EmployeeRelationship : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int RelationshipTypeId { get; set; }

    public virtual RelationshipType RelationshipType { get; set; }

    public virtual int RelatedEmployeeId { get; set; }

    public virtual Employee RelatedEmployee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
