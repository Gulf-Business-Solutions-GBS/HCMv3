using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Location master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.Location</c>). Code/NameEn/NameAr lookup plus a
/// <see cref="Comments"/> field, so unlike the pure GeneralMasterInfo lookups it cannot derive from
/// <see cref="SimpleLookupEntity"/> (same reasoning as <c>PayrollManagement.AccountInfo</c>) -- it gets its
/// own bespoke DTOs/app-service. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not
/// ported.
/// </summary>
public class Location : FullAuditedEntity, IMustHaveTenant
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
