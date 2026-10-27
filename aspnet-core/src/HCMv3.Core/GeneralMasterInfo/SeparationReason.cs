using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// SeparationReason master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.SeparationReason</c>).
/// Extends the Code/NameEn/NameAr shape with <c>IsSystemRecord</c>/<c>AllowAdvanceEoS</c> flags, so it
/// can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class SeparationReason : FullAuditedEntity, IMustHaveTenant
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

    /// <summary>
    /// Legacy parity: system-seeded records are locked from editing/deletion outside the seed process.
    /// </summary>
    public virtual bool IsSystemRecord { get; set; }

    /// <summary>
    /// Legacy "Allow Advance End of Service" flag.
    /// </summary>
    public virtual bool AllowAdvanceEoS { get; set; }
}
