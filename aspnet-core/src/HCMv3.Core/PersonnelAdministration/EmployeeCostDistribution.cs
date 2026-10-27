using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee cost distribution record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeCostDistribution</c>, Wave 4 deferred
/// bucket).
///
/// The legacy <c>OrganizationUnit</c> field is mapped to <see cref="OrganizationUnitId"/>, Zero's own
/// built-in <c>long</c> <c>Abp.Organizations.OrganizationUnit</c> id -- same pattern as
/// <see cref="EmployeeDisciplinary"/>'s Location field and <c>GovtRelationsManagement.Sponsor</c>.
/// </summary>
public class EmployeeCostDistribution : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual long? OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int CostCount { get; set; }

    public virtual int UsedDays { get; set; }
}
