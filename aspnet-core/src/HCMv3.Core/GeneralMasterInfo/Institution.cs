using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Institution master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Institution</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="University"/> and <see cref="Country"/> FKs, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class Institution : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? UniversityId { get; set; }

    public virtual University University { get; set; }

    /// <summary>Legacy <c>Institution.Country</c>. Optional.</summary>
    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }
}
