using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// A competency-development review period (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CDPeriod</c>).
/// </summary>
public class CDPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    public virtual bool Closed { get; set; }
}
