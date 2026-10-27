using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// AccommodationType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.AccommodationType</c>). Code/NameEn/NameAr lookup
/// plus a <see cref="Comments"/> field, so (like <c>Location</c> in this module) it cannot derive from the
/// pure <c>SimpleLookupEntity</c> shape -- it gets its own bespoke DTOs/app-service. The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class AccommodationType : FullAuditedEntity, IMustHaveTenant
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

    public virtual string Comments { get; set; }
}
