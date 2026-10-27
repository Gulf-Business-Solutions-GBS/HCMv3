using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Effective-dated KPI weighting header for an <c>Abp.Organizations.OrganizationUnit</c> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiByOrgUnit</c>).
/// <see cref="OrganizationUnitId"/> FKs Zero's own built-in organization unit tree (a <c>long</c>), same
/// pattern as <c>GovtRelationsManagement.Sponsor.OrganizationUnitId</c>/<c>OrganizationDocument.OrganizationUnitId</c>.
/// Aggregated child is <see cref="KpiByOrgUnitDetail"/>. EndDate auto-chaining is ported in
/// <c>KpiByOrgUnitsAppService</c>, same pattern as <see cref="KpiByJobTitle"/>.
/// </summary>
public class KpiByOrgUnit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
