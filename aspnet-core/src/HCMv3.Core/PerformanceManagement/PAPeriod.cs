using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal period master for the older <see cref="PAForm"/> model (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAPeriod</c>).
/// </summary>
public class PAPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    public virtual bool Closed { get; set; }
}
