using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Free-text goal entry for a <see cref="PAForm"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAGoal</c>).
/// </summary>
public class PAGoal : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAFormId { get; set; }

    public virtual PAForm PAForm { get; set; }

    [StringLength(2000)]
    public virtual string Goal { get; set; }
}
