using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// City master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.City</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="State"/> FK, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class City : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? StateId { get; set; }

    public virtual State State { get; set; }
}
