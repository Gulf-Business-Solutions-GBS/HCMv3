using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Access Unit master data (ported from the legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.AccessUnit</c>).
///
/// Legacy XAF/XPO mapping notes:
///  - <c>SysCreator/SysCreatedOn/SysModifier/SysModifiedOn</c>  -> handled by <see cref="FullAuditedEntity"/>.
///  - <c>SysTenant</c>                                          -> handled by <see cref="IMustHaveTenant.TenantId"/>.
///  - <c>Code</c> auto-numbering + duplicate Code/Name checks   -> moved into the application service (out of the persistence lifecycle).
/// </summary>
public class AccessUnit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Business code. Auto-generated (max existing numeric code + 1) when left blank, mirroring the legacy <c>GetMaxCode</c> behavior.
    /// </summary>
    [StringLength(AccessUnitConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(AccessUnitConsts.MaxNameEnLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(AccessUnitConsts.MaxNameArLength)]
    public virtual string NameAr { get; set; }

    [StringLength(AccessUnitConsts.MaxIPAddressLength)]
    public virtual string IPAddress { get; set; }

    /// <summary>
    /// Legacy record-lock flag (0 = unlocked). Preserved as-is for parity with the legacy data model.
    /// </summary>
    public virtual int Locked { get; set; }
}
