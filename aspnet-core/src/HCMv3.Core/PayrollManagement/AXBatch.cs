using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Journal batch header used for AX (accounting export) integration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AXBatch</c>). Trivial entity: no Code/auto-gen, just
/// three required strings.
/// </summary>
public class AXBatch : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(128)]
    public virtual string JournalBatchNumber { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    [Required]
    [StringLength(1024)]
    public virtual string Description { get; set; }
}
