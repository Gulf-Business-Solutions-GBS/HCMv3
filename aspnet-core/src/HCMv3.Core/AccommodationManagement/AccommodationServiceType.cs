using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// AccommodationServiceType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.AccommodationServiceType</c>). Code/NameEn/NameAr
/// lookup plus a <see cref="Comments"/> field, so it cannot derive from the pure <c>SimpleLookupEntity</c>
/// shape -- it gets its own bespoke DTOs/app-service (unlike <c>AccommodationType</c>, the legacy source
/// does not implement <c>IExcelImport</c> on this entity).
/// </summary>
public class AccommodationServiceType : FullAuditedEntity, IMustHaveTenant
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
