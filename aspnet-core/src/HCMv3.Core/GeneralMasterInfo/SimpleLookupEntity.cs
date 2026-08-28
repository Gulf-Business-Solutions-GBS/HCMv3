using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Base class for the pure bilingual lookup entities ported from the legacy XAF GeneralMasterInfo
/// module (Code/NameEn/NameAr only, e.g. Country, Title, Religion). Concrete entities are empty
/// subclasses; the shared code/name shape and validation lives here and in
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/>.
/// </summary>
public abstract class SimpleLookupEntity : FullAuditedEntity, IMustHaveTenant, IBilingualLookup
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
}
