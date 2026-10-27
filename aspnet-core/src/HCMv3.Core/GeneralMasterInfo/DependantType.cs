using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DependantType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DependantType</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="Gender"/> enum and an <c>IsEmployee</c> flag,
/// so it can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class DependantType : FullAuditedEntity, IMustHaveTenant
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

    public virtual Gender Gender { get; set; }

    /// <summary>
    /// Legacy parity: only one DependantType record can have this flag set to <c>true</c>.
    /// </summary>
    public virtual bool IsEmployee { get; set; }
}
