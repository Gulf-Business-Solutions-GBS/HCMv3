using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Nationality master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Nationality</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="NationalityGroup"/> FK, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class Nationality : FullAuditedEntity, IMustHaveTenant, IBilingualLookup
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

    public virtual int? NationalityGroupId { get; set; }

    public virtual NationalityGroup NationalityGroup { get; set; }
}
