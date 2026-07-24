using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Bed master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.AccommodationManagement.Bed</c>). Code/NameEn/NameAr/Comments lookup
/// with a <see cref="RoomId"/> FK. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not
/// ported.
/// </summary>
public class Bed : FullAuditedEntity, IMustHaveTenant
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

    public virtual int RoomId { get; set; }

    public virtual Room Room { get; set; }

    public virtual string Comments { get; set; }
}
