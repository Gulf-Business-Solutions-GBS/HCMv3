using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI weighting line for a <see cref="KpiByOrgUnit"/> header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiByOrgUnitDetail</c>).
/// </summary>
public class KpiByOrgUnitDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int KpiByOrgUnitId { get; set; }

    public virtual KpiByOrgUnit KpiByOrgUnit { get; set; }

    public virtual int KpiInfoId { get; set; }

    public virtual KpiInfo KpiInfo { get; set; }

    public virtual double Weight { get; set; }

    public virtual string Target { get; set; }
}
