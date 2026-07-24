using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI weighting line for a <see cref="KpiByJobTitle"/> header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiByJobTitleDetail</c>).
/// </summary>
public class KpiByJobTitleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int KpiByJobTitleId { get; set; }

    public virtual KpiByJobTitle KpiByJobTitle { get; set; }

    public virtual int KpiInfoId { get; set; }

    public virtual KpiInfo KpiInfo { get; set; }

    public virtual double Weight { get; set; }

    public virtual string Target { get; set; }
}
