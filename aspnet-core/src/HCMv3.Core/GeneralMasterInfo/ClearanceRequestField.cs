using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// ClearanceRequestField master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.ClearanceRequestField</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="ClearanceRequestType"/> FK,
/// so it can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>IsActive</c> flag is preserved as-is.
/// </summary>
public class ClearanceRequestField : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? ClearanceRequestTypeId { get; set; }

    public virtual ClearanceRequestType ClearanceRequestType { get; set; }

    public virtual bool IsActive { get; set; }
}
