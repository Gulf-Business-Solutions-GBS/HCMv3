using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NotificationConfig (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NotificationConfig</c>).
/// No Code/NameEn/NameAr per legacy (just <see cref="Name"/>) plus notification-routing bool flags.
/// The legacy <c>EmailTemplate</c> FK is not ported (module out of scope); the aggregated
/// <c>NotificationGroup</c> collection is ported separately as its own entity (<see cref="NotificationGroup"/>).
/// </summary>
public class NotificationConfig : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    public virtual bool SendNotification { get; set; }

    public virtual bool SendEmail { get; set; }

    public virtual bool ToEmployee { get; set; }

    public virtual bool ToRequestCreator { get; set; }

    public virtual bool ToManager { get; set; }

    public virtual bool SendToAll { get; set; }

    public virtual bool SendToAllApproved { get; set; }

    public virtual bool SendToApprover { get; set; }
}
