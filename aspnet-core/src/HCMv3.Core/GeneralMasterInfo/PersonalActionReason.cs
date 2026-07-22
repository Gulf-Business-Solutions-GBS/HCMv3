using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// PersonalActionReason master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.PersonalActionReason</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="PersonalAction"/> FK, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// The legacy <c>NotificationConfig</c> FK is intentionally omitted: NotificationConfig isn't ported yet.
/// </summary>
public class PersonalActionReason : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? PersonalActionId { get; set; }

    public virtual PersonalAction PersonalAction { get; set; }
}
