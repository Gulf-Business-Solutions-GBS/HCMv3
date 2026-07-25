using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Annual-year-review rating lookup (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CDRating</c>). A pure bilingual Code/NameEn/NameAr
/// lookup, but kept as a standalone entity (not <see cref="SimpleLookupEntity"/>) for consistency with the
/// rest of this standalone module.
/// </summary>
public class CDRating : FullAuditedEntity, IMustHaveTenant
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
