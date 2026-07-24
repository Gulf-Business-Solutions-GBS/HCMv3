using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiInfo</c>).
/// The legacy <c>Competency</c> FK (to the never-ported CompetencyDevelopment module) is DROPPED --
/// deliberate simplification, consistent with other cross-module FKs dropped elsewhere in this port.
/// <see cref="NameEn"/>/<see cref="NameAr"/> are unlimited length per legacy <c>[Size(SizeAttribute.Unlimited)]</c>.
/// </summary>
public class KpiInfo : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(64)]
    public virtual string Code { get; set; }

    [Required]
    public virtual string NameEn { get; set; }

    [Required]
    public virtual string NameAr { get; set; }

    public virtual int KpiCategoryId { get; set; }

    public virtual KpiCategory KpiCategory { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual bool Active { get; set; }
}
