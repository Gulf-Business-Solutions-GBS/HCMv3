using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.CompetencyDevelopment;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiInfo</c>).
/// Legacy <c>Competency</c> FK is <see cref="CompetencyId"/> (CompetencyDevelopment
/// <see cref="CompetencyDevelopment.Competency"/>), optional as in legacy.
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

    /// <summary>Legacy <c>KpiInfo.Competency</c>. Optional; legacy validation never required it.</summary>
    public virtual int? CompetencyId { get; set; }

    public virtual Competency Competency { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual bool Active { get; set; }
}
