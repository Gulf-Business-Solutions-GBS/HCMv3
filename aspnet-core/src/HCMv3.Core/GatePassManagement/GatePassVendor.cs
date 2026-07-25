using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Gate pass vendor master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GatePassManagement.GatePassVendor</c>). Extends the Code/NameEn/NameAr
/// shape with a <see cref="VendorAddress"/> field, so (like <c>AccommodationType</c> in the
/// AccommodationManagement module) it cannot derive from the pure <c>SimpleLookupEntity</c> shape -- it gets
/// its own bespoke DTOs/app-service. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not
/// ported.
/// </summary>
public class GatePassVendor : FullAuditedEntity, IMustHaveTenant
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

    [StringLength(1000)]
    public virtual string VendorAddress { get; set; }
}
