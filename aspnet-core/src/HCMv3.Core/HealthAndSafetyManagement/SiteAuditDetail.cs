using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Aggregated child of <see cref="SiteAudit"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.SiteAuditDetail</c>), linked back via
/// <see cref="SiteAuditId"/>. Legacy <c>PerformValidation</c> required SiteAudit and a non-empty
/// Observation -- ported as-is in <c>SiteAuditDetailsAppService</c>.
/// </summary>
public class SiteAuditDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SiteAuditId { get; set; }

    public virtual SiteAudit SiteAudit { get; set; }

    public virtual string Observation { get; set; }

    public virtual string CorrectionAction { get; set; }
}
