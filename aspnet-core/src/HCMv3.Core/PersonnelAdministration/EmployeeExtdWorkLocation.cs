using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee extended work-location record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeExtdWorkLocation</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="WorkLocationId"/> FKs <c>HCMv3.OrganizationManagement.WorkLocation</c> (already ported).
///
/// DELIBERATE SIMPLIFICATION: the legacy overlap-with-<c>EmployeeOrgAssignment</c>'s-default-WorkLocation
/// cross-entity validation is application-layer concern, enforced in
/// <c>EmployeeExtdWorkLocationsAppService</c>, not on the entity itself.
/// </summary>
public class EmployeeExtdWorkLocation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
