using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Cost element master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.CostElement</c>).
/// Extends the Code/NameEn/NameAr shape with a required <see cref="CostElementType"/> FK, so it can't
/// derive from <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy duplicate-Code check was conditionally scoped per-tenant-setting
/// (<c>AllowDuplicateCodeInCostElement</c>, dependent on the unported SysSettings module) — simplified
/// here to a single global unique-Code check, same as every other entity.
/// </summary>
public class CostElement : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual int? CostElementTypeId { get; set; }

    public virtual CostElementType CostElementType { get; set; }
}
